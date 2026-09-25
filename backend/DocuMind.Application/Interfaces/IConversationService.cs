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

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
