namespace DocuMind.Application.Interfaces;

using DocuMind.Application.DTOs.Documents;

public interface IDocumentService
{
    Task<DocumentResponse> CreateAsync(
        CreateDocumentRequest request);

    Task<List<DocumentResponse>> GetAllAsync();

    Task<DocumentResponse?> GetByIdAsync(Guid id);

    Task<bool> DeleteAsync(Guid id);
}