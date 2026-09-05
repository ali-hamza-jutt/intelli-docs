using DocuMind.Application.DTOs.Documents;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;

namespace DocuMind.Application.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _repository;

    public DocumentService(IDocumentRepository repository)
    {
        _repository = repository;
    }

    public async Task<DocumentResponse> CreateAsync(
        CreateDocumentRequest request)
    {
        var document = new Document(
            request.FileName,
            request.ContentType,
            request.FileSize
        );

        await _repository.AddAsync(document);
        await _repository.SaveChangesAsync();

        return MapToResponse(document);
    }

    public async Task<List<DocumentResponse>> GetAllAsync()
    {
        var documents = await _repository.GetAllAsync();

        return documents
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<DocumentResponse?> GetByIdAsync(Guid id)
    {
        var document = await _repository.GetByIdAsync(id);

        return document is null
            ? null
            : MapToResponse(document);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var document = await _repository.GetByIdAsync(id);

        if (document is null)
        {
            return false;
        }

        await _repository.DeleteAsync(document);
        await _repository.SaveChangesAsync();

        return true;
    }

    private static DocumentResponse MapToResponse(
        Document document)
    {
        return new DocumentResponse
        {
            Id = document.Id,
            FileName = document.FileName,
            ContentType = document.ContentType,
            FileSize = document.FileSize,
            CreatedAt = document.CreatedAt
        };
    }
}