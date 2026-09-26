using System.ComponentModel.DataAnnotations;

namespace DocuMind.Application.DTOs.Search;

public class SemanticSearchRequest
{
    /// <summary>
    /// What to look for, in the user's own words. The lower bound rejects a stray keystroke, which
    /// would otherwise cost an embedding call and return whatever happened to be least unrelated.
    /// </summary>
    [Required]
    public string Query { get; set; } = string.Empty;

    /// <summary>How many passages to return. The configured default when omitted.</summary>
    public int? TopK { get; set; }

    /// <summary>Restricts the search to one document. All of the caller's documents when omitted.</summary>
    public Guid? DocumentId { get; set; }
}

public class SemanticSearchResponse
{
    public required string Query { get; set; }

    /// <summary>The floor a passage had to clear to appear here, so a thin result set is explicable.</summary>
    public required double SimilarityThreshold { get; set; }

    public required List<SemanticMatchResponse> Matches { get; set; }
}

/// <summary>One matching passage, with what a citation needs to point back at it.</summary>
public class SemanticMatchResponse
{
    public required Guid DocumentId { get; set; }

    public required string FileName { get; set; }

    public required int ChunkIndex { get; set; }

    public required int PageNumber { get; set; }

    public required int EndPageNumber { get; set; }

    /// <summary>Cosine similarity, 0 (unrelated) to 1 (identical).</summary>
    public required double Similarity { get; set; }

    public required string Text { get; set; }
}
