namespace DocuMind.Application.DTOs.Usage;

/// <summary>
/// What the signed-in user has spent at the AI provider since a given moment.
///
/// Tokens rather than money: prices change and depend on the account, so converting here would be a
/// number that quietly goes stale. This is the honest measure, and the groundwork for pricing later.
/// </summary>
public class UsageResponse
{
    public required DateTime Since { get; set; }

    /// <summary>Calls made while indexing documents, and the tokens they read.</summary>
    public required int EmbeddingCalls { get; set; }

    public required int EmbeddingInputTokens { get; set; }

    /// <summary>Questions answered, and the tokens read and written for them.</summary>
    public required int ChatCalls { get; set; }

    public required int ChatInputTokens { get; set; }

    public required int ChatOutputTokens { get; set; }
}
