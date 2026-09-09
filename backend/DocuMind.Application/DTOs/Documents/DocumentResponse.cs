namespace DocuMind.Application.DTOs.Documents;

public class DocumentResponse
{
    public required Guid Id { get; set; }

    public required string FileName { get; set; }

    public required string ContentType { get; set; }

    public required long FileSize { get; set; }

    public required DateTime CreatedAt { get; set; }
}
