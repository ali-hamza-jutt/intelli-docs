namespace DocuMind.Application.Interfaces;

/// <summary>
/// Pulls text out of a PDF, one page at a time. Kept as an abstraction so no PDF-library type
/// reaches the Application layer and the library can be replaced — or an OCR fallback added —
/// without touching the pipeline.
/// </summary>
public interface IPdfTextExtractor
{
    /// <summary>
    /// Reads every page in reading order. Pages that contain no text are returned as empty
    /// strings rather than skipped, so page numbering stays aligned with the source document.
    /// </summary>
    Task<IReadOnlyList<ExtractedPage>> ExtractAsync(
        Stream content,
        CancellationToken cancellationToken = default);
}

/// <summary><paramref name="Number"/> is 1-based, matching what a PDF reader displays.</summary>
public record ExtractedPage(int Number, string Text);

/// <summary>Raised when a PDF carries no text layer at all — typically a scan needing OCR.</summary>
public class NoTextLayerException(string message) : Exception(message);

/// <summary>Raised when the file cannot be opened as a PDF, or is encrypted.</summary>
public class UnreadablePdfException(string message, Exception? inner = null)
    : Exception(message, inner);
