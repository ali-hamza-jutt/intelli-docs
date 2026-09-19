namespace DocuMind.Application.Interfaces;

/// <summary>
/// Hands a document to background processing so the HTTP request that uploaded it can return
/// immediately.
///
/// Work is addressed by id rather than by passing a payload, which is what lets the in-process
/// implementation be swapped for a durable broker later without changing a single caller.
/// </summary>
public interface IIngestionQueue
{
    /// <summary>
    /// Queues a document for ingestion. Returns as soon as it is accepted, not when it is done.
    /// </summary>
    ValueTask EnqueueAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>How many documents are waiting. Diagnostics only — never branch on it.</summary>
    int PendingCount { get; }
}
