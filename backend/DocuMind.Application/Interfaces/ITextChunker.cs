namespace DocuMind.Application.Interfaces;

/// <summary>
/// Splits cleaned text into overlapping passages small enough to embed, and to fit several of
/// into a single prompt.
///
/// A pure function: no I/O, no clock, no randomness. The same pages always produce the same
/// chunks, which is what makes chunking the easiest stage of the pipeline to test properly.
/// </summary>
public interface ITextChunker
{
    /// <summary>Maximum characters per chunk, overlap included.</summary>
    int ChunkSize { get; }

    /// <summary>Characters carried from the end of each chunk into the start of the next.</summary>
    int ChunkOverlap { get; }

    /// <summary>Chunks in reading order. Empty only when the pages contain no text.</summary>
    IReadOnlyList<TextChunk> Chunk(IReadOnlyList<ExtractedPage> pages);
}

/// <summary>
/// A passage and the page range its own content came from. A chunk can cross a page break, so it
/// carries both ends of the range. Text repeated from the previous chunk as overlap does not count
/// toward the range — it belongs to the chunk it was copied from.
/// </summary>
public record TextChunk(string Text, int StartPage, int EndPage);
