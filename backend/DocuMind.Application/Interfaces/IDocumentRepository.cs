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

    /// <summary>
    /// By id alone, for background processing that runs outside any user request. Never call this
    /// from a path that serves a user — use GetByIdForUserAsync so ownership stays enforced.
    /// </summary>
    Task<Document?> GetByIdAsync(Guid id);

    /// <summary>
    /// Documents in any of the given states, across all users. Used only by the ingestion worker
    /// to recover work stranded by a restart.
    /// </summary>
    Task<List<Document>> GetByStatusAsync(IReadOnlyCollection<DocumentStatus> statuses);

    Task DeleteAsync(Document document);

    Task SaveChangesAsync();
}
