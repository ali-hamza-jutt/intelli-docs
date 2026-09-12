namespace DocuMind.Domain.Entities;

/// <summary>
/// Where a document sits in the ingestion pipeline. Stored as an int, so the order of these
/// members must not change once rows exist.
/// </summary>
public enum DocumentStatus
{
    /// <summary>Stored on disk, nothing read from it yet.</summary>
    Uploaded = 0,

    /// <summary>Extraction, chunking or embedding is running.</summary>
    Processing = 1,

    /// <summary>Fully ingested and available for retrieval.</summary>
    Completed = 2,

    /// <summary>Ingestion failed; <see cref="Document.ErrorMessage"/> says why.</summary>
    Failed = 3
}
