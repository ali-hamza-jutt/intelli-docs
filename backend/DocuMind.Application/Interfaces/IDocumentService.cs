namespace DocuMind.Application.Interfaces;

using DocuMind.Application.DTOs.Documents;

public interface IDocumentService
{
    /// <summary>Signs a short-lived, single-purpose permission for the browser to upload directly.</summary>
    UploadTicketResponse CreateUploadTicket(UploadTicketRequest request);

    /// <summary>
    /// Registers a document after a direct upload. Verifies the asset with the provider rather
    /// than trusting what the browser reports.
    /// </summary>
    Task<DocumentResponse> ConfirmUploadAsync(
        ConfirmUploadRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Validates the file, stores it, and records it as Uploaded.</summary>
    Task<DocumentResponse> UploadAsync(
        UploadDocumentRequest request,
        CancellationToken cancellationToken = default);

    Task<List<DocumentResponse>> GetAllAsync();

    Task<DocumentResponse?> GetByIdAsync(Guid id);

    /// <summary>Just the fields the frontend polls for while a document is processing.</summary>
    Task<DocumentStatusResponse?> GetStatusAsync(Guid id);

    /// <summary>Removes the database row and the stored file together.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Opens the stored file for download, scoped to the owner.</summary>
    Task<DocumentDownload?> DownloadAsync(Guid id, CancellationToken cancellationToken = default);
}

public record DocumentDownload(Stream Content, string FileName, string ContentType);
