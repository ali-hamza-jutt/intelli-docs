namespace DocuMind.Application.DTOs.Documents;

public class CreateDocumentRequest
{
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }
}