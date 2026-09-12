namespace DocuMind.Application.Interfaces;

/// <summary>
/// Checks an upload before it is accepted. Split into separate checks because direct upload has
/// no stream to inspect at ticket time — only the name and the claimed size.
/// </summary>
public interface IFileValidator
{
    long MaxFileSizeBytes { get; }

    /// <summary>Extension allow-list and a safe file name. Throws <c>ValidationAppException</c>.</summary>
    void ValidateName(string fileName);

    /// <summary>Non-empty and within the configured limit.</summary>
    void ValidateSize(long sizeBytes);

    /// <summary>
    /// The full check, including the file's leading bytes. Only possible when the API holds the
    /// stream — that is, on the local provider's upload path.
    /// </summary>
    Task ValidateAsync(Stream content, string fileName, string? declaredContentType);

    /// <summary>The content type the system will record, derived from the file name.</summary>
    string ResolveContentType(string fileName);
}
