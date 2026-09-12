using DocuMind.Application.Common;
using DocuMind.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace DocuMind.Infrastructure.Storage;

/// <summary>
/// Gate-keeps uploads. The important rule is that neither the filename nor the browser's declared
/// Content-Type is trusted — both are attacker-controlled.
///
/// When the API holds the stream, the file's leading bytes decide what it actually is. With
/// direct-to-provider upload there is no stream here, so only the name and size can be checked
/// up front and the provider's own report is verified afterwards.
/// </summary>
public class FileValidator : IFileValidator
{
    /// <summary>
    /// Magic numbers keyed by extension. A PDF always begins "%PDF-", so an executable renamed
    /// to .pdf fails here rather than reaching the extraction stage.
    /// </summary>
    private static readonly Dictionary<string, byte[]> Signatures =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "%PDF-"u8.ToArray()
        };

    private static readonly Dictionary<string, string> ContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf"
        };

    private readonly StorageOptions _options;

    public FileValidator(IOptions<StorageOptions> options)
    {
        _options = options.Value;
    }

    public long MaxFileSizeBytes => _options.MaxFileSizeBytes;

    public void ValidateName(string fileName)
    {
        // Path.GetFileName strips any directory portion, so "../../etc/passwd" becomes "passwd"
        // before the extension is even read.
        var safeName = Path.GetFileName(fileName ?? string.Empty);

        if (string.IsNullOrWhiteSpace(safeName))
        {
            throw new ValidationAppException("A file name is required.", "FILE_NAME_REQUIRED");
        }

        var extension = Path.GetExtension(safeName).ToLowerInvariant();

        if (string.IsNullOrEmpty(extension) ||
            !_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            var allowed = string.Join(", ", _options.AllowedExtensions);

            throw new ValidationAppException(
                $"'{extension}' files are not supported. Allowed types: {allowed}.",
                "FILE_TYPE_NOT_SUPPORTED");
        }
    }

    public void ValidateSize(long sizeBytes)
    {
        if (sizeBytes <= 0)
        {
            throw new ValidationAppException("The file is empty.", "FILE_EMPTY");
        }

        if (sizeBytes > _options.MaxFileSizeBytes)
        {
            var limitMb = _options.MaxFileSizeBytes / 1024d / 1024d;
            var actualMb = sizeBytes / 1024d / 1024d;

            throw new ValidationAppException(
                $"The file is {actualMb:F1} MB; the limit is {limitMb:F0} MB.",
                "FILE_TOO_LARGE");
        }
    }

    public async Task ValidateAsync(Stream content, string fileName, string? declaredContentType)
    {
        ValidateName(fileName);
        ValidateSize(content.Length);

        await VerifySignatureAsync(content, Path.GetExtension(Path.GetFileName(fileName)).ToLowerInvariant());
    }

    public string ResolveContentType(string fileName)
    {
        var extension = Path.GetExtension(Path.GetFileName(fileName)).ToLowerInvariant();

        return ContentTypes.TryGetValue(extension, out var contentType)
            ? contentType
            : "application/octet-stream";
    }

    private static async Task VerifySignatureAsync(Stream content, string extension)
    {
        if (!Signatures.TryGetValue(extension, out var expected))
        {
            // An allowed extension with no signature registered would silently skip the check,
            // which is exactly the hole this guard exists to prevent.
            throw new ValidationAppException(
                $"No content check is configured for '{extension}' files.",
                "FILE_TYPE_NOT_VERIFIABLE");
        }

        var actual = new byte[expected.Length];
        var originalPosition = content.Position;

        content.Position = 0;
        var read = await content.ReadAtLeastAsync(actual, expected.Length, throwOnEndOfStream: false);
        content.Position = originalPosition;

        if (read < expected.Length || !actual.SequenceEqual(expected))
        {
            throw new ValidationAppException(
                "That file is not a valid PDF. It may be corrupt, or renamed from another format.",
                "FILE_CONTENT_MISMATCH");
        }
    }
}
