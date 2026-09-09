using DocuMind.Domain.Entities;

namespace DocuMind.Application.Interfaces;

/// <summary>
/// Every read takes a userId. Ownership is enforced in the query rather than checked afterwards,
/// so there is no path that can accidentally return another user's document.
/// </summary>
public interface IDocumentRepository
{
    Task AddAsync(Document document);

    Task<List<Document>> GetAllForUserAsync(Guid userId);

    Task<Document?> GetByIdForUserAsync(Guid id, Guid userId);

    Task DeleteAsync(Document document);

    Task SaveChangesAsync();
}
