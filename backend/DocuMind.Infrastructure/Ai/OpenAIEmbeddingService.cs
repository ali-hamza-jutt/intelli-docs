using System.ClientModel;
using System.ClientModel.Primitives;
using DocuMind.Application.Common;
using DocuMind.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Embeddings;

namespace DocuMind.Infrastructure.Ai;

/// <summary>
/// Embeddings from OpenAI.
///
/// This is the first step in the pipeline that leaves the machine, so it is also the first that
/// fails for reasons the code cannot prevent: rate limits, timeouts, a revoked key. Every one of
/// those surfaces as <see cref="AiUnavailableAppException"/> with a message safe to show the user, so the
/// pipeline can mark the document Failed rather than leave it stuck in Processing.
///
/// Two limits shape a request: how many inputs one call may carry, and how many tokens. Long
/// documents therefore go up in several calls, and the vectors are reassembled in input order.
/// </summary>
public class OpenAIEmbeddingService : IEmbeddingService
{
    /// <summary>
    /// Inputs per request. The API allows far more, but smaller requests fail smaller: a rejected
    /// batch costs one retry of 96 chunks rather than of the whole document.
    /// </summary>
    private const int MaxInputsPerRequest = 96;

    /// <summary>
    /// Roughly four characters per English token — the same approximation the chunker budgets with.
    /// The two limits below are expressed in tokens, as the provider documents them, and converted
    /// with this so nothing here needs a tokenizer.
    /// </summary>
    private const int CharactersPerToken = 4;

    /// <summary>Tokens per request, kept under the provider's 300k ceiling.</summary>
    private const int MaxTokensPerRequest = 250_000;

    /// <summary>
    /// Tokens per single input, under the model's 8,192 limit. Chunks are a fraction of this; a
    /// pasted question could exceed it, and truncating is friendlier than a failed search.
    /// </summary>
    private const int MaxTokensPerInput = 8_000;

    /// <summary>
    /// Retries the provider client performs for 429 and 5xx responses, with exponential backoff.
    /// Beyond this the failure is reported rather than retried forever.
    /// </summary>
    private const int MaxRetries = 3;

    /// <summary>One attempt, not the whole batch: a hung connection must not hold a document open.</summary>
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    private readonly AiOptions _options;
    private readonly ILogger<OpenAIEmbeddingService> _logger;

    /// <summary>
    /// Built on first use, not in the constructor: the service is registered whether or not a key
    /// is configured, so that a missing key fails the documents that need embedding with a clear
    /// message instead of breaking dependency injection for everything that shares this scope.
    /// </summary>
    private readonly Lazy<EmbeddingClient> _client;

    public OpenAIEmbeddingService(
        IOptions<AiOptions> options,
        ILogger<OpenAIEmbeddingService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new Lazy<EmbeddingClient>(CreateClient);
    }

    public string Model => _options.EmbeddingModel;

    public int Dimensions => _options.EmbeddingDimensions;

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var batch = await EmbedBatchAsync([text], cancellationToken);

        return batch.Vectors[0];
    }

    public async Task<EmbeddingBatch> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
        {
            return new EmbeddingBatch([], 0);
        }

        var inputs = texts.Select(Prepare).ToArray();
        var vectors = new float[inputs.Length][];
        var requests = 0;
        var tokensUsed = 0;

        for (var start = 0; start < inputs.Length;)
        {
            var count = BatchSizeFrom(inputs, start);
            var batch = inputs[start..(start + count)];

            var response = await SendAsync(batch, cancellationToken);
            requests++;
            tokensUsed += response.Usage?.TotalTokenCount ?? 0;

            if (response.Count != batch.Length)
            {
                throw new AiUnavailableAppException(
                    $"The AI provider returned {response.Count} vectors for {batch.Length} inputs.");
            }

            foreach (var embedding in response)
            {
                // Position is taken from the provider's own index rather than from iteration order,
                // so a reordered response cannot silently attach a vector to the wrong chunk.
                vectors[start + embedding.Index] = Validated(embedding);
            }

            start += count;
        }

        _logger.LogInformation(
            "Embedded {Texts} texts in {Requests} request(s) using {Model} ({Dimensions}d), {Tokens} tokens",
            inputs.Length, requests, Model, Dimensions, tokensUsed);

        return new EmbeddingBatch(vectors, tokensUsed);
    }

    // ------------------------------------------------------------------ batching

    /// <summary>
    /// How many inputs starting at <paramref name="start"/> fit in one request. At least one is
    /// always taken, so an input at the token limit cannot stall the loop.
    /// </summary>
    private static int BatchSizeFrom(string[] inputs, int start)
    {
        const int maxCharacters = MaxTokensPerRequest * CharactersPerToken;

        var count = 0;
        var characters = 0;

        while (start + count < inputs.Length && count < MaxInputsPerRequest)
        {
            var next = inputs[start + count].Length;

            if (count > 0 && characters + next > maxCharacters)
            {
                break;
            }

            characters += next;
            count++;
        }

        return count;
    }

    private string Prepare(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ValidationAppException("Cannot embed empty text.");
        }

        const int maxCharacters = MaxTokensPerInput * CharactersPerToken;

        if (text.Length <= maxCharacters)
        {
            return text;
        }

        _logger.LogWarning(
            "Truncating a {Length}-character input to {Max} characters before embedding",
            text.Length, maxCharacters);

        return text[..maxCharacters];
    }

    private float[] Validated(OpenAIEmbedding embedding)
    {
        var vector = embedding.ToFloats().ToArray();

        // A width other than the configured one would be stored in module 7's fixed-width column,
        // so it is caught here, where the message can name the setting that disagrees.
        if (vector.Length != Dimensions)
        {
            throw new AiUnavailableAppException(
                $"{Model} returned {vector.Length}-dimension vectors but AI:EmbeddingDimensions is {Dimensions}.");
        }

        return vector;
    }

    // ------------------------------------------------------------------ provider

    private async Task<OpenAIEmbeddingCollection> SendAsync(
        string[] inputs,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _client.Value.GenerateEmbeddingsAsync(
                inputs,
                new EmbeddingGenerationOptions { Dimensions = Dimensions },
                cancellationToken);

            return result.Value;
        }
        catch (ClientResultException ex)
        {
            // Status only, without the exception: a rejection body quotes the API key it
            // rejected, and a log is exactly the wrong place for that.
            _logger.LogError(
                "The AI provider rejected an embedding request with status {Status}", ex.Status);

            throw new AiUnavailableAppException(Describe(ex.Status));
        }
        catch (Exception ex) when (ex is not (OperationCanceledException or AppException))
        {
            _logger.LogError(ex, "The AI provider could not be reached for embeddings");

            throw new AiUnavailableAppException(
                "The AI service could not be reached. Try again shortly.");
        }
    }

    private static string Describe(int status) => status switch
    {
        401 or 403 => "The AI service rejected this server's credentials.",
        429 => "The AI service is busy or over quota. Try again in a minute.",
        >= 500 => "The AI service is unavailable. Try again shortly.",
        _ => "The AI service could not process this document."
    };

    private EmbeddingClient CreateClient()
    {
        if (!_options.IsConfigured)
        {
            _logger.LogError(
                "AI:ApiKey is not configured. Set it with: dotnet user-secrets set \"AI:ApiKey\" \"<key>\"");

            throw new AiUnavailableAppException("Search indexing is not configured on this server.");
        }

        var clientOptions = new OpenAIClientOptions
        {
            // Retries 429 and 5xx with exponential backoff. Written out rather than left to the
            // default so the number is visible next to the timeout it multiplies.
            RetryPolicy = new ClientRetryPolicy(MaxRetries),
            NetworkTimeout = RequestTimeout
        };

        if (_options.Endpoint.Length > 0)
        {
            clientOptions.Endpoint = new Uri(_options.Endpoint);
        }

        return new EmbeddingClient(Model, new ApiKeyCredential(_options.ApiKey), clientOptions);
    }
}
