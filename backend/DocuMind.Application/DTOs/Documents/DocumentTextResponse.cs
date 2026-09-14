namespace DocuMind.Application.DTOs.Documents;

/// <summary>Extracted text for the document detail preview.</summary>
public class DocumentTextResponse
{
    public required Guid DocumentId { get; set; }

    public required int PageCount { get; set; }

    public required int WordCount { get; set; }

    public required int CharacterCount { get; set; }

    public required DateTime ExtractedAt { get; set; }

    /// <summary>Pages in reading order.</summary>
    public required List<DocumentPageResponse> Pages { get; set; }
}

public class DocumentPageResponse
{
    public required int PageNumber { get; set; }

    public required string Text { get; set; }

    public required int WordCount { get; set; }
}
