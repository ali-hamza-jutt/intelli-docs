namespace DocuMind.Infrastructure.Chunking;

/// <summary>
/// Chunking settings, measured in characters rather than tokens.
///
/// Characters are exact and need no tokenizer, so the same text always splits the same way. At
/// roughly four characters per English token, 1,000 characters is about 250 tokens — small enough
/// that several chunks fit one prompt with room for the question and the answer.
/// </summary>
public class ChunkingOptions
{
    public const string SectionName = "Rag";

    /// <summary>Maximum characters in a chunk, overlap included.</summary>
    public int ChunkSize { get; set; } = 1000;

    /// <summary>Characters repeated from the end of one chunk at the start of the next.</summary>
    public int ChunkOverlap { get; set; } = 150;

    /// <summary>
    /// Returns a message for each problem, or nothing if the settings are usable. Checked at
    /// startup, so a bad value stops the app rather than quietly producing useless chunks.
    /// </summary>
    public IEnumerable<string> Validate()
    {
        if (ChunkSize is < 200 or > 8000)
        {
            yield return $"Rag:ChunkSize must be between 200 and 8000 characters (was {ChunkSize}).";
        }

        if (ChunkOverlap < 0)
        {
            yield return $"Rag:ChunkOverlap cannot be negative (was {ChunkOverlap}).";
        }

        // Overlap eats into every chunk. Past half, most of each chunk would be repeated text and
        // the document would produce roughly twice as many chunks as it needs.
        if (ChunkOverlap >= ChunkSize / 2)
        {
            yield return $"Rag:ChunkOverlap ({ChunkOverlap}) must be less than half of Rag:ChunkSize ({ChunkSize}).";
        }
    }
}
