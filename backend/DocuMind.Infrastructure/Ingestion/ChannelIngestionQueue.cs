using System.Threading.Channels;
using DocuMind.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocuMind.Infrastructure.Ingestion;

/// <summary>
/// In-process work queue backing document ingestion.
///
/// Adequate for a single API instance. Two limitations are deliberate and documented rather than
/// hidden: queued ids are lost if the process stops, and the queue does not span instances. The
/// worker compensates for the first by re-queuing anything left mid-flight on startup; the second
/// needs a durable broker, which is why callers only ever pass an id.
/// </summary>
public class ChannelIngestionQueue : IIngestionQueue
{
    /// <summary>
    /// Bounded on purpose. An unbounded channel turns a flood of uploads into unbounded memory
    /// growth; a bounded one applies backpressure instead, and the wait is what signals overload.
    /// </summary>
    private const int Capacity = 1000;

    private readonly Channel<Guid> _channel;
    private readonly ILogger<ChannelIngestionQueue> _logger;

    public ChannelIngestionQueue(ILogger<ChannelIngestionQueue> logger)
    {
        _logger = logger;

        _channel = Channel.CreateBounded<Guid>(new BoundedChannelOptions(Capacity)
        {
            // One worker drains it; many request threads write to it.
            SingleReader = true,
            SingleWriter = false,

            // Wait rather than drop. Dropping a document silently would leave it stuck in
            // Uploaded forever with nothing to explain why.
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public int PendingCount => _channel.Reader.Count;

    public async ValueTask EnqueueAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(documentId, cancellationToken);

        _logger.LogInformation(
            "Queued document {DocumentId} for ingestion ({Pending} pending)",
            documentId, PendingCount);
    }

    /// <summary>Consumed by the worker. Completes when the channel is closed at shutdown.</summary>
    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);

    /// <summary>Stops accepting new work so the worker can drain what is already queued.</summary>
    public void Complete() => _channel.Writer.TryComplete();
}


