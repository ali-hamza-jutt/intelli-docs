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
    /// <summary>
    /// The width of every stored embedding, fixed because the database column is declared
    /// <c>vector(1536)</c>. Configuration is checked against this at startup, so the number in
    /// settings can never drift from the number in the schema — changing it means a migration and
    /// re-embedding every chunk.
    /// </summary>
    public const int EmbeddingDimensions = 1536;

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

    /// <summary>
    /// The passage's meaning as coordinates. Null until the chunk has been embedded — a chunk with
    /// no vector cannot be found by a search, which is why the pipeline embeds before it saves.
    ///
    /// Held as <c>float[]</c> so the Domain stays free of any database or provider type; the
    /// persistence layer maps it to a pgvector column.
    /// </summary>
    public float[]? Embedding { get; private set; }

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

    /// <summary>
    /// Attaches the vector this passage embeds to. Rejects the wrong width here rather than letting
    /// the database do it, so the message names the chunk instead of the column.
    /// </summary>
    public void AttachEmbedding(float[] embedding)
    {
        if (embedding.Length != EmbeddingDimensions)
        {
            throw new ArgumentException(
                $"A chunk embedding must have {EmbeddingDimensions} dimensions, not {embedding.Length}.",
                nameof(embedding));
        }

        Embedding = embedding;
    }
}
