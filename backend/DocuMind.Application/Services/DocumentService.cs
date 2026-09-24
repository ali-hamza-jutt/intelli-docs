using DocuMind.Application.Common;
using DocuMind.Application.DTOs.Documents;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DocuMind.Application.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _repository;
    private readonly IFileStorageService _storage;
    private readonly IFileValidator _validator;

    /// <summary>Null when the configured provider does not support direct upload.</summary>
    private readonly IDirectUploadService? _directUpload;
    private readonly IDocumentTextRepository _texts;
    private readonly IDocumentChunkRepository _chunks;
    private readonly ITextChunker _chunker;
    private readonly IIngestionQueue _queue;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IDocumentRepository repository,
        IFileStorageService storage,
        IFileValidator validator,
        ICurrentUser currentUser,
        ILogger<DocumentService> logger,
        IDocumentTextRepository texts,
        IDocumentChunkRepository chunks,
        ITextChunker chunker,
        IIngestionQueue queue,
        IDirectUploadService? directUpload = null)
    {
        _texts = texts;
        _chunks = chunks;
        _chunker = chunker;
        _queue = queue;
        _repository = repository;
        _storage = storage;
        _validator = validator;
        _directUpload = directUpload;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<DocumentResponse> UploadAsync(
        UploadDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        // The owner comes from the bearer token, never from the request.
        var userId = _currentUser.RequireUserId();

        await _validator.ValidateAsync(request.Content, request.FileName, request.DeclaredContentType);

        var stored = await _storage.SaveAsync(request.Content, request.FileName, cancellationToken);

        Document document;

        try
        {
            document = Document.Upload(
                userId,
                originalFileName: Path.GetFileName(request.FileName),
                storedFileName: stored.StoredFileName,
                filePath: stored.StorageKey,
                contentType: _validator.ResolveContentType(request.FileName),
                fileSize: stored.SizeBytes);

            await _repository.AddAsync(document);
            await _repository.SaveChangesAsync();

            _logger.LogInformation(
                "Document {DocumentId} uploaded by {UserId} ({SizeBytes} bytes)",
                document.Id, userId, stored.SizeBytes);
        }
        catch
        {
            // The row never landed, so the file would be unreachable. Remove it rather than
            // leaving storage to accumulate orphans.
            await _storage.DeleteAsync(stored.StorageKey, CancellationToken.None);
            throw;
        }

        // Queued rather than run here: extraction and embedding take seconds to minutes, and the
        // caller should not hold a connection open for it. The document is returned with status
        // Uploaded and the client polls until it settles.
        await _queue.EnqueueAsync(document.Id, cancellationToken);

        return MapToResponse(document);
    }

    public UploadTicketResponse CreateUploadTicket(UploadTicketRequest request)
    {
        var userId = _currentUser.RequireUserId();

        // Validate before checking capability, so a rejected file reports why it was rejected
        // rather than which provider happens to be configured.
        _validator.ValidateName(request.FileName);
        _validator.ValidateSize(request.FileSize);

        if (_directUpload is null)
        {
            throw new ValidationAppException(
                "Direct upload is not available with the current storage provider.",
                "DIRECT_UPLOAD_UNAVAILABLE");
        }

        var ticket = _directUpload.CreateTicket(userId, request.FileName);

        return new UploadTicketResponse
        {
            UploadUrl = ticket.UploadUrl,
            ApiKey = ticket.ApiKey,
            PublicId = ticket.PublicId,
            Timestamp = ticket.Timestamp,
            Signature = ticket.Signature,
            ResourceType = ticket.ResourceType,
            MaxFileSizeBytes = _validator.MaxFileSizeBytes
        };
    }

    public async Task<DocumentResponse> ConfirmUploadAsync(
        ConfirmUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.RequireUserId();

        if (_directUpload is null)
        {
            throw new ValidationAppException(
                "Direct upload is not available with the current storage provider.",
                "DIRECT_UPLOAD_UNAVAILABLE");
        }

        _validator.ValidateName(request.FileName);

        // The public id encodes its owner. Without this check a caller could confirm an asset
        // uploaded by somebody else and claim it as their own document.
        if (!_directUpload.BelongsToUser(request.PublicId, userId))
        {
            _logger.LogWarning(
                "User {UserId} tried to confirm public id {PublicId} outside their folder",
                userId, request.PublicId);

            throw new NotFoundAppException("That upload could not be found.", "UPLOAD_NOT_FOUND");
        }

        // The authoritative read: size and format come from the provider, never the browser.
        var verified = await _directUpload.VerifyAsync(request.PublicId, cancellationToken)
            ?? throw new NotFoundAppException("That upload could not be found.", "UPLOAD_NOT_FOUND");

        try
        {
            _validator.ValidateSize(verified.Bytes);
        }
        catch
        {
            // Registering it would leave an oversized asset billed to the account with no row
            // pointing at it, so remove it before rejecting.
            await _storage.DeleteAsync(request.PublicId, CancellationToken.None);
            throw;
        }

        var document = Document.Upload(
            userId,
            originalFileName: Path.GetFileName(request.FileName),
            storedFileName: verified.PublicId.Split('/').Last(),
            filePath: verified.PublicId,
            contentType: _validator.ResolveContentType(request.FileName),
            fileSize: verified.Bytes);

        await _repository.AddAsync(document);
        await _repository.SaveChangesAsync();

        _logger.LogInformation(
            "Document {DocumentId} registered from direct upload {PublicId} ({Bytes} bytes)",
            document.Id, verified.PublicId, verified.Bytes);

        await _queue.EnqueueAsync(document.Id, cancellationToken);

        return MapToResponse(document);
    }

    public async Task<List<DocumentResponse>> GetAllAsync()
    {
        var userId = _currentUser.RequireUserId();
        var documents = await _repository.GetAllForUserAsync(userId);

        return documents
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<DocumentResponse?> GetByIdAsync(Guid id)
    {
        var document = await LoadOwnedAsync(id);

        // A document owned by someone else is indistinguishable from one that does not exist.
        return document is null
            ? null
            : MapToResponse(document);
    }

    public async Task<DocumentStatusResponse?> GetStatusAsync(Guid id)
    {
        var document = await LoadOwnedAsync(id);

        if (document is null)
        {
            return null;
        }

        return new DocumentStatusResponse
        {
            Id = document.Id,
            Status = document.Status.ToString(),
            ErrorMessage = document.ErrorMessage,
            ProcessedAt = document.ProcessedAt
        };
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await LoadOwnedAsync(id);

        if (document is null)
        {
            return false;
        }

        await _repository.DeleteAsync(document);
        await _repository.SaveChangesAsync();

        // Only after the row is gone: a failed delete here leaves a harmless orphan, whereas
        // deleting first would risk a missing file for a document that still exists.
        await _storage.DeleteAsync(document.FilePath, cancellationToken);

        _logger.LogInformation("Document {DocumentId} deleted", document.Id);

        return true;
    }

    public async Task<DocumentTextResponse?> GetTextAsync(Guid id)
    {
        // Ownership first: the text is the document's content, so it needs the same protection.
        var document = await LoadOwnedAsync(id);

        if (document is null)
        {
            return null;
        }

        var text = await _texts.GetWithPagesAsync(id);

        if (text is null)
        {
            return null;
        }

        return new DocumentTextResponse
        {
            DocumentId = document.Id,
            PageCount = text.PageCount,
            WordCount = text.WordCount,
            CharacterCount = text.CharacterCount,
            ExtractedAt = text.CreatedAt,
            Pages = [.. text.Pages.Select(page => new DocumentPageResponse
            {
                PageNumber = page.PageNumber,
                Text = page.Text,
                WordCount = page.WordCount
            })]
        };
    }

    public async Task<DocumentChunksResponse?> GetChunksAsync(Guid id, int offset, int limit)
    {
        // Ownership first — chunks are the document's content and need the same protection.
        var document = await LoadOwnedAsync(id);

        if (document is null)
        {
            return null;
        }

        var total = await _chunks.CountForDocumentAsync(id);

        // Fetch one chunk before the requested page, so the first chunk on it can still report
        // how much it overlaps its predecessor. That extra chunk is not returned.
        var fetchFrom = Math.Max(0, offset - 1);
        var fetched = await _chunks.GetForDocumentAsync(id, fetchFrom, limit + (offset - fetchFrom));

        var page = new List<DocumentChunkResponse>(limit);

        for (var i = offset - fetchFrom; i < fetched.Count; i++)
        {
            var chunk = fetched[i];
            var previous = i > 0 ? fetched[i - 1] : null;

            page.Add(new DocumentChunkResponse
            {
                Index = chunk.ChunkIndex,
                PageNumber = chunk.PageNumber,
                EndPageNumber = chunk.EndPageNumber,
                CharacterCount = chunk.CharacterCount,
                TokenEstimate = chunk.TokenEstimate,
                OverlapWithPrevious = previous is null ? 0 : MeasureOverlap(previous.Text, chunk.Text),
                Text = chunk.Text
            });
        }

        return new DocumentChunksResponse
        {
            DocumentId = document.Id,
            TotalCount = total,
            Offset = offset,
            Limit = limit,
            ChunkSize = _chunker.ChunkSize,
            ChunkOverlap = _chunker.ChunkOverlap,
            Chunks = page
        };
    }

    /// <summary>
    /// Length of the text a chunk repeats from the end of its predecessor.
    ///
    /// The chunker always follows a carried overlap with a paragraph separator, so a genuine
    /// overlap is a prefix of <paramref name="current"/> that is also a suffix of
    /// <paramref name="previous"/> and is immediately followed by whitespace. Requiring that last
    /// part stops a coincidental one-character match being reported as overlap.
    /// </summary>
    private int MeasureOverlap(string previous, string current)
    {
        var longest = Math.Min(_chunker.ChunkOverlap, Math.Min(previous.Length, current.Length));

        for (var length = longest; length > 0; length--)
        {
            var followedByBreak = length == current.Length || char.IsWhiteSpace(current[length]);

            if (followedByBreak && previous.EndsWith(current[..length], StringComparison.Ordinal))
            {
                return length;
            }
        }

        return 0;
    }

    public async Task<bool> ReprocessAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await LoadOwnedAsync(id);

        if (document is null)
        {
            return false;
        }

        // Already queued or running: it will be processed with current settings anyway. Queuing
        // it again would run the whole pipeline once per click — harmless to the data, since each
        // run replaces the last, but once embedding is added every extra run is a paid API call.
        if (document.Status is DocumentStatus.Uploaded or DocumentStatus.Processing)
        {
            _logger.LogInformation(
                "Reprocess of {DocumentId} ignored: already {Status}", document.Id, document.Status);

            return true;
        }

        // Clear the previous failure and re-queue. The processor discards any earlier extraction
        // before writing a new one, so a retry cannot leave two copies behind.
        document.ResetForRetry();
        await _repository.SaveChangesAsync();

        await _queue.EnqueueAsync(document.Id, cancellationToken);
        return true;
    }

    public async Task<DocumentDownload?> DownloadAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var document = await LoadOwnedAsync(id);

        if (document is null)
        {
            return null;
        }

        try
        {
            var content = await _storage.OpenAsync(document.FilePath, cancellationToken);
            return new DocumentDownload(content, document.OriginalFileName, document.ContentType);
        }
        catch (FileNotFoundException)
        {
            // The row outlived its file — report it rather than returning a broken stream.
            _logger.LogError("Stored file missing for document {DocumentId}", document.Id);
            throw new NotFoundAppException("The stored file is no longer available.", "FILE_MISSING");
        }
    }

    private Task<Document?> LoadOwnedAsync(Guid id)
    {
        var userId = _currentUser.RequireUserId();
        return _repository.GetByIdForUserAsync(id, userId);
    }

    private static DocumentResponse MapToResponse(Document document)
    {
        return new DocumentResponse
        {
            Id = document.Id,
            FileName = document.FileName,
            OriginalFileName = document.OriginalFileName,
            ContentType = document.ContentType,
            FileSize = document.FileSize,
            Status = document.Status.ToString(),
            ErrorMessage = document.ErrorMessage,
            CreatedAt = document.CreatedAt,
            ProcessedAt = document.ProcessedAt
        };
    }
}
