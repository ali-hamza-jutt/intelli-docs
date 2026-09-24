using System.Text;
using DocuMind.Application.Interfaces;

namespace DocuMind.Infrastructure.Ai;

/// <summary>
/// The prompt that keeps answers tied to the documents.
///
/// Three rules do the work. Answer only from the passages, so the model's own training cannot leak
/// in as fact. Say plainly when the passages do not cover the question, because a confident wrong
/// answer is worse than no answer. Cite by number, so every claim can be traced to a passage the
/// user can open and read.
/// </summary>
public class GroundedPromptBuilder : IPromptBuilder
{
    private const string Instructions = """
        You are DocuMind, an assistant that answers questions about the user's own documents.

        Rules:
        1. Answer only from the numbered passages below. Do not use outside knowledge, and do not
           guess.
        2. If the passages do not contain the answer, say so in one sentence and stop. Do not offer
           a general answer instead.
        3. Cite the passages you used with square-bracket markers, like [1] or [2][3], placed right
           after the sentence they support. Only cite passage numbers that appear below.
        4. Never invent a page number, a file name or a quotation.
        5. Be concise and factual. Prefer the document's own wording for specifics such as numbers,
           dates and names.
        """;

    public LlmPrompt BuildAnswerPrompt(string question, IReadOnlyList<ChunkMatch> passages)
    {
        var user = new StringBuilder();

        user.AppendLine("Passages:");
        user.AppendLine();

        for (var index = 0; index < passages.Count; index++)
        {
            var passage = passages[index];

            // The file name and pages are given to the model as context, not for it to repeat: the
            // citation the user sees is built from these same values in code, never parsed back out
            // of the answer.
            user.AppendLine(
                $"[{index + 1}] {passage.FileName}, {PageLabel(passage)}:");
            user.AppendLine(passage.Text);
            user.AppendLine();
        }

        user.AppendLine("Question:");
        user.Append(question);

        return new LlmPrompt(Instructions, user.ToString());
    }

    private static string PageLabel(ChunkMatch passage)
    {
        return passage.PageNumber == passage.EndPageNumber
            ? $"page {passage.PageNumber}"
            : $"pages {passage.PageNumber}–{passage.EndPageNumber}";
    }
}
