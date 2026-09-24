namespace DocuMind.Infrastructure.Chunking;

/// <summary>
/// How much to retrieve for a question, and how close a passage has to be to count. Shares the
/// <c>Rag</c> section with <see cref="ChunkingOptions"/>: one section for the whole retrieval
/// pipeline, split into the part that writes chunks and the part that reads them.
/// </summary>
public class RetrievalOptions
{
    public const string SectionName = "Rag";

    /// <summary>
    /// How many chunks a search returns. Enough to answer from several passages, few enough that
    /// they all fit one prompt alongside the question and the answer.
    /// </summary>
    public int TopK { get; set; } = 5;

    /// <summary>
    /// Cosine similarity a chunk must reach to be returned at all, from 0 (unrelated) to 1
    /// (identical). Without a floor, a question the documents do not cover still returns the five
    /// least-unrelated passages, and module 8 would build an answer out of them.
    /// </summary>
    public double SimilarityThreshold { get; set; } = 0.25;

    /// <summary>
    /// How many passages may go into a prompt. Every one costs tokens on every question, and a
    /// model given too much context answers from the wrong part of it, so this stays small.
    /// </summary>
    public int MaxContextChunks { get; set; } = 5;

    /// <summary>Returns a message for each problem, or nothing if the settings are usable.</summary>
    public IEnumerable<string> Validate()
    {
        if (MaxContextChunks is < 1 or > 20)
        {
            yield return $"Rag:MaxContextChunks must be between 1 and 20 (was {MaxContextChunks}).";
        }

        if (TopK is < 1 or > 50)
        {
            yield return $"Rag:TopK must be between 1 and 50 (was {TopK}).";
        }

        if (SimilarityThreshold is < 0 or > 1)
        {
            yield return
                $"Rag:SimilarityThreshold must be between 0 and 1 (was {SimilarityThreshold}).";
        }
    }
}
