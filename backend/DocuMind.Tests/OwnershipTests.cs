using DocuMind.Infrastructure.Repositories;

namespace DocuMind.Tests;

/// <summary>
/// The three tests the whole schema was shaped around: one account must never reach another's
/// documents, chunks or conversations.
///
/// Both users hold identical documents, identical chunk text and — deliberately — the same embedding,
/// so nothing but the ownership predicate can separate them. Each test asks for the other user's row
/// by its real id, which is the case a missing filter would let through.
/// </summary>
public class OwnershipTests : IClassFixture<OwnershipFixture>
{
    private readonly OwnershipFixture _fixture;

    public OwnershipTests(OwnershipFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task A_document_is_invisible_to_another_user()
    {
        await using var context = _fixture.NewContext();
        var documents = new DocumentRepository(context);

        var own = await documents.GetByIdForUserAsync(_fixture.AliceDocumentId, _fixture.AliceId);
        var other = await documents.GetByIdForUserAsync(_fixture.BobDocumentId, _fixture.AliceId);
        var list = await documents.GetAllForUserAsync(_fixture.AliceId);

        Assert.NotNull(own);

        // Null, not a document she may not read: the API turns this into a 404, so the response does
        // not reveal that the id exists.
        Assert.Null(other);
        Assert.Single(list);
        Assert.Equal(_fixture.AliceDocumentId, list[0].Id);
    }

    [Fact]
    public async Task Vector_search_never_crosses_users_even_on_an_identical_match()
    {
        await using var context = _fixture.NewContext();
        var chunks = new DocumentChunkRepository(context);

        // The query vector is exactly what both users' chunks were stored with, so Bob's chunk is as
        // perfect a match as Alice's. Only the owner filter keeps it out.
        var matches = await chunks.SearchAsync(
            _fixture.SharedEmbedding,
            _fixture.AliceId,
            topK: 10,
            documentId: null,
            minimumSimilarity: 0);

        Assert.NotEmpty(matches);
        Assert.All(matches, match => Assert.Equal(_fixture.AliceDocumentId, match.DocumentId));

        // Asking for the other user's document by id narrows the search to nothing rather than
        // widening it to him.
        var scoped = await chunks.SearchAsync(
            _fixture.SharedEmbedding,
            _fixture.AliceId,
            topK: 10,
            documentId: _fixture.BobDocumentId,
            minimumSimilarity: 0);

        Assert.Empty(scoped);
    }

    [Fact]
    public async Task A_conversation_and_its_messages_are_invisible_to_another_user()
    {
        await using var context = _fixture.NewContext();
        var conversations = new ConversationRepository(context);

        var own = await conversations.GetWithMessagesAsync(
            _fixture.AliceConversationId, _fixture.AliceId);
        var other = await conversations.GetWithMessagesAsync(
            _fixture.BobConversationId, _fixture.AliceId);
        var forAppending = await conversations.GetAsync(_fixture.BobConversationId, _fixture.AliceId);
        var list = await conversations.ListForUserAsync(_fixture.AliceId);

        Assert.NotNull(own);
        Assert.Equal(2, own.Messages.Count);
        Assert.Single(own.Messages.Last().Sources);

        // Both reads are filtered: one serves the screen, the other appends a question to the thread.
        Assert.Null(other);
        Assert.Null(forAppending);
        Assert.Single(list);
        Assert.Equal(_fixture.AliceConversationId, list[0].Id);
    }
}
