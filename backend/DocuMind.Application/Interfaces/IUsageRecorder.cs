using DocuMind.Domain.Entities;

namespace DocuMind.Application.Interfaces;

/// <summary>
/// Records what a user spent at the AI provider.
///
/// Called from the paths that make provider calls rather than from the provider clients themselves,
/// because only the caller knows whose work it was: ingestion runs on a background thread with no
/// signed-in user, and the owner has to be passed in explicitly.
/// </summary>
public interface IUsageRecorder
{
    /// <summary>
    /// Never throws. Losing a usage row is a reporting gap; failing a user's question over one would
    /// be a fault, and the answer has already been paid for by then either way.
    /// </summary>
    Task RecordAsync(
        Guid userId,
        UsageKind kind,
        string model,
        int inputTokens,
        int outputTokens,
        int items,
        CancellationToken cancellationToken = default);

    /// <summary>Totals for one user since a point in time, for showing them what they have used.</summary>
    Task<UsageSummary> SummariseAsync(
        Guid userId,
        DateTime since,
        CancellationToken cancellationToken = default);
}

/// <summary>Totals by kind, plus the calls behind them.</summary>
public record UsageSummary(
    DateTime Since,
    int EmbeddingCalls,
    int EmbeddingInputTokens,
    int ChatCalls,
    int ChatInputTokens,
    int ChatOutputTokens);
