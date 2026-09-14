using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DocuMind.Application.Services;

/// <summary>
/// The ingestion pipeline for one document: fetch the stored file, extract its text, clean it,
/// and record the result.
///
/// Content failures never propagate. A PDF that cannot be read is a normal outcome the user needs
/// to see, so it becomes a Failed status with an actionable message rather than an exception that
/// surfaces as a 500 or, worse, leaves the document stuck in Processing forever.
/// </summary>
public class DocumentProcessor : IDocumentProcessor
{
    private readonly IDocumentRepository _documents;
    private readonly IDocumentTextRepository _texts;
    private readonly IFileStorageService _storage;
    private readonly IPdfTextExtractor _extractor;
    private readonly ITextCleaner _cleaner;
    private readonly ILogger<DocumentProcessor> _logger;

    public DocumentProcessor(
        IDocumentRepository documents,
        IDocumentTextRepository texts,
        IFileStorageService storage,
        IPdfTextExtractor extractor,
        ITextCleaner cleaner,
        ILogger<DocumentProcessor> logger)
    {
        _documents = documents;
        _texts = texts;
        _storage = storage;
        _extractor = extractor;
        _cleaner = cleaner;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // Processing runs outside any user's request in module 4, so it loads by id alone.
        // Ownership was established when the document was created.
        var document = await _documents.GetByIdAsync(documentId);

        if (document is null)
        {
            _logger.LogWarning("Processing requested for unknown document {DocumentId}", documentId);
            return;
        }

        document.MarkProcessing();
        await _documents.SaveChangesAsync();

        try
        {
            // A reprocess must not leave the previous extraction behind alongside the new one.
            await _texts.RemoveForDocumentAsync(documentId);

            await using var content = await _storage.OpenAsync(document.FilePath, cancellationToken);

            var rawPages = await _extractor.ExtractAsync(content, cancellationToken);
            var cleanPages = _cleaner.CleanPages(rawPages);

            if (cleanPages.Count == 0)
            {
                throw new NoTextLayerException(
                    "No readable text remained after cleaning this document.");
            }

            var documentText = DocumentText.Create(
                documentId,
                [.. cleanPages.Select(page => (page.Number, page.Text))]);

            await _texts.AddAsync(documentText);
            await _texts.SaveChangesAsync();

            document.MarkCompleted();
            await _documents.SaveChangesAsync();

            _logger.LogInformation(
                "Extracted {Pages} pages, {Words} words from document {DocumentId}",
                documentText.PageCount, documentText.WordCount, documentId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown, not a content problem. Leave the document Processing so the next run
            // picks it up instead of marking it permanently failed.
            _logger.LogInformation("Processing of {DocumentId} cancelled", documentId);
            throw;
        }
        catch (Exception ex)
        {
            // Everything else is reported to the user on the document itself.
            var message = ex switch
            {
                NoTextLayerException or UnreadablePdfException => ex.Message,
                FileNotFoundException => "The stored file is no longer available.",
                _ => "Something went wrong while processing this document."
            };

            _logger.LogError(ex, "Processing failed for document {DocumentId}", documentId);

            document.MarkFailed(message);
            await _documents.SaveChangesAsync();
        }
    }
}
