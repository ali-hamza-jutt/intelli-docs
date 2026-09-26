using DocuMind.Application.DTOs.Conversations;

namespace DocuMind.Application.Interfaces;

/// <summary>
/// Conversations belonging to the signed-in user. Every method resolves the owner itself, and
/// anything that is not theirs comes back as null so the API can answer 404 rather than reveal that
/// the thread exists.
/// </summary>
public interface IConversationService
{
    /// <summary>
    /// Starts a thread about a document, optionally answering an opening question in the same call.
    /// Null when the document is not this user's.
    /// </summary>
    Task<ConversationResponse?> StartAsync(
        StartConversationRequest request,
        CancellationToken cancellationToken = default);

    Task<List<ConversationSummaryResponse>> ListAsync(CancellationToken cancellationToken = default);

    Task<ConversationResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<ChatMessageResponse>?> GetMessagesAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks a question in an existing thread and returns the answer that was stored, sources
    /// included.
    /// </summary>
    Task<ChatMessageResponse?> AskAsync(
        Guid id,
        AskInConversationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks a question and hands the answer over as it is written.
    ///
    /// Whatever text arrived is stored either way. If the reader stops it — or walks away, which
    /// looks the same from here — the partial answer is saved as partial, so the thread reflects what
    /// was actually said rather than losing it. Throws <see cref="Common.NotFoundAppException"/>
    /// before anything is emitted if the conversation is not this user's.
    /// </summary>
    IAsyncEnumerable<ConversationStreamEvent> StreamAskAsync(
        Guid id,
        AskInConversationRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>What a streamed answer emits: deltas as it is written, then one final event.</summary>
public abstract record ConversationStreamEvent
{
    /// <summary>The next piece of the answer's text.</summary>
    public sealed record Delta(string Text) : ConversationStreamEvent;

    /// <summary>
    /// The stored answer. Sent even when the answer was stopped, so a client can replace what it
    /// rendered with what was actually kept.
    /// </summary>
    public sealed record Final(ChatMessageResponse Message) : ConversationStreamEvent;
}
