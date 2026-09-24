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

    /// <summary>
    /// The chunks closest to <paramref name="embedding"/>, nearest first.
    ///
    /// <paramref name="userId"/> is not a convenience filter — it is the security boundary, applied
    /// inside the query so no candidate from another account can reach the ranking at all.
    /// </summary>
    /// <param name="documentId">Limits the search to one document when set.</param>
    /// <param name="minimumSimilarity">Drops matches below this cosine similarity.</param>
    Task<List<ChunkMatch>> SearchAsync(
        float[] embedding,
        Guid userId,
        int topK,
        Guid? documentId,
        double minimumSimilarity,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// One retrieved passage with the score that retrieved it and enough of its document to cite it.
/// <paramref name="Similarity"/> runs from 0 (unrelated) to 1 (identical).
/// </summary>
public record ChunkMatch(
    Guid ChunkId,
    Guid DocumentId,
    string FileName,
    int ChunkIndex,
    int PageNumber,
    int EndPageNumber,
    string Text,
    double Similarity);
