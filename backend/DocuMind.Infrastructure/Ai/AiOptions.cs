using DocuMind.Domain.Entities;

namespace DocuMind.Infrastructure.Ai;

/// <summary>
/// The wire protocols <see cref="AiOptions.Provider"/> accepts.
///
/// This names the shape of the API, not the company behind it. "OpenAI" covers every service that
/// speaks the OpenAI protocol — OpenAI itself, Google's Gemini through its compatibility endpoint,
/// Azure OpenAI, a gateway, or a model running on this machine. Which one is in use is decided by
/// <see cref="AiOptions.Endpoint"/>, not here.
/// </summary>
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

    /// <summary>
    /// The embedding model. Gemini serves this one on its free tier, and its OpenAI-compatible
    /// endpoint accepts the same request this app already sends.
    /// </summary>
    public string EmbeddingModel { get; set; } = "gemini-embedding-001";

    /// <summary>
    /// Vector length to request. A model that supports several widths is asked for this one, which
    /// is what lets the provider change without rebuilding the column the vectors live in. A model
    /// that answers with a different width is refused by name rather than stored.
    /// </summary>
    public int EmbeddingDimensions { get; set; } = 1536;

    /// <summary>
    /// The model that writes answers. A small, cheap model is enough here: the facts come from the
    /// retrieved passages, and the model's job is to read them and reply, not to know things.
    /// </summary>
    public string ChatModel { get; set; } = "gemini-3.5-flash-lite";

    /// <summary>
    /// The provider credential. Set in user secrets, never in appsettings.json:
    /// <c>dotnet user-secrets set "AI:ApiKey" "&lt;key&gt;"</c>
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Base address of the provider, which is what decides whose models answer. Empty means OpenAI
    /// itself; the configured value points at Gemini's OpenAI-compatible endpoint, and it can
    /// equally point at Azure OpenAI, a gateway, a model running on this machine, or a test stub.
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

        if (string.IsNullOrWhiteSpace(ChatModel))
        {
            yield return "AI:ChatModel must name a chat model.";
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
