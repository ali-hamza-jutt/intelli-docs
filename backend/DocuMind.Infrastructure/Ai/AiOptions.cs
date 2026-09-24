using DocuMind.Domain.Entities;

namespace DocuMind.Infrastructure.Ai;

/// <summary>The providers <see cref="AiOptions.Provider"/> accepts.</summary>
public static class AiProviders
{
    public const string OpenAI = "OpenAI";
}

/// <summary>
/// Model and credential settings for the AI provider.
///
/// <see cref="EmbeddingDimensions"/> is the one value that is expensive to change: module 7 stores
/// vectors in a <c>vector(N)</c> column where N is fixed when the migration runs, so a different
/// number later means a new column, a new index, and re-embedding every chunk.
/// </summary>
public class AiOptions
{
    public const string SectionName = "AI";

    public string Provider { get; set; } = AiProviders.OpenAI;

    /// <summary>Cheapest current OpenAI embedding model, and strong enough for document retrieval.</summary>
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    /// <summary>
    /// Vector length to request. text-embedding-3 models can return a shortened vector, which
    /// trades a little accuracy for half the storage — 1536 is this model's full width.
    /// </summary>
    public int EmbeddingDimensions { get; set; } = 1536;

    /// <summary>
    /// The provider credential. Set in user secrets, never in appsettings.json:
    /// <c>dotnet user-secrets set "AI:ApiKey" "&lt;key&gt;"</c>
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Base address of the provider. Empty means OpenAI itself; set it to point at an
    /// OpenAI-compatible endpoint instead, such as a gateway, a local model server, or a stub
    /// while testing.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Whether a credential is present. Nothing can be embedded without one.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    /// <summary>
    /// Returns a message for each problem, or nothing if the settings are usable. The API key is
    /// deliberately not required here: a missing key fails the documents that need embedding
    /// instead of stopping the whole application from starting.
    /// </summary>
    public IEnumerable<string> Validate()
    {
        if (!string.Equals(Provider, AiProviders.OpenAI, StringComparison.OrdinalIgnoreCase))
        {
            yield return
                $"AI:Provider '{Provider}' is not supported — only '{AiProviders.OpenAI}' is implemented.";
        }

        if (string.IsNullOrWhiteSpace(EmbeddingModel))
        {
            yield return "AI:EmbeddingModel must name an embedding model.";
        }

        // Not a range check: the column chunks are stored in is declared at exactly this width, so
        // any other value would fail on the first insert. Caught at startup instead.
        if (EmbeddingDimensions != DocumentChunk.EmbeddingDimensions)
        {
            yield return
                $"AI:EmbeddingDimensions must be {DocumentChunk.EmbeddingDimensions} to match the "
                + $"vector column chunks are stored in (was {EmbeddingDimensions}). Changing it "
                + "needs a migration and re-embedding every chunk.";
        }

        if (Endpoint.Length > 0 && !Uri.TryCreate(Endpoint, UriKind.Absolute, out _))
        {
            yield return $"AI:Endpoint must be an absolute URL (was '{Endpoint}').";
        }
    }
}
