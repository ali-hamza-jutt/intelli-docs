namespace DocuMind.Domain.Entities;

public class Document
{
    private Document() { }

    public Guid Id { get; private set; }

    /// <summary>Owner. Every read of a document must be filtered by this.</summary>
    public Guid UserId { get; private set; }

    /// <summary>The name the user sees. Starts as the uploaded filename and can be renamed.</summary>
    public string FileName { get; private set; } = null!;

    /// <summary>Exactly what the browser sent, kept for display and download.</summary>
    public string OriginalFileName { get; private set; } = null!;

    /// <summary>
    /// The generated name on disk. Never derived from user input, so a crafted filename cannot
    /// steer where the file lands.
    /// </summary>
    public string StoredFileName { get; private set; } = null!;

    /// <summary>Opaque storage key. Only the storage implementation interprets it.</summary>
    public string FilePath { get; private set; } = null!;

    public string ContentType { get; private set; } = null!;

    public long FileSize { get; private set; }

    public DocumentStatus Status { get; private set; }

    /// <summary>Set when <see cref="Status"/> is Failed, cleared on a retry.</summary>
    public string? ErrorMessage { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    /// <summary>When ingestion finished, successfully or not.</summary>
    public DateTime? ProcessedAt { get; private set; }

    public static Document Upload(
        Guid userId,
        string originalFileName,
        string storedFileName,
        string filePath,
        string contentType,
        long fileSize)
    {
        return new Document
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FileName = originalFileName,
            OriginalFileName = originalFileName,
            StoredFileName = storedFileName,
            FilePath = filePath,
            ContentType = contentType,
            FileSize = fileSize,
            Status = DocumentStatus.Uploaded,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkProcessing()
    {
        Status = DocumentStatus.Processing;
        ErrorMessage = null;
        Touch();
    }

    public void MarkCompleted()
    {
        Status = DocumentStatus.Completed;
        ErrorMessage = null;
        ProcessedAt = DateTime.UtcNow;
        Touch();
    }

    public void MarkFailed(string errorMessage)
    {
        Status = DocumentStatus.Failed;

        // Bounded so a provider stack trace cannot overflow the column.
        ErrorMessage = errorMessage.Length > 1000
            ? errorMessage[..1000]
            : errorMessage;

        ProcessedAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>Puts a failed document back in the queue without re-uploading the file.</summary>
    public void ResetForRetry()
    {
        Status = DocumentStatus.Uploaded;
        ErrorMessage = null;
        ProcessedAt = null;
        Touch();
    }

    public void Rename(string fileName)
    {
        FileName = fileName.Trim();
        Touch();
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
