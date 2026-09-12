namespace DocuMind.Application.Interfaces;

/// <summary>
/// Issues short-lived, signed permission for the browser to upload straight to the storage
/// provider, then verifies afterwards that the asset really landed.
///
/// The browser is never trusted: the ticket pins exactly what may be uploaded and where, and
/// the verification step reads the truth back from the provider rather than from the client.
/// </summary>
public interface IDirectUploadService
{
    /// <summary>Signs an upload confined to one user's folder under a generated public id.</summary>
    UploadTicket CreateTicket(Guid userId, string fileName);

    /// <summary>
    /// Confirms the asset exists at the provider and returns its authoritative metadata.
    /// Returns null when the asset cannot be found.
    /// </summary>
    Task<VerifiedUpload?> VerifyAsync(string publicId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the public id belongs to this user's folder. Stops a caller confirming somebody
    /// else's asset, or one outside the application's namespace entirely.
    /// </summary>
    bool BelongsToUser(string publicId, Guid userId);
}

/// <summary>Everything the browser needs to POST the file itself. No secret is included.</summary>
public record UploadTicket
{
    public required string UploadUrl { get; init; }
    public required string ApiKey { get; init; }
    public required string PublicId { get; init; }
    public required string Folder { get; init; }
    public required long Timestamp { get; init; }
    public required string Signature { get; init; }
    public required string ResourceType { get; init; }
    public required long MaxFileSizeBytes { get; init; }
}

/// <summary>What the provider reports about a stored asset — the values we trust.</summary>
public record VerifiedUpload(string PublicId, string SecureUrl, long Bytes, string Format);
