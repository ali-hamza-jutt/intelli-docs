using DocuMind.Domain.Entities;

namespace DocuMind.Application.Interfaces;

public interface IDocumentTextRepository
{
    Task AddAsync(DocumentText documentText);

    /// <summary>Header and full text, without loading the per-page rows.</summary>
    Task<DocumentText?> GetByDocumentIdAsync(Guid documentId);

    /// <summary>Includes the pages, ordered by page number.</summary>
    Task<DocumentText?> GetWithPagesAsync(Guid documentId);

    /// <summary>Clears any previous extraction so a reprocess cannot leave two copies behind.</summary>
    Task RemoveForDocumentAsync(Guid documentId);

    Task SaveChangesAsync();
}
