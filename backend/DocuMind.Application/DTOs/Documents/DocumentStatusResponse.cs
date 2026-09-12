namespace DocuMind.Application.DTOs.Documents;

/// <summary>
/// Lightweight payload for the polling endpoint — the documents screen asks for this every few
/// seconds while a file is processing, so it stays far smaller than the full document.
/// </summary>
public class DocumentStatusResponse
{
    public required Guid Id { get; set; }

    public required string Status { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime? ProcessedAt { get; set; }
}
