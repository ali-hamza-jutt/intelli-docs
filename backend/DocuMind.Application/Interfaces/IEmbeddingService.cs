namespace DocuMind.Application.Interfaces;

/// <summary>
/// Turns text into an embedding: a fixed-length list of numbers that stands for the text's meaning.
/// Two passages about the same subject produce vectors that sit close together, which is what makes
/// "find the passages that answer this question" a distance calculation rather than a keyword match.
///
/// Kept as an abstraction so no provider type reaches the Application layer, and so the pipeline
/// does not change when the model does.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>The model producing the vectors. Logged, because a change in it invalidates every
    /// vector already stored — distances are only comparable within one model.</summary>
    string Model { get; }

    /// <summary>
    /// The length of every vector returned. Fixed in configuration rather than read from the
    /// response, because module 7 declares a database column of exactly this width.
    /// </summary>
    int Dimensions { get; }

    /// <summary>Embeds one text — a search query, typically.</summary>
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Embeds many texts in as few provider calls as possible. The result has one vector per input,
    /// in the same order, so callers can pair it back to whatever the texts came from by position.
    /// </summary>
    Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Raised when embeddings cannot be produced: the provider is unreachable, refuses the request, is
/// rate limiting, or is not configured. The message is safe to show a user; the detail goes to the
/// log.
/// </summary>
public class EmbeddingException(string message, Exception? inner = null) : Exception(message, inner);
