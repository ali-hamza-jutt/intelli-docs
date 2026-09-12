using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DocuMind.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocuMind.Infrastructure.Storage;

/// <summary>
/// Signed direct-to-Cloudinary uploads.
///
/// The file never passes through this API. The browser asks for a ticket, uploads straight to
/// Cloudinary, then tells us the public id — at which point we ask Cloudinary what actually
/// landed rather than believing the browser.
/// </summary>
public class CloudinaryDirectUploadService : IDirectUploadService
{
    /// <summary>
    /// PDFs are stored as "raw" so Cloudinary preserves the bytes exactly. The "image" resource
    /// type would let it rasterise pages, which is useful for thumbnails and wrong for a file we
    /// need to extract text from later.
    /// </summary>
    private const string ResourceType = "raw";

    private readonly CloudinaryOptions _options;
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryDirectUploadService> _logger;

    public CloudinaryDirectUploadService(
        IOptions<StorageOptions> options,
        ILogger<CloudinaryDirectUploadService> logger)
    {
        _options = options.Value.Cloudinary;
        _logger = logger;

        if (!_options.IsConfigured)
        {
            throw new InvalidOperationException(
                "Cloudinary is selected as the storage provider but is not configured. Set " +
                "Storage:Cloudinary:CloudName, :ApiKey and :ApiSecret (the secret via user secrets).");
        }

        _cloudinary = new Cloudinary(new Account(
            _options.CloudName,
            _options.ApiKey,
            _options.ApiSecret));
    }

    public UploadTicket CreateTicket(Guid userId, string fileName)
    {
        // The folder carries the owner, so a public id is self-describing: documind/<user>/<guid>.
        // BelongsToUser reads that back, which is what stops one user confirming another's asset.
        var folder = FolderFor(userId);
        var publicId = $"{folder}/{Guid.NewGuid():N}";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Cloudinary's scheme: take every parameter the client will send except file, api_key,
        // resource_type and cloud_name; sort by key; join as k=v&k=v; append the api_secret;
        // SHA-1 the result. The SDK does exactly this — hand-rolling it invites a subtle bug in
        // the one place a bug is a security hole.
        var parameters = new SortedDictionary<string, object>
        {
            ["public_id"] = publicId,
            ["timestamp"] = timestamp
        };

        var signature = _cloudinary.Api.SignParameters(parameters);

        _logger.LogInformation("Issued upload ticket {PublicId} for user {UserId}", publicId, userId);

        return new UploadTicket
        {
            UploadUrl = $"https://api.cloudinary.com/v1_1/{_options.CloudName}/{ResourceType}/upload",
            ApiKey = _options.ApiKey,
            PublicId = publicId,
            Folder = folder,
            Timestamp = timestamp,
            Signature = signature,
            ResourceType = ResourceType,
            MaxFileSizeBytes = 0 // filled in by the caller, which owns the limit
        };
    }

    public async Task<VerifiedUpload?> VerifyAsync(
        string publicId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // The authoritative read. Size and format come from Cloudinary, not from the client,
            // so a client claiming a 1 KB PDF that is really a 500 MB video gets caught here.
            var result = await _cloudinary.GetResourceAsync(
                new GetResourceParams(publicId) { ResourceType = ResourceTypeConverter(ResourceType) },
                cancellationToken);

            if (result.StatusCode is System.Net.HttpStatusCode.NotFound
                || string.IsNullOrEmpty(result.PublicId))
            {
                _logger.LogWarning("Upload confirmation for unknown asset {PublicId}", publicId);
                return null;
            }

            return new VerifiedUpload(
                result.PublicId,
                result.SecureUrl ?? result.Url ?? string.Empty,
                result.Bytes,
                result.Format ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not verify Cloudinary asset {PublicId}", publicId);
            return null;
        }
    }

    public bool BelongsToUser(string publicId, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return false;
        }

        // Must sit exactly under this user's folder — a prefix check alone would let
        // "documind/<user>evil/x" through.
        var expectedPrefix = $"{FolderFor(userId)}/";

        return publicId.StartsWith(expectedPrefix, StringComparison.Ordinal)
            && !publicId[expectedPrefix.Length..].Contains('/');
    }

    private string FolderFor(Guid userId) => $"{_options.Folder}/{userId:N}";

    private static CloudinaryDotNet.Actions.ResourceType ResourceTypeConverter(string value) =>
        value switch
        {
            "raw" => CloudinaryDotNet.Actions.ResourceType.Raw,
            "video" => CloudinaryDotNet.Actions.ResourceType.Video,
            _ => CloudinaryDotNet.Actions.ResourceType.Image
        };
}
