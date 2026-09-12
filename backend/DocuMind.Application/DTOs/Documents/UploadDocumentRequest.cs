namespace DocuMind.Application.DTOs.Documents;

/// <summary>
/// A file handed to the application layer. Deliberately not <c>IFormFile</c> — that type belongs
/// to ASP.NET Core, and depending on it here would drag the web framework into the Application project.
/// </summary>
public class UploadDocumentRequest
{
    public required Stream Content { get; init; }

    public required string FileName { get; init; }

    /// <summary>What the browser claimed. Advisory only; the extension decides.</summary>
    public string? DeclaredContentType { get; init; }

    public required long Length { get; init; }
}
