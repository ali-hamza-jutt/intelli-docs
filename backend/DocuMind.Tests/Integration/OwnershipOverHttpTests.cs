using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DocuMind.Domain.Entities;
using DocuMind.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace DocuMind.Tests.Integration;

/// <summary>
/// Ownership as a caller experiences it: over HTTP, with a real token, against the real routes.
///
/// The repository tests prove the SQL filters. These prove that nothing reaches around them — not a
/// route that forgot to pass a user id, not a body naming someone else's document, not a search.
/// </summary>
[Collection("api")]
public class OwnershipOverHttpTests
{
    private readonly ApiFixture _api;

    public OwnershipOverHttpTests(ApiFixture api)
    {
        _api = api;
    }

    [Fact]
    public async Task Another_users_document_is_not_found_rather_than_forbidden()
    {
        var (alice, _, _) = await _api.SignedInAsync();
        var (bob, _, _) = await _api.SignedInAsync();

        var document = await UploadAsync(alice);

        foreach (var path in new[]
        {
            $"/api/documents/{document}",
            $"/api/documents/{document}/status",
            $"/api/documents/{document}/text",
            $"/api/documents/{document}/chunks",
        })
        {
            var response = await bob.GetAsync(path);

            // 404, never 403: a 403 confirms the id exists, which is itself a disclosure.
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        Assert.Equal(HttpStatusCode.OK, (await alice.GetAsync($"/api/documents/{document}")).StatusCode);
    }

    [Fact]
    public async Task One_users_documents_never_appear_in_anothers_list()
    {
        var (alice, _, _) = await _api.SignedInAsync();
        var (bob, _, _) = await _api.SignedInAsync();

        var aliceDocument = await UploadAsync(alice);
        await UploadAsync(bob);

        var bobsList = await bob.GetFromJsonAsync<List<DocumentRow>>("/api/documents");

        Assert.NotNull(bobsList);
        Assert.DoesNotContain(bobsList, row => row.Id == aliceDocument);
        Assert.Single(bobsList);
    }

    [Fact]
    public async Task Search_never_returns_another_users_passage_even_when_it_matches_perfectly()
    {
        var (alice, aliceId, _) = await _api.SignedInAsync();
        var (bob, bobId, _) = await _api.SignedInAsync();

        // The same words for both, so the vectors are identical and only the owner filter can tell
        // them apart. This is precisely what a missing predicate would leak.
        const string text = "Employees are entitled to twenty days of paid annual leave.";

        var aliceDocument = await SeedSearchableChunkAsync(aliceId, text);
        var bobDocument = await SeedSearchableChunkAsync(bobId, text);

        var aliceFound = await SearchAsync(alice, text);
        var bobFound = await SearchAsync(bob, text);

        Assert.NotEmpty(aliceFound);
        Assert.NotEmpty(bobFound);
        Assert.All(aliceFound, match => Assert.Equal(aliceDocument, match.DocumentId));
        Assert.All(bobFound, match => Assert.Equal(bobDocument, match.DocumentId));
    }

    [Fact]
    public async Task Asking_about_another_users_document_by_id_finds_nothing_rather_than_their_text()
    {
        var (alice, aliceId, _) = await _api.SignedInAsync();
        var (bob, _, _) = await _api.SignedInAsync();

        const string text = "Employees are entitled to twenty days of paid annual leave.";
        var aliceDocument = await SeedSearchableChunkAsync(aliceId, text);

        var response = await bob.PostAsJsonAsync(
            "/api/search/semantic", new { query = text, documentId = aliceDocument });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<SearchResult>();

        // Narrowing to a document that is not yours narrows to nothing; it never widens.
        Assert.Empty(body!.Matches);

        var chat = await bob.PostAsJsonAsync(
            $"/api/documents/{aliceDocument}/chat", new { question = "How much annual leave is there?" });

        Assert.Equal(HttpStatusCode.NotFound, chat.StatusCode);
    }

    [Fact]
    public async Task A_conversation_belongs_to_the_account_that_started_it()
    {
        var (alice, aliceId, _) = await _api.SignedInAsync();
        var (bob, _, _) = await _api.SignedInAsync();

        var documentId = await SeedSearchableChunkAsync(aliceId, "Twenty days of annual leave.");

        var started = await alice.PostAsJsonAsync("/api/conversations", new { documentId });
        started.EnsureSuccessStatusCode();

        var conversation = (await started.Content.ReadFromJsonAsync<ConversationRow>())!.Id;

        Assert.Equal(HttpStatusCode.NotFound,
            (await bob.GetAsync($"/api/conversations/{conversation}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await bob.GetAsync($"/api/conversations/{conversation}/messages")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await bob.DeleteAsync($"/api/conversations/{conversation}")).StatusCode);

        var asked = await bob.PostAsJsonAsync(
            $"/api/conversations/{conversation}/messages", new { question = "Tell me everything" });

        Assert.Equal(HttpStatusCode.NotFound, asked.StatusCode);

        // Alice's thread survives all of it, and Bob's list never mentions it.
        Assert.Equal(HttpStatusCode.OK, (await alice.GetAsync($"/api/conversations/{conversation}")).StatusCode);
        Assert.Empty(await bob.GetFromJsonAsync<List<ConversationRow>>("/api/conversations") ?? []);
    }

    [Fact]
    public async Task Everything_refuses_a_caller_with_no_token()
    {
        var anonymous = _api.CreateClient();

        foreach (var path in new[] { "/api/documents", "/api/conversations", "/api/usage" })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path)).StatusCode);
        }

        var searched = await anonymous.PostAsJsonAsync(
            "/api/search/semantic", new { query = "anything at all" });

        Assert.Equal(HttpStatusCode.Unauthorized, searched.StatusCode);
    }

    // ------------------------------------------------------------------------------- helpers

    /// <summary>Uploads the fixture PDF and returns the new document's id.</summary>
    private static async Task<Guid> UploadAsync(HttpClient client)
    {
        using var content = new MultipartFormDataContent();
        using var file = new ByteArrayContent(PdfFixture.Bytes);

        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(file, "file", "handbook.pdf");

        var response = await client.PostAsync("/api/documents/upload", content);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<DocumentRow>())!.Id;
    }

    /// <summary>
    /// Writes one searchable chunk straight to the database. Retrieval is what these tests are
    /// about, so they do not wait on extraction to produce something to retrieve.
    /// </summary>
    private async Task<Guid> SeedSearchableChunkAsync(Guid userId, string text)
    {
        using var scope = _api.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DocuMindDbContext>();

        var document = Document.Upload(
            userId, "handbook.pdf", $"{Guid.NewGuid():N}.pdf", $"{Guid.NewGuid():N}.pdf",
            "application/pdf", PdfFixture.Bytes.Length);

        document.MarkCompleted();
        context.Documents.Add(document);

        var chunk = DocumentChunk.Create(document.Id, 0, text, 1, 1);
        chunk.AttachEmbedding(await _api.Embeddings.EmbedAsync(text));
        context.DocumentChunks.Add(chunk);

        await context.SaveChangesAsync();

        return document.Id;
    }

    private static async Task<List<SearchMatch>> SearchAsync(HttpClient client, string query)
    {
        var response = await client.PostAsJsonAsync("/api/search/semantic", new { query, topK = 10 });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<SearchResult>())!.Matches;
    }

    private sealed record DocumentRow(Guid Id, string FileName, string Status);

    private sealed record ConversationRow(Guid Id, string Title);

    private sealed record SearchResult(List<SearchMatch> Matches);

    private sealed record SearchMatch(Guid DocumentId, int PageNumber, double Similarity);
}
