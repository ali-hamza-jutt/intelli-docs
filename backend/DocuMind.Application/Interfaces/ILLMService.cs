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
}

/// <summary>
/// The two halves of a chat request: the standing instructions, and the question with its context.
/// </summary>
public record LlmPrompt(string System, string User);

/// <summary>
/// What the model returned, with the token counts from the provider's own accounting — the basis
/// for cost reporting later, and the only honest source for it.
/// </summary>
public record LlmCompletion(string Text, int InputTokens, int OutputTokens);
