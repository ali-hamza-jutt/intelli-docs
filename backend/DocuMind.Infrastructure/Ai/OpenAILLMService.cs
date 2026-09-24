using System.ClientModel;
using System.ClientModel.Primitives;
using DocuMind.Application.Common;
using DocuMind.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace DocuMind.Infrastructure.Ai;

/// <summary>
/// Chat completions from OpenAI, the same account and key as the embeddings.
///
/// Failures are treated exactly as they are there: reported as a service being unavailable, with a
/// message written for the user, and the provider's own wording kept to the log.
/// </summary>
public class OpenAILLMService : ILLMService
{
    /// <summary>Retries for 429 and 5xx, performed by the provider client with backoff.</summary>
    private const int MaxRetries = 2;

    /// <summary>
    /// A chat call is slower than an embedding call — it writes the answer a token at a time — so
    /// it gets a longer ceiling. Until module 10 streams, the user is waiting on this whole time.
    /// </summary>
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(90);

    private readonly AiOptions _options;
    private readonly ILogger<OpenAILLMService> _logger;
    private readonly Lazy<ChatClient> _client;

    public OpenAILLMService(IOptions<AiOptions> options, ILogger<OpenAILLMService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new Lazy<ChatClient>(CreateClient);
    }

    public string Model => _options.ChatModel;

    public async Task<LlmCompletion> CompleteAsync(
        LlmPrompt prompt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var completion = await _client.Value.CompleteChatAsync(
                [
                    new SystemChatMessage(prompt.System),
                    new UserChatMessage(prompt.User)
                ],
                new ChatCompletionOptions
                {
                    // Low, not zero: answers should restate the document, not improvise on it.
                    Temperature = 0.2f
                },
                cancellationToken);

            var value = completion.Value;
            var text = string.Concat(value.Content.Select(part => part.Text)).Trim();

            if (text.Length == 0)
            {
                throw new AiUnavailableAppException("The AI service returned an empty answer.");
            }

            _logger.LogInformation(
                "Answered with {Model}: {InputTokens} in, {OutputTokens} out, finish reason {Finish}",
                Model, value.Usage?.InputTokenCount ?? 0, value.Usage?.OutputTokenCount ?? 0,
                value.FinishReason);

            return new LlmCompletion(
                text,
                value.Usage?.InputTokenCount ?? 0,
                value.Usage?.OutputTokenCount ?? 0);
        }
        catch (ClientResultException ex)
        {
            _logger.LogError(ex, "The AI provider rejected a chat request with status {Status}", ex.Status);

            throw new AiUnavailableAppException(Describe(ex.Status));
        }
        catch (Exception ex) when (ex is not (OperationCanceledException or AppException))
        {
            _logger.LogError(ex, "The AI provider could not be reached for a chat request");

            throw new AiUnavailableAppException("The AI service could not be reached. Try again shortly.");
        }
    }

    private static string Describe(int status) => status switch
    {
        401 or 403 => "The AI service rejected this server's credentials.",
        429 => "The AI service is busy or over quota. Try again in a minute.",
        >= 500 => "The AI service is unavailable. Try again shortly.",
        _ => "The AI service could not answer this question."
    };

    private ChatClient CreateClient()
    {
        if (!_options.IsConfigured)
        {
            _logger.LogError(
                "AI:ApiKey is not configured. Set it with: dotnet user-secrets set \"AI:ApiKey\" \"<key>\"");

            throw new AiUnavailableAppException("Answering is not configured on this server.");
        }

        var clientOptions = new OpenAIClientOptions
        {
            RetryPolicy = new ClientRetryPolicy(MaxRetries),
            NetworkTimeout = RequestTimeout
        };

        if (_options.Endpoint.Length > 0)
        {
            clientOptions.Endpoint = new Uri(_options.Endpoint);
        }

        return new ChatClient(Model, new ApiKeyCredential(_options.ApiKey), clientOptions);
    }
}
