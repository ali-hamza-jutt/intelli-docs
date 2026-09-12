namespace DocuMind.Application.Interfaces;

/// <summary>
/// Blob storage for uploaded files. Keys are opaque — nothing outside the implementation parses
/// or builds one — so local disk can be swapped for S3 or Azure Blob without touching this layer.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Persists the stream under a generated name and returns the key to store on the document,
    /// along with the generated file name for display.
    /// </summary>
    Task<StoredFileResult> SaveAsync(
        Stream content,
        string originalFileName,
        CancellationToken cancellationToken = default);

    /// <summary>Opens a previously saved file. Throws if the key is unknown.</summary>
    Task<Stream> OpenAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>Best-effort delete; succeeds silently when the key is already gone.</summary>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}

public record StoredFileResult(string StoredFileName, string StorageKey, long SizeBytes);
