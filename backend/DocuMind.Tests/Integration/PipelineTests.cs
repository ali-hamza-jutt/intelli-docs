using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace DocuMind.Tests.Integration;

/// <summary>
/// A document's whole journey: uploaded, queued, read, cleaned, split, embedded, and finally
/// answerable. Everything here is real except the embedding model — PdfPig really opens the file,
/// the background worker really picks it up, and the chunks really land in Postgres.
/// </summary>
[Collection("api")]
public class PipelineTests
{
    private readonly ApiFixture _api;

    public PipelineTests(ApiFixture api)
    {
        _api = api;
    }

    [Fact]
    public async Task A_pdf_becomes_something_that_can_be_asked_about()
    {
        var (client, _, _) = await _api.SignedInAsync();

        var accepted = await UploadAsync(client, PdfFixture.Bytes, "handbook.pdf");

        // 202, not 201: the document is registered but not yet usable, and the client polls.
        Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);

        var document = (await accepted.Content.ReadFromJsonAsync<DocumentRow>())!;
        Assert.Equal("Uploaded", document.Status);

        var settled = await WaitForStatusAsync(client, document.Id);
        Assert.Equal("Completed", settled.Status);
        Assert.Null(settled.ErrorMessage);

        // The text survived extraction, page by page.
        var text = await client.GetFromJsonAsync<DocumentText>($"/api/documents/{document.Id}/text");
        Assert.NotNull(text);
        Assert.True(text!.PageCount >= 1);
        Assert.Equal(text.PageCount, text.Pages.Count);
        Assert.All(text.Pages, page => Assert.True(page.PageNumber >= 1));

        var everything = string.Join(" ", text.Pages.Select(page => page.Text));
        Assert.Contains(PdfFixture.KnownPhrase, everything, StringComparison.OrdinalIgnoreCase);

        // And it was split into passages that each know where they came from.
        var chunks = await client.GetFromJsonAsync<ChunkPage>($"/api/documents/{document.Id}/chunks?limit=50");
        Assert.NotNull(chunks);
        Assert.True(chunks!.TotalCount > 0);
        Assert.All(chunks.Chunks, chunk =>
        {
            Assert.True(chunk.PageNumber >= 1);
            Assert.True(chunk.EndPageNumber >= chunk.PageNumber);
            Assert.True(chunk.CharacterCount <= chunks.ChunkSize);
        });

        // Which is what makes the document answerable.
        _api.Llm.WillAnswer("Employees are entitled to twenty days of paid annual leave [1].");

        var answered = await client.PostAsJsonAsync(
            $"/api/documents/{document.Id}/chat", new { question = "How much annual leave do employees get?" });

        answered.EnsureSuccessStatusCode();
        var answer = (await answered.Content.ReadFromJsonAsync<ChatAnswer>())!;

        Assert.True(answer.Grounded);
        Assert.NotEmpty(answer.Citations);
        Assert.Equal(document.Id, answer.Citations[0].DocumentId);
    }

    [Fact]
    public async Task Something_that_is_not_a_pdf_is_refused_on_its_contents()
    {
        var (client, _, _) = await _api.SignedInAsync();

        // An executable wearing a .pdf name. The extension is not what decides.
        var executable = Encoding.ASCII.GetBytes("MZ\u0090\0\u0003\0\0\0not a pdf at all");

        var response = await UploadAsync(client, executable, "totally-a.pdf");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var documents = await client.GetFromJsonAsync<List<DocumentRow>>("/api/documents");
        Assert.Empty(documents!);
    }

    [Fact]
    public async Task Deleting_a_document_takes_its_passages_with_it()
    {
        var (client, _, _) = await _api.SignedInAsync();

        var accepted = await UploadAsync(client, PdfFixture.Bytes, "handbook.pdf");
        var document = (await accepted.Content.ReadFromJsonAsync<DocumentRow>())!;

        await WaitForStatusAsync(client, document.Id);

        var before = await client.GetFromJsonAsync<ChunkPage>($"/api/documents/{document.Id}/chunks?limit=1");
        Assert.True(before!.TotalCount > 0);

        var deleted = await client.DeleteAsync($"/api/documents/{document.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        // Gone, and so is everything that pointed at it.
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/documents/{document.Id}/chunks")).StatusCode);
    }

    // ------------------------------------------------------------------------------- helpers

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, byte[] bytes, string fileName)
    {
        using var content = new MultipartFormDataContent();
        using var file = new ByteArrayContent(bytes);

        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(file, "file", fileName);

        return await client.PostAsync("/api/documents/upload", content);
    }

    /// <summary>Polls the way the browser does, until the document stops being in flight.</summary>
    private static async Task<StatusRow> WaitForStatusAsync(HttpClient client, Guid documentId)
    {
        for (var attempt = 0; attempt < 60; attempt++)
        {
            var status = await client.GetFromJsonAsync<StatusRow>($"/api/documents/{documentId}/status");

            if (status!.Status is "Completed" or "Failed")
            {
                return status;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException("the document never finished processing");
    }

    private sealed record DocumentRow(Guid Id, string FileName, string Status);

    private sealed record StatusRow(Guid Id, string Status, string? ErrorMessage);

    private sealed record DocumentText(int PageCount, int WordCount, List<PageRow> Pages);

    private sealed record PageRow(int PageNumber, string Text, int WordCount);

    private sealed record ChunkPage(int TotalCount, int ChunkSize, List<ChunkRow> Chunks);

    private sealed record ChunkRow(int Index, int PageNumber, int EndPageNumber, int CharacterCount);

    private sealed record ChatAnswer(string Answer, bool Grounded, List<Citation> Citations);

    private sealed record Citation(int Marker, Guid DocumentId, int PageNumber);
}
