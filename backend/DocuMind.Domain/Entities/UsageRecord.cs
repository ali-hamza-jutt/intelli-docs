namespace DocuMind.Domain.Entities;

/// <summary>What a provider call was for. Embedding and answering are billed at very different rates.</summary>
public enum UsageKind
{
    Embedding = 0,
    Chat = 1
}

/// <summary>
/// One billable call to an AI provider, attributed to the user who caused it.
///
/// Recorded from what the provider reported, never estimated: this is the basis for telling someone
/// what they have used, and a guess dressed as a number would be worse than no number. A call whose
/// usage the provider did not report is still recorded, with zeros, so the count of calls stays true.
/// </summary>
public class UsageRecord
{
    private UsageRecord() { }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public UsageKind Kind { get; private set; }

    public string Model { get; private set; } = null!;

    public int InputTokens { get; private set; }

    public int OutputTokens { get; private set; }

    /// <summary>How many texts or questions the call covered — one chat, or a batch of chunks.</summary>
    public int Items { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static UsageRecord Create(
        Guid userId,
        UsageKind kind,
        string model,
        int inputTokens,
        int outputTokens,
        int items)
    {
        return new UsageRecord
        {
            UserId = userId,
            Kind = kind,
            Model = model,
            InputTokens = Math.Max(0, inputTokens),
            OutputTokens = Math.Max(0, outputTokens),
            Items = Math.Max(0, items),
            CreatedAt = DateTime.UtcNow
        };
    }
}
