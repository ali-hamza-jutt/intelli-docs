using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DocuMind.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocuMind.Infrastructure.Storage;

/// <summary>
/// Reads and deletes assets that the browser uploaded directly. Saving is not supported here —
/// with direct upload the API never holds the bytes, which is the point of the arrangement.
/// </summary>
public class CloudinaryFileStorageService : IFileStorageService
{
    private const string ResourceType = "raw";

    private readonly CloudinaryOptions _options;
    private readonly Cloudinary _cloudinary;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CloudinaryFileStorageService> _logger;

    public CloudinaryFileStorageService(
        IOptions<StorageOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<CloudinaryFileStorageService> logger)
    {
        _options = options.Value.Cloudinary;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        _cloudinary = new Cloudinary(new Account(
            _options.CloudName,
            _options.ApiKey,
            _options.ApiSecret));
    }

    public Task<StoredFileResult> SaveAsync(
        Stream content,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Cloudinary uploads go straight from the browser. Use IDirectUploadService to issue " +
            "a signed ticket instead of routing the file through the API.");
    }

    /// <summary>
    /// Streams the asset back from Cloudinary. The storage key is the public id; the delivery URL
    /// is derived from it so a moved account or renamed cloud does not invalidate stored rows.
    /// </summary>
    public async Task<Stream> OpenAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var url = $"https://res.cloudinary.com/{_options.CloudName}/{ResourceType}/upload/{storageKey}";

        var client = _httpClientFactory.CreateClient(nameof(CloudinaryFileStorageService));
        var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new FileNotFoundException($"No Cloudinary asset for key '{storageKey}'.", url);
        }

        if (!response.IsSuccessStatusCode)
        {
            // A 401 here usually means the account restricts raw/PDF delivery — a setting in the
            // Cloudinary console, not a bug in this code.
            _logger.LogError(
                "Cloudinary returned {Status} fetching {StorageKey}. If this is 401, check that " +
                "raw/PDF delivery is enabled for the account.",
                (int)response.StatusCode, storageKey);

            throw new InvalidOperationException(
                $"Cloudinary returned {(int)response.StatusCode} for '{storageKey}'.");
        }

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _cloudinary.DestroyAsync(new DeletionParams(storageKey)
            {
                ResourceType = CloudinaryDotNet.Actions.ResourceType.Raw
            });

            _logger.LogInformation(
                "Cloudinary delete for {StorageKey} returned {Result}", storageKey, result.Result);
        }
        catch (Exception ex)
        {
            // Best-effort, matching the local provider: a leftover asset is cheaper than failing
            // the user's delete.
            _logger.LogWarning(ex, "Could not delete Cloudinary asset {StorageKey}", storageKey);
        }
    }
}
