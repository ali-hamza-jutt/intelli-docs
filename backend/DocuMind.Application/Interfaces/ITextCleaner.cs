namespace DocuMind.Application.Interfaces;

/// <summary>
/// Normalises raw extracted text. The goal is to remove artefacts of PDF layout without
/// altering meaning — an aggressive cleaner destroys the very content retrieval depends on.
/// </summary>
public interface ITextCleaner
{
    /// <summary>Cleans a single page. Returns an empty string when nothing survives.</summary>
    string Clean(string rawText);

    /// <summary>
    /// Cleans every page and drops those left empty, while keeping each surviving page's original
    /// number so citations still point at the right place.
    /// </summary>
    IReadOnlyList<ExtractedPage> CleanPages(IReadOnlyList<ExtractedPage> pages);
}
