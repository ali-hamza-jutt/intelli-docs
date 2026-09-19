using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DocuMind.Infrastructure.Ingestion;

/// <summary>
/// Drains the ingestion queue, one document at a time.
///
/// Each document is processed inside its own dependency-injection scope. That is not a detail:
/// <c>DocumentProcessor</c> and <c>DbContext</c> are scoped services, and a long-lived singleton
/// worker resolving them directly would share one change-tracking context across every document
/// it ever handled.
/// </summary>
public class IngestionWorker : BackgroundService
{
    private readonly ChannelIngestionQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IngestionWorker> _logger;

    public IngestionWorker(
        ChannelIngestionQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<IngestionWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Ingestion worker started");

        await RequeueInterruptedAsync(stoppingToken);

        try
        {
            await foreach (var documentId in _queue.ReadAllAsync(stoppingToken))
            {
                await ProcessOneAsync(documentId, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }

        _logger.LogInformation("Ingestion worker stopped");
    }

    private async Task ProcessOneAsync(Guid documentId, CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<IDocumentProcessor>();

            await processor.ProcessAsync(documentId, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutdown mid-document. The row stays in Processing and is picked up by
            // RequeueInterruptedAsync on the next start.
            _logger.LogInformation("Ingestion of {DocumentId} interrupted by shutdown", documentId);
            throw;
        }
        catch (Exception ex)
        {
            // The processor already records content failures on the document itself, so reaching
            // here means something infrastructural broke. Keep the worker alive for the next
            // document rather than killing the whole pipeline.
            _logger.LogError(ex, "Unhandled failure ingesting document {DocumentId}", documentId);
        }
    }

    /// <summary>
    /// Re-queues anything the previous run left mid-flight.
    ///
    /// The channel is in-process, so a restart loses whatever was queued. Without this, a document
    /// interrupted by a deploy would sit in Processing forever with no worker aware of it — the
    /// single worst failure mode of an in-memory queue.
    /// </summary>
    private async Task RequeueInterruptedAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var documents = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

            var stranded = await documents.GetByStatusAsync(
                [DocumentStatus.Uploaded, DocumentStatus.Processing]);

            if (stranded.Count == 0)
            {
                return;
            }

            _logger.LogWarning(
                "Found {Count} document(s) left unfinished by a previous run — re-queuing",
                stranded.Count);

            foreach (var document in stranded)
            {
                await _queue.EnqueueAsync(document.Id, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            // Recovery is best-effort. Failing here must not stop the worker from serving new
            // uploads, which are the more important case.
            _logger.LogError(ex, "Could not re-queue interrupted documents");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Stop accepting new work, then let the base implementation wait for the current document
        // to finish or for the shutdown timeout to elapse.
        _queue.Complete();

        await base.StopAsync(cancellationToken);
    }
}
