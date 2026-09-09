using DocuMind.Application.DTOs.Documents;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;

namespace DocuMind.Application.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _repository;
    private readonly ICurrentUser _currentUser;

    public DocumentService(
        IDocumentRepository repository,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<DocumentResponse> CreateAsync(
        CreateDocumentRequest request)
    {
        // The owner comes from the bearer token, never from the request body.
        var userId = _currentUser.RequireUserId();

        var document = new Document(
            userId,
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
        var userId = _currentUser.RequireUserId();
        var documents = await _repository.GetAllForUserAsync(userId);

        return documents
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<DocumentResponse?> GetByIdAsync(Guid id)
    {
        var userId = _currentUser.RequireUserId();
        var document = await _repository.GetByIdForUserAsync(id, userId);

        // A document owned by someone else is indistinguishable from one that does not exist.
        return document is null
            ? null
            : MapToResponse(document);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var userId = _currentUser.RequireUserId();
        var document = await _repository.GetByIdForUserAsync(id, userId);

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
