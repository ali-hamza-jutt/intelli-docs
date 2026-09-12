using System.ComponentModel.DataAnnotations;

namespace DocuMind.Application.DTOs.Documents;

public class UploadTicketRequest
{
    [Required, StringLength(260, MinimumLength = 1)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>Checked against the limit up front so a large file is refused before uploading.</summary>
    [Range(1, long.MaxValue, ErrorMessage = "The file appears to be empty.")]
    public long FileSize { get; set; }
}

/// <summary>Everything the browser needs to POST the file to the provider. Carries no secret.</summary>
public class UploadTicketResponse
{
    public required string UploadUrl { get; set; }
    public required string ApiKey { get; set; }
    public required string PublicId { get; set; }
    public required long Timestamp { get; set; }
    public required string Signature { get; set; }
    public required string ResourceType { get; set; }
    public required long MaxFileSizeBytes { get; set; }
}

/// <summary>Sent back after the browser's upload finishes, to register the document.</summary>
public class ConfirmUploadRequest
{
    [Required, StringLength(400)]
    public string PublicId { get; set; } = string.Empty;

    [Required, StringLength(260, MinimumLength = 1)]
    public string FileName { get; set; } = string.Empty;
}
