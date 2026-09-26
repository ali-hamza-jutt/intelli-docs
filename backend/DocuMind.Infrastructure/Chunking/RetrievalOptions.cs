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
    /// Cosine similarity a passage must reach to be used at all, from 0 (unrelated) to 1
    /// (identical).
    ///
    /// Measured, not guessed: against gemini-embedding-001, questions the documents answer score
    /// 67-75% on their best passage, while questions they cannot answer top out around 44%. Half
    /// sits in that gap with room on both sides. The number belongs to the embedding model, so
    /// changing AI:EmbeddingModel means measuring it again — a floor that is too low sends an
    /// unanswerable question to the model anyway, and one that is too high starves a fair question
    /// of its context.
    /// </summary>
    public double SimilarityThreshold { get; set; } = 0.5;

    /// <summary>
    /// How many passages may go into a prompt. Every one costs tokens on every question, and a
    /// model given too much context answers from the wrong part of it, so this stays small.
    /// </summary>
    public int MaxContextChunks { get; set; } = 5;

    /// <summary>
    /// How many earlier turns of a conversation are replayed into a prompt. Every one is re-sent with
    /// every question, so an unbounded history costs more on each turn while adding less.
    /// </summary>
    public int MaxHistoryMessages { get; set; } = 8;

    /// <summary>Returns a message for each problem, or nothing if the settings are usable.</summary>
    public IEnumerable<string> Validate()
    {
        if (MaxHistoryMessages is < 0 or > 50)
        {
            yield return $"Rag:MaxHistoryMessages must be between 0 and 50 (was {MaxHistoryMessages}).";
        }

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
