namespace DocuMind.Application.Interfaces;

/// <summary>
/// Sends a prompt to a chat model and returns what it wrote.
///
/// Deliberately narrow: no conversation state, no tools, no knowledge of retrieval. Everything that
/// decides <em>what</em> to ask lives in <see cref="IPromptBuilder"/>, so the wording of a prompt can
/// change without touching the code that talks to the provider.
/// </summary>
public interface ILLMService
{
    /// <summary>The model answering, echoed back so a user can see what wrote their answer.</summary>
    string Model { get; }

    Task<LlmCompletion> CompleteAsync(LlmPrompt prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// The same completion, handed over as it is written rather than when it is finished.
    ///
    /// Cancelling <paramref name="cancellationToken"/> abandons the request at the provider, which is
    /// what makes a Stop button save money rather than merely hide output.
    /// </summary>
    IAsyncEnumerable<LlmChunk> StreamAsync(
        LlmPrompt prompt,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A piece of an answer as it arrives.
///
/// Token counts are only known at the end, so they come on a final chunk that carries no text. A
/// provider that does not report usage while streaming leaves them at zero rather than guessing.
/// </summary>
public record LlmChunk(string Text, int InputTokens = 0, int OutputTokens = 0);

/// <summary>
/// The two halves of a chat request: the standing instructions, and the question with its context.
/// </summary>
public record LlmPrompt(string System, string User);

/// <summary>
/// What the model returned, with the token counts from the provider's own accounting — the basis
/// for cost reporting later, and the only honest source for it.
/// </summary>
public record LlmCompletion(string Text, int InputTokens, int OutputTokens);
