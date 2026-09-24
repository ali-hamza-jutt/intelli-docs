using DocuMind.Domain.Entities;

namespace DocuMind.Application.Interfaces;

public interface IDocumentChunkRepository
{
    Task AddRangeAsync(IEnumerable<DocumentChunk> chunks);

    /// <summary>A page of chunks in reading order.</summary>
    Task<List<DocumentChunk>> GetForDocumentAsync(Guid documentId, int offset, int limit);

    Task<int> CountForDocumentAsync(Guid documentId);

    /// <summary>Clears a previous run's chunks so reprocessing cannot leave two sets behind.</summary>
    Task RemoveForDocumentAsync(Guid documentId);
}
