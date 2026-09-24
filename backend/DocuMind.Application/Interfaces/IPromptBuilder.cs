namespace DocuMind.Application.Interfaces;

/// <summary>
/// Turns a question and the passages retrieved for it into a prompt.
///
/// The wording lives here and nowhere else. A prompt scattered across controllers is impossible to
/// review, and this one carries the rules the whole feature depends on: answer only from what is
/// given, say so when the answer is not there, and cite by number.
/// </summary>
public interface IPromptBuilder
{
    /// <summary>
    /// Builds the grounded-answer prompt. Passages are numbered from 1 in the order given, and the
    /// model is told to cite those numbers, so a marker in the answer can be traced back to exactly
    /// one passage.
    /// </summary>
    LlmPrompt BuildAnswerPrompt(string question, IReadOnlyList<ChunkMatch> passages);
}
