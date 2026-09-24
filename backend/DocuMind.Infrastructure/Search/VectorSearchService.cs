using DocuMind.Application.Interfaces;
using DocuMind.Infrastructure.Chunking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocuMind.Infrastructure.Search;

/// <summary>
/// Embeds a question and asks the repository for the nearest chunks.
///
/// Deliberately thin: the ranking rules live in SQL next to the index they depend on, and the
/// provider call lives behind <see cref="IEmbeddingService"/>. What is left here is the policy —
/// how many matches, how close they must be, and whose documents may be searched.
/// </summary>
public class VectorSearchService : IVectorSearchService
{
    /// <summary>Ceiling on a caller-supplied count, so a request cannot ask for the whole library.</summary>
    private const int MaxTopK = 50;

    private readonly IEmbeddingService _embeddings;
    private readonly IDocumentChunkRepository _chunks;
    private readonly RetrievalOptions _options;
    private readonly ILogger<VectorSearchService> _logger;

    public VectorSearchService(
        IEmbeddingService embeddings,
        IDocumentChunkRepository chunks,
        IOptions<RetrievalOptions> options,
        ILogger<VectorSearchService> logger)
    {
        _embeddings = embeddings;
        _chunks = chunks;
        _options = options.Value;
        _logger = logger;
    }

    public double SimilarityThreshold => _options.SimilarityThreshold;

    public async Task<IReadOnlyList<ChunkMatch>> SearchAsync(
        string query,
        Guid userId,
        int? topK = null,
        Guid? documentId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var limit = Math.Clamp(topK ?? _options.TopK, 1, MaxTopK);
        var embedding = await _embeddings.EmbedAsync(query, cancellationToken);

        var matches = await _chunks.SearchAsync(
            embedding, userId, limit, documentId, _options.SimilarityThreshold, cancellationToken);

        // The question itself is the user's content, so only its shape is logged.
        _logger.LogInformation(
            "Search over {Scope} returned {Matches} of at most {Limit} chunks, best similarity {Best:F3}",
            documentId is null ? "all documents" : "one document",
            matches.Count,
            limit,
            matches.Count == 0 ? 0 : matches[0].Similarity);

        return matches;
    }
}
