using System.ComponentModel.DataAnnotations;

namespace DocuMind.Application.DTOs.Search;

public class DocumentChatRequest
{
    [Required]
    public string Question { get; set; } = string.Empty;
}

public class ChatAnswerResponse
{
    public required string Question { get; set; }

    public required string Answer { get; set; }

    /// <summary>
    /// False when no passage was relevant enough to answer from, in which case the model was never
    /// asked and <see cref="Citations"/> is empty. A client can say "not in your documents" rather
    /// than presenting a refusal as the model's opinion.
    /// </summary>
    public required bool Grounded { get; set; }

    /// <summary>The passages the answer cites, matching the [n] markers in its text.</summary>
    public required List<ChatCitationResponse> Citations { get; set; }

    /// <summary>Which model wrote it, or null when nothing was asked.</summary>
    public string? Model { get; set; }

    public required int InputTokens { get; set; }

    public required int OutputTokens { get; set; }
}

/// <summary>A cited passage. Copied onto the answer, not looked up later.</summary>
public class ChatCitationResponse
{
    public required int Marker { get; set; }

    public required Guid DocumentId { get; set; }

    public required string FileName { get; set; }

    public required int PageNumber { get; set; }

    public required int EndPageNumber { get; set; }

    public required string Text { get; set; }

    public required double Similarity { get; set; }
}
