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
    /// Streams the asset back from Cloudinary, through the credentialed download API rather than a
    /// delivery URL.
    ///
    /// Cloudinary accounts restrict the delivery of PDFs, so <c>res.cloudinary.com</c> answers 401
    /// for one — signed URLs included. Asking as the account instead works with that restriction
    /// rather than against it, and it is what a document store should do anyway: the file is never
    /// reachable by URL alone, so a link that escapes into a log or a browser history is not enough
    /// to read someone's document.
    /// </summary>
    public async Task<Stream> OpenAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        // Signed the same way as an upload ticket: sorted parameters, joined, secret appended.
        var parameters = new SortedDictionary<string, object>
        {
            ["public_id"] = storageKey,
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["type"] = "upload"
        };

        var query = string.Join(
            '&',
            parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value.ToString()!)}"));

        var url =
            $"https://api.cloudinary.com/v1_1/{_options.CloudName}/{ResourceType}/download" +
            $"?{query}&signature={_cloudinary.Api.SignParameters(parameters)}&api_key={_options.ApiKey}";

        var client = _httpClientFactory.CreateClient(nameof(CloudinaryFileStorageService));
        var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new FileNotFoundException($"No Cloudinary asset for key '{storageKey}'.", url);
        }

        if (!response.IsSuccessStatusCode)
        {
            // Cloudinary explains a refused delivery in the body — "Invalid signature", "Restricted
            // media type" and so on. Without it the status alone sends you guessing, and the body
            // carries no credential of ours, only its opinion of the request.
            var reason = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogError(
                "Cloudinary returned {Status} fetching {StorageKey}: {Reason}",
                (int)response.StatusCode, storageKey, reason.Trim().Length > 0
                    ? reason.Trim()[..Math.Min(200, reason.Trim().Length)]
                    : "(no body)");

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
