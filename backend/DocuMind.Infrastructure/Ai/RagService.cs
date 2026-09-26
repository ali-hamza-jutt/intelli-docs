using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
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
    private readonly IUsageRecorder _usage;
    private readonly RetrievalOptions _options;
    private readonly ILogger<RagService> _logger;

    public RagService(
        IVectorSearchService search,
        IPromptBuilder prompts,
        ILLMService llm,
        IUsageRecorder usage,
        IOptions<RetrievalOptions> options,
        ILogger<RagService> logger)
    {
        _search = search;
        _prompts = prompts;
        _llm = llm;
        _usage = usage;
        _options = options.Value;
        _logger = logger;
    }

    public int MaxHistoryMessages => _options.MaxHistoryMessages;

    public async Task<RagAnswer> AskAsync(
        Guid userId,
        string question,
        Guid? documentId = null,
        IReadOnlyList<RagTurn>? history = null,
        CancellationToken cancellationToken = default)
    {
        var recent = Bounded(history);

        var passages = await _search.SearchAsync(
            RetrievalQuery(question, recent),
            userId,
            _options.MaxContextChunks,
            documentId,
            cancellationToken);

        if (passages.Count == 0)
        {
            _logger.LogInformation("No passage cleared the similarity floor; the model was not asked");

            return new RagAnswer(question, NoContextAnswer, Grounded: false, [], null, 0, 0);
        }

        var prompt = _prompts.BuildAnswerPrompt(question, passages, recent);
        var completion = await _llm.CompleteAsync(prompt, cancellationToken);
        var citations = CitationsIn(completion.Text, passages);

        await RecordAsync(userId, completion.InputTokens, completion.OutputTokens, cancellationToken);

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

    public async IAsyncEnumerable<RagStreamEvent> StreamAsync(
        Guid userId,
        string question,
        Guid? documentId = null,
        IReadOnlyList<RagTurn>? history = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var recent = Bounded(history);

        var passages = await _search.SearchAsync(
            RetrievalQuery(question, recent),
            userId,
            _options.MaxContextChunks,
            documentId,
            cancellationToken);

        if (passages.Count == 0)
        {
            // Nothing to answer from, so nothing is generated. The refusal is still delivered as a
            // delta, so a client has one path to render rather than two.
            _logger.LogInformation("No passage cleared the similarity floor; the model was not asked");

            yield return new RagStreamEvent.Delta(NoContextAnswer);
            yield return new RagStreamEvent.Final(
                new RagAnswer(question, NoContextAnswer, Grounded: false, [], null, 0, 0));

            yield break;
        }

        var prompt = _prompts.BuildAnswerPrompt(question, passages, recent);
        var answer = new StringBuilder();
        var inputTokens = 0;
        var outputTokens = 0;

        await foreach (var chunk in _llm.StreamAsync(prompt, cancellationToken))
        {
            if (chunk.Text.Length > 0)
            {
                answer.Append(chunk.Text);

                yield return new RagStreamEvent.Delta(chunk.Text);
            }

            if (chunk.InputTokens > 0 || chunk.OutputTokens > 0)
            {
                inputTokens = chunk.InputTokens;
                outputTokens = chunk.OutputTokens;
            }
        }

        var text = answer.ToString().Trim();
        var citations = CitationsIn(text, passages);

        _logger.LogInformation(
            "Streamed an answer from {Passages} passages, {Cited} of them cited",
            passages.Count, citations.Count);

        // Only reached when the answer finished. A stopped stream abandons this iterator, so its
        // partial cost goes unrecorded — the provider does not report usage for a call it never
        // completed, and inventing a number would be worse than the gap.
        await RecordAsync(userId, inputTokens, outputTokens, cancellationToken);

        yield return new RagStreamEvent.Final(new RagAnswer(
            question, text, Grounded: true, citations, _llm.Model, inputTokens, outputTokens));
    }

    /// <summary>
    /// Books the answer against the user who asked for it. Both paths — waiting for the whole answer
    /// and streaming it — end here, so chat spending is recorded in one place.
    /// </summary>
    private async Task RecordAsync(
        Guid userId,
        int inputTokens,
        int outputTokens,
        CancellationToken cancellationToken)
    {
        await _usage.RecordAsync(
            userId,
            UsageKind.Chat,
            _llm.Model,
            inputTokens,
            outputTokens,
            items: 1,
            cancellationToken);
    }

    /// <summary>
    /// The tail of the thread, never more than the configured number of turns.
    /// </summary>
    private IReadOnlyList<RagTurn>? Bounded(IReadOnlyList<RagTurn>? history)
    {
        if (history is null or { Count: 0 })
        {
            return null;
        }

        return history.Count <= MaxHistoryMessages
            ? history
            : [.. history.Skip(history.Count - MaxHistoryMessages)];
    }

    /// <summary>
    /// What gets embedded and searched for.
    ///
    /// "And for sick leave?" retrieves nothing useful on its own, so the previous question is
    /// prepended to give it a subject. Only the previous <em>question</em>, not the answer: an answer
    /// is long enough to drown out the words that matter, and only one, because further back the
    /// topic has usually moved on.
    /// </summary>
    private static string RetrievalQuery(string question, IReadOnlyList<RagTurn>? history)
    {
        var previousQuestion = history?.LastOrDefault(turn => turn.FromUser)?.Text;

        return string.IsNullOrWhiteSpace(previousQuestion)
            ? question
            : $"{previousQuestion}\n{question}";
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
