namespace DocuMind.Domain.Entities;

/// <summary>
/// One retrievable passage of a document — the unit that later gets embedded, searched, and cited.
///
/// A chunk can straddle a page break, so it records where it starts and ends. The start page is
/// the one a citation points at; the end page is kept so a citation can say "pages 7–8" honestly
/// rather than claiming the passage sits wholly on one page.
/// </summary>
public class DocumentChunk
{
    private DocumentChunk() { }

    public Guid Id { get; private set; }

    public Guid DocumentId { get; private set; }

    /// <summary>Zero-based position in the document. Restores reading order when chunks are listed.</summary>
    public int ChunkIndex { get; private set; }

    public string Text { get; private set; } = null!;

    /// <summary>1-based page the passage begins on.</summary>
    public int PageNumber { get; private set; }

    /// <summary>1-based page the passage ends on. Equal to <see cref="PageNumber"/> for most chunks.</summary>
    public int EndPageNumber { get; private set; }

    public int CharacterCount { get; private set; }

    /// <summary>Approximate — see <see cref="TextStatistics"/>. Used to budget how many chunks fit a prompt.</summary>
    public int TokenEstimate { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static DocumentChunk Create(
        Guid documentId,
        int chunkIndex,
        string text,
        int pageNumber,
        int endPageNumber)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("A chunk must contain text.", nameof(text));
        }

        if (chunkIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkIndex), "Chunk index cannot be negative.");
        }

        if (pageNumber < 1 || endPageNumber < pageNumber)
        {
            throw new ArgumentOutOfRangeException(
                nameof(endPageNumber),
                $"Page range {pageNumber}–{endPageNumber} is not valid.");
        }

        return new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            ChunkIndex = chunkIndex,
            Text = text,
            PageNumber = pageNumber,
            EndPageNumber = endPageNumber,
            CharacterCount = text.Length,
            TokenEstimate = TextStatistics.EstimateTokens(text),
            CreatedAt = DateTime.UtcNow
        };
    }
}
