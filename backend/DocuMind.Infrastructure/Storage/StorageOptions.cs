namespace DocuMind.Infrastructure.Storage;

public class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>"Local" or "Cloudinary". Local keeps development working without credentials.</summary>
    public string Provider { get; set; } = StorageProviders.Local;

    /// <summary>20 MB by default. Enforced by the API and re-checked against Cloudinary's report.</summary>
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;

    /// <summary>
    /// Left empty on purpose: configuration binding <i>appends</i> to a collection that already
    /// has values, so a non-empty default would produce duplicates. The fallback is applied in
    /// AddFileStorage instead.
    /// </summary>
    public string[] AllowedExtensions { get; set; } = [];

    /// <summary>Applied when configuration supplies no extensions at all.</summary>
    public static readonly string[] DefaultExtensions = [".pdf"];

    public LocalStorageOptions Local { get; set; } = new();

    public CloudinaryOptions Cloudinary { get; set; } = new();
}

public static class StorageProviders
{
    public const string Local = "Local";
    public const string Cloudinary = "Cloudinary";
}

public class LocalStorageOptions
{
    /// <summary>Absolute, or relative to the API's content root.</summary>
    public string UploadDirectory { get; set; } = "uploads";
}

public class CloudinaryOptions
{
    public string CloudName { get; set; } = string.Empty;

    /// <summary>Public — it is sent to the browser with every upload ticket.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Secret. Set via user secrets or an environment variable, never appsettings.</summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>Root folder for every uploaded document.</summary>
    public string Folder { get; set; } = "documind";

    /// <summary>
    /// How long a signed upload ticket stays usable. Short, because the browser should upload
    /// immediately after asking for one.
    /// </summary>
    public int TicketLifetimeSeconds { get; set; } = 600;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(CloudName)
        && !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(ApiSecret);
}
