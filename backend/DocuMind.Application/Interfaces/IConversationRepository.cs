using DocuMind.Domain.Entities;

namespace DocuMind.Application.Interfaces;

public interface IConversationRepository
{
    Task AddAsync(Conversation conversation);

    /// <summary>
    /// A user's conversations, most recently used first, without their messages. The counts come
    /// back with them so the list needs one query rather than one per row.
    /// </summary>
    Task<List<ConversationSummary>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// One conversation with every message and source, or null if it is not this user's. Ownership is
    /// part of the query so a wrong id cannot return someone else's thread.
    /// </summary>
    Task<Conversation?> GetWithMessagesAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>The conversation alone, for appending to it without loading its history.</summary>
    Task<Conversation?> GetAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The last <paramref name="take"/> messages in order, for giving a model enough of the thread
    /// to resolve a follow-up question without paying for all of it.
    /// </summary>
    Task<List<ChatMessage>> GetRecentMessagesAsync(
        Guid conversationId,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a new turn.
    ///
    /// Added explicitly rather than left to be discovered through the conversation it was appended
    /// to: a turn is usually added to a thread that is already saved, and letting the change tracker
    /// infer what to do with it produced an update of a row that did not exist yet.
    /// </summary>
    Task AddMessageAsync(ChatMessage message);

    Task RemoveAsync(Conversation conversation);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>A row of the conversation list: the thread without its contents.</summary>
public record ConversationSummary(
    Guid Id,
    Guid DocumentId,
    string Title,
    int MessageCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);
