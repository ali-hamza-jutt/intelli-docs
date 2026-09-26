using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using DocuMind.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocuMind.Infrastructure.Repositories;

public class UsageRecorder : IUsageRecorder
{
    private readonly DocuMindDbContext _context;
    private readonly ILogger<UsageRecorder> _logger;

    public UsageRecorder(DocuMindDbContext context, ILogger<UsageRecorder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RecordAsync(
        Guid userId,
        UsageKind kind,
        string model,
        int inputTokens,
        int outputTokens,
        int items,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.UsageRecords.AddAsync(
                UsageRecord.Create(userId, kind, model, inputTokens, outputTokens, items),
                cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Deliberately swallowed: the provider has already been paid, the user has their answer,
            // and a bookkeeping failure must not turn that into an error they see.
            _logger.LogError(exception, "Could not record {Kind} usage for user {UserId}", kind, userId);
        }
    }

    public async Task<UsageSummary> SummariseAsync(
        Guid userId,
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        // Grouped in the database: a busy month is thousands of rows and none of them need loading.
        var totals = await _context.UsageRecords
            .AsNoTracking()
            .Where(record => record.UserId == userId && record.CreatedAt >= since)
            .GroupBy(record => record.Kind)
            .Select(group => new
            {
                Kind = group.Key,
                Calls = group.Count(),
                InputTokens = group.Sum(record => record.InputTokens),
                OutputTokens = group.Sum(record => record.OutputTokens)
            })
            .ToListAsync(cancellationToken);

        var embedding = totals.FirstOrDefault(total => total.Kind == UsageKind.Embedding);
        var chat = totals.FirstOrDefault(total => total.Kind == UsageKind.Chat);

        return new UsageSummary(
            since,
            embedding?.Calls ?? 0,
            embedding?.InputTokens ?? 0,
            chat?.Calls ?? 0,
            chat?.InputTokens ?? 0,
            chat?.OutputTokens ?? 0);
    }
}
