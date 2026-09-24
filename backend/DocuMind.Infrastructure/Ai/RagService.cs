using System.Text.RegularExpressions;
using DocuMind.Application.Interfaces;
using DocuMind.Infrastructure.Chunking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocuMind.Infrastructure.Ai;

/// <summary>
/// Retrieve, then answer, then attribute.
///
/// The order matters for honesty as much as for quality. Retrieval happens first and decides
/// whether there is anything to say at all: if nothing clears the similarity floor, the model is
/// never asked, which is the only way to be certain it cannot invent an answer or a source. When it
/// is asked, the citations the user sees are built in code from the passages that were sent, so a
/// marker can never point at a page the model made up.
/// </summary>
public partial class RagService : IRagService
{
    /// <summary>What the user is told when their documents do not cover the question.</summary>
    private const string NoContextAnswer =
        "I could not find anything about that in your documents.";

    private readonly IVectorSearchService _search;
    private readonly IPromptBuilder _prompts;
    private readonly ILLMService _llm;
    private readonly RetrievalOptions _options;
    private readonly ILogger<RagService> _logger;

    public RagService(
        IVectorSearchService search,
        IPromptBuilder prompts,
        ILLMService llm,
        IOptions<RetrievalOptions> options,
        ILogger<RagService> logger)
    {
        _search = search;
        _prompts = prompts;
        _llm = llm;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RagAnswer> AskAsync(
        Guid userId,
        string question,
        Guid? documentId = null,
        CancellationToken cancellationToken = default)
    {
        var passages = await _search.SearchAsync(
            question, userId, _options.MaxContextChunks, documentId, cancellationToken);

        if (passages.Count == 0)
        {
            _logger.LogInformation("No passage cleared the similarity floor; the model was not asked");

            return new RagAnswer(question, NoContextAnswer, Grounded: false, [], null, 0, 0);
        }

        var prompt = _prompts.BuildAnswerPrompt(question, passages);
        var completion = await _llm.CompleteAsync(prompt, cancellationToken);
        var citations = CitationsIn(completion.Text, passages);

        _logger.LogInformation(
            "Answered from {Passages} passages, {Cited} of them cited",
            passages.Count, citations.Count);

        return new RagAnswer(
            question,
            completion.Text,
            Grounded: true,
            citations,
            _llm.Model,
            completion.InputTokens,
            completion.OutputTokens);
    }

    /// <summary>
    /// The passages the answer actually cited, in the order it first cites them. A marker for a
    /// passage that was not sent is dropped rather than shown, so no citation can point at nothing.
    /// </summary>
    private static List<RagCitation> CitationsIn(string answer, IReadOnlyList<ChunkMatch> passages)
    {
        var citations = new List<RagCitation>();
        var seen = new HashSet<int>();

        foreach (Match match in CitationMarker().Matches(answer))
        {
            foreach (var part in match.Groups[1].Value.Split(','))
            {
                if (!int.TryParse(part.Trim(), out var marker)
                    || marker < 1
                    || marker > passages.Count
                    || !seen.Add(marker))
                {
                    continue;
                }

                var passage = passages[marker - 1];

                citations.Add(new RagCitation(
                    marker,
                    passage.DocumentId,
                    passage.FileName,
                    passage.PageNumber,
                    passage.EndPageNumber,
                    passage.Text,
                    passage.Similarity));
            }
        }

        return citations;
    }

    /// <summary>Matches [1], [2][3] and the occasional [1, 2] a model writes anyway.</summary>
    [GeneratedRegex(@"\[\s*(\d+(?:\s*,\s*\d+)*)\s*\]")]
    private static partial Regex CitationMarker();
}
