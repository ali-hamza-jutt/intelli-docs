namespace DocuMind.Application.DTOs.Documents;

/// <summary>
/// A document's chunks, paged. Returned mainly so chunking can be inspected — seeing where the
/// splits land is the fastest way to tell whether chunk size and overlap suit a document.
/// </summary>
public class DocumentChunksResponse
{
    public required Guid DocumentId { get; set; }

    /// <summary>Total chunks for the document, not just this page.</summary>
    public required int TotalCount { get; set; }

    public required int Offset { get; set; }

    public required int Limit { get; set; }

    /// <summary>
    /// The current chunking settings. A document processed under different settings shows its old
    /// chunks until it is reprocessed.
    /// </summary>
    public required int ChunkSize { get; set; }

    public required int ChunkOverlap { get; set; }

    public required List<DocumentChunkResponse> Chunks { get; set; }
}

public class DocumentChunkResponse
{
    public required int Index { get; set; }

    public required int PageNumber { get; set; }

    public required int EndPageNumber { get; set; }

    public required int CharacterCount { get; set; }

    public required int TokenEstimate { get; set; }

    /// <summary>
    /// How many leading characters repeat the end of the previous chunk. Zero for the first chunk
    /// in the document, and for a chunk whose predecessor was not in this page of results.
    /// </summary>
    public required int OverlapWithPrevious { get; set; }

    public required string Text { get; set; }
}
