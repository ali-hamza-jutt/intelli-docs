using DocuMind.Application.Interfaces;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
using UglyToad.PdfPig.Exceptions;

namespace DocuMind.Infrastructure.Pdf;

/// <summary>
/// Extracts text with PdfPig.
///
/// PDFs store glyphs with coordinates, not sentences — the drawing order is whatever the
/// producing program emitted, which for a two-column layout is often column-interleaved nonsense.
/// The nearest-neighbour word extractor groups glyphs into words by proximity, which recovers
/// something close to reading order.
/// </summary>
public class PdfPigTextExtractor : IPdfTextExtractor
{
    private readonly ILogger<PdfPigTextExtractor> _logger;

    public PdfPigTextExtractor(ILogger<PdfPigTextExtractor> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<ExtractedPage>> ExtractAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        // PdfPig needs random access and the stream may be a forward-only network response,
        // so it is buffered first.
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        PdfDocument document;

        try
        {
            document = PdfDocument.Open(buffer);
        }
        catch (PdfDocumentEncryptedException ex)
        {
            throw new UnreadablePdfException(
                "This PDF is password-protected. Remove the password and upload it again.", ex);
        }
        catch (Exception ex)
        {
            throw new UnreadablePdfException(
                "This file could not be opened as a PDF. It may be corrupt.", ex);
        }

        using (document)
        {
            var pages = new List<ExtractedPage>(document.NumberOfPages);

            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();

                pages.Add(new ExtractedPage(page.Number, ReadPage(page)));
            }

            // A scan has pages but no glyphs on any of them. Saying so beats storing an empty
            // document that silently answers nothing for the rest of its life.
            if (pages.All(page => string.IsNullOrWhiteSpace(page.Text)))
            {
                _logger.LogWarning(
                    "PDF has {PageCount} pages but no text layer — likely a scan",
                    pages.Count);

                throw new NoTextLayerException(
                    "No text could be read from this PDF. It looks like a scan, which needs OCR " +
                    "before it can be searched.");
            }

            return pages;
        }
    }

    private string ReadPage(UglyToad.PdfPig.Content.Page page)
    {
        try
        {
            var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters);

            return string.Join(' ', words.Select(word => word.Text));
        }
        catch (Exception ex)
        {
            // One malformed page should not lose the other ninety-nine.
            _logger.LogWarning(ex, "Falling back to raw text order on page {PageNumber}", page.Number);

            return page.Text;
        }
    }
}
