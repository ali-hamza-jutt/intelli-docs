using DocuMind.Application.Common;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DocuMind.Application.Services;

/// <summary>
/// The ingestion pipeline for one document: fetch the stored file, extract its text, clean it,
/// split it into chunks, embed those chunks, and record the result.
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
    private readonly ITextChunker _chunker;
    private readonly IDocumentChunkRepository _chunks;
    private readonly IEmbeddingService _embeddings;
    private readonly ILogger<DocumentProcessor> _logger;

    public DocumentProcessor(
        IDocumentRepository documents,
        IDocumentTextRepository texts,
        IDocumentChunkRepository chunks,
        IFileStorageService storage,
        IPdfTextExtractor extractor,
        ITextCleaner cleaner,
        ITextChunker chunker,
        IEmbeddingService embeddings,
        ILogger<DocumentProcessor> logger)
    {
        _documents = documents;
        _texts = texts;
        _chunks = chunks;
        _storage = storage;
        _extractor = extractor;
        _cleaner = cleaner;
        _chunker = chunker;
        _embeddings = embeddings;
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
            // A reprocess must not leave the previous run's text or chunks behind alongside the new.
            await _chunks.RemoveForDocumentAsync(documentId);
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

            // Chunking runs on the cleaned pages rather than the combined text, so every chunk
            // keeps the page numbers it came from.
            var textChunks = _chunker.Chunk(cleanPages);

            if (textChunks.Count == 0)
            {
                throw new NoTextLayerException(
                    "No readable text remained after splitting this document into passages.");
            }

            var chunks = textChunks
                .Select((chunk, index) => DocumentChunk.Create(
                    documentId, index, chunk.Text, chunk.StartPage, chunk.EndPage))
                .ToList();

            // Embedding happens before anything is saved, so a provider failure leaves the document
            // Failed rather than Completed but unsearchable. The vectors come back in the order the
            // texts went out, which is what makes pairing them by position safe.
            var vectors = await _embeddings.EmbedBatchAsync(
                [.. chunks.Select(chunk => chunk.Text)], cancellationToken);

            for (var index = 0; index < chunks.Count; index++)
            {
                chunks[index].AttachEmbedding(vectors[index]);
            }

            await _texts.AddAsync(documentText);
            await _chunks.AddRangeAsync(chunks);

            document.MarkCompleted();

            // One save for text, chunks and status together. Every repository shares this scope's
            // DbContext, so they commit as a unit: a document can never read Completed while its
            // chunks are missing.
            await _documents.SaveChangesAsync();

            _logger.LogInformation(
                "Processed document {DocumentId}: {Pages} pages, {Words} words, {Chunks} chunks, "
                    + "{Vectors} vectors of {Dimensions} dimensions",
                documentId, documentText.PageCount, documentText.WordCount, chunks.Count,
                vectors.Count, _embeddings.Dimensions);
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
                NoTextLayerException or UnreadablePdfException or AiUnavailableAppException => ex.Message,
                FileNotFoundException => "The stored file is no longer available.",
                _ => "Something went wrong while processing this document."
            };

            _logger.LogError(ex, "Processing failed for document {DocumentId}", documentId);

            document.MarkFailed(message);
            await _documents.SaveChangesAsync();
        }
    }
}
