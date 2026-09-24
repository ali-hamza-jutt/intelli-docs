namespace DocuMind.Application.Interfaces;

/// <summary>
/// Finds the passages that best match a question.
///
/// The question is embedded with the same model as the chunks — distances only mean anything within
/// one model — and compared by cosine similarity. Module 8 puts the matches into a prompt; on its
/// own this is already a working search over a user's documents.
/// </summary>
public interface IVectorSearchService
{
    /// <summary>
    /// The similarity a passage must reach to be returned, so a caller can report why a result set
    /// is short rather than leaving "no matches" unexplained.
    /// </summary>
    double SimilarityThreshold { get; }

    /// <summary>
    /// The best matches for <paramref name="query"/>, most similar first.
    /// </summary>
    /// <param name="userId">Whose documents to search. Never taken from client input.</param>
    /// <param name="topK">How many to return; the configured default when null.</param>
    /// <param name="documentId">Restricts the search to one document when set.</param>
    Task<IReadOnlyList<ChunkMatch>> SearchAsync(
        string query,
        Guid userId,
        int? topK = null,
        Guid? documentId = null,
        CancellationToken cancellationToken = default);
}
