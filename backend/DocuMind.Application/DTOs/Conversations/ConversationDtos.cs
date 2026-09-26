using System.ComponentModel.DataAnnotations;

namespace DocuMind.Application.DTOs.Conversations;

public class StartConversationRequest
{
    /// <summary>The document the whole thread will be grounded in.</summary>
    [Required]
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Optional opening question. Supplying it starts the thread and returns the first answer in one
    /// round trip, which is what "Chat with document" does.
    /// </summary>
    [StringLength(2000, MinimumLength = 3)]
    public string? Question { get; set; }
}

public class AskInConversationRequest
{
    [Required, StringLength(2000, MinimumLength = 3)]
    public string Question { get; set; } = string.Empty;
}

/// <summary>A row in the conversation list.</summary>
public class ConversationSummaryResponse
{
    public required Guid Id { get; set; }

    public required Guid DocumentId { get; set; }

    public required string Title { get; set; }

    public required int MessageCount { get; set; }

    public required DateTime CreatedAt { get; set; }

    public required DateTime UpdatedAt { get; set; }
}

/// <summary>A conversation with its messages, as the chat screen loads it after a reload.</summary>
public class ConversationResponse
{
    public required Guid Id { get; set; }

    public required Guid DocumentId { get; set; }

    /// <summary>The document's current name, or null if it has since been deleted.</summary>
    public string? DocumentName { get; set; }

    public required string Title { get; set; }

    public required DateTime CreatedAt { get; set; }

    public required DateTime UpdatedAt { get; set; }

    public required List<ChatMessageResponse> Messages { get; set; }
}

public class ChatMessageResponse
{
    public required Guid Id { get; set; }

    /// <summary>"User" or "Assistant", decided by the server.</summary>
    public required string Role { get; set; }

    public required string Content { get; set; }

    /// <summary>Null on a question. False on an answer written without any matching passage.</summary>
    public bool? Grounded { get; set; }

    public string? Model { get; set; }

    /// <summary>
    /// True when the reader stopped this answer part-way, so its text is incomplete and its model
    /// and token counts are unknown.
    /// </summary>
    public required bool Stopped { get; set; }

    public required int InputTokens { get; set; }

    public required int OutputTokens { get; set; }

    public required DateTime CreatedAt { get; set; }

    public required List<MessageSourceResponse> Sources { get; set; }
}

/// <summary>A stored citation. Copied when the answer was written, not looked up now.</summary>
public class MessageSourceResponse
{
    public required int Marker { get; set; }

    public required Guid DocumentId { get; set; }

    public required string FileName { get; set; }

    public required int PageNumber { get; set; }

    public required int EndPageNumber { get; set; }

    public required string Text { get; set; }

    public required double Similarity { get; set; }
}
