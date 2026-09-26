using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using DocuMind.Infrastructure.Ai;
using DocuMind.Infrastructure.Chunking;
using DocuMind.Tests.Doubles;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DocuMind.Tests.Unit;

/// <summary>
/// The rules that make an answer trustworthy: it is written only from passages, it refuses when
/// there are none, and its citations point at passages that were really sent.
///
/// These run against fakes rather than a provider, because none of the rules are the provider's —
/// they are this code's, and they have to hold whichever model is behind them.
/// </summary>
public class RagServiceTests
{
    private readonly FakeVectorSearchService _search = new();
    private readonly FakeLlmService _llm = new();
    private readonly FakeUsageRecorder _usage = new();

    private RagService Service(int maxHistory = 8, int maxContext = 5) =>
        new(_search,
            new GroundedPromptBuilder(),
            _llm,
            _usage,
            Options.Create(new RetrievalOptions { MaxHistoryMessages = maxHistory, MaxContextChunks = maxContext }),
            NullLogger<RagService>.Instance);

    [Fact]
    public async Task Refuses_without_calling_the_model_when_nothing_is_relevant()
    {
        var answer = await Service().AskAsync(Guid.NewGuid(), "What is the capital of France?");

        Assert.False(answer.Grounded);
        Assert.Empty(answer.Citations);
        Assert.Null(answer.Model);

        // The point of the floor: an unanswerable question costs nothing.
        Assert.Empty(_llm.Prompts);
        Assert.Empty(_usage.Records);
    }

    [Fact]
    public async Task Cites_only_the_passages_the_answer_marks()
    {
        _search.WillFind(
            FakeVectorSearchService.Passage("Twenty days of annual leave.", page: 1),
            FakeVectorSearchService.Passage("Ten days of sick leave.", page: 2),
            FakeVectorSearchService.Passage("Parental leave in three blocks.", page: 3));

        _llm.WillAnswer("Twenty days [1], and ten sick days [2].");

        var answer = await Service().AskAsync(Guid.NewGuid(), "How much leave is there?");

        Assert.True(answer.Grounded);
        Assert.Equal([1, 2], answer.Citations.Select(citation => citation.Marker));
        Assert.Equal(1, answer.Citations[0].PageNumber);
        Assert.Equal(2, answer.Citations[1].PageNumber);
    }

    [Fact]
    public async Task Drops_a_marker_for_a_passage_that_was_never_sent()
    {
        _search.WillFind(FakeVectorSearchService.Passage());
        _llm.WillAnswer("This cites a passage that does not exist [9].");

        var answer = await Service().AskAsync(Guid.NewGuid(), "Anything at all?");

        // A citation must point at something, or it is worse than no citation.
        Assert.Empty(answer.Citations);
        Assert.True(answer.Grounded);
    }

    [Fact]
    public async Task Cites_each_passage_once_however_often_it_is_marked()
    {
        _search.WillFind(
            FakeVectorSearchService.Passage("First.", page: 1),
            FakeVectorSearchService.Passage("Second.", page: 2));

        _llm.WillAnswer("As set out [1], and again [1][2], and once more [1, 2].");

        var answer = await Service().AskAsync(Guid.NewGuid(), "Repeat yourself");

        Assert.Equal([1, 2], answer.Citations.Select(citation => citation.Marker));
    }

    [Fact]
    public async Task Sends_no_more_passages_than_configured()
    {
        _search.WillFind([.. Enumerable.Range(0, 10)
            .Select(index => FakeVectorSearchService.Passage($"Passage {index}.", page: index + 1))]);

        await Service(maxContext: 3).AskAsync(Guid.NewGuid(), "How much leave is there?");

        var numbered = _llm.Prompts[0].User.Split('\n').Count(line => line.StartsWith('['));

        Assert.Equal(3, numbered);
    }

    [Fact]
    public async Task Gives_a_follow_up_its_subject_back()
    {
        _search.WillFind(FakeVectorSearchService.Passage());
        _llm.WillAnswer("Twenty days [1].");

        await Service().AskAsync(
            Guid.NewGuid(),
            "And what about carrying it over?",
            documentId: null,
            history:
            [
                new RagTurn(FromUser: true, "How much annual leave do we get?"),
                new RagTurn(FromUser: false, "Twenty days."),
            ]);

        // "And what about carrying it over?" retrieves nothing on its own, so the previous question
        // is carried into the search — but not the previous answer, which would drown it out.
        var query = Assert.Single(_search.Queries);
        Assert.Contains("How much annual leave do we get?", query);
        Assert.Contains("carrying it over", query);
        Assert.DoesNotContain("Twenty days.", query);
    }

    [Fact]
    public async Task Replays_no_more_history_than_configured()
    {
        _search.WillFind(FakeVectorSearchService.Passage());

        var history = Enumerable.Range(0, 12)
            .Select(index => new RagTurn(index % 2 == 0, $"turn number {index}"))
            .ToList();

        await Service(maxHistory: 4).AskAsync(Guid.NewGuid(), "And then?", null, history);

        var prompt = _llm.Prompts[0].User;

        Assert.DoesNotContain("turn number 0", prompt);
        Assert.Contains("turn number 11", prompt);
    }

    [Fact]
    public async Task Books_what_an_answer_cost_against_the_user_who_asked()
    {
        var userId = Guid.NewGuid();
        _search.WillFind(FakeVectorSearchService.Passage());
        _llm.InputTokens = 900;
        _llm.OutputTokens = 40;

        await Service().AskAsync(userId, "How much leave is there?");

        var record = Assert.Single(_usage.Records);
        Assert.Equal(userId, record.UserId);
        Assert.Equal(UsageKind.Chat, record.Kind);
        Assert.Equal(900, record.InputTokens);
        Assert.Equal(40, record.OutputTokens);
    }

    [Fact]
    public async Task Streams_the_answer_then_its_citations()
    {
        _search.WillFind(
            FakeVectorSearchService.Passage("Twenty days.", page: 1),
            FakeVectorSearchService.Passage("Ten sick days.", page: 2));

        _llm.WillAnswer("Twenty days of annual leave [1].");

        var deltas = new List<string>();
        RagAnswer? final = null;

        await foreach (var change in Service().StreamAsync(Guid.NewGuid(), "How much leave?"))
        {
            switch (change)
            {
                case RagStreamEvent.Delta delta:
                    Assert.Null(final); // nothing arrives after the final event
                    deltas.Add(delta.Text);
                    break;

                case RagStreamEvent.Final completed:
                    final = completed.Answer;
                    break;
            }
        }

        Assert.True(deltas.Count > 1);
        Assert.NotNull(final);
        Assert.Equal(string.Concat(deltas).Trim(), final.Answer);
        Assert.Equal([1], final.Citations.Select(citation => citation.Marker));
    }

    [Fact]
    public async Task Streams_the_refusal_too_when_nothing_is_relevant()
    {
        var events = new List<RagStreamEvent>();

        await foreach (var change in Service().StreamAsync(Guid.NewGuid(), "Something unrelated"))
        {
            events.Add(change);
        }

        // One delta and one final, so a client has a single path to render either way.
        var delta = Assert.IsType<RagStreamEvent.Delta>(events[0]);
        var final = Assert.IsType<RagStreamEvent.Final>(events[1]);

        Assert.Contains("could not find", delta.Text);
        Assert.False(final.Answer.Grounded);
        Assert.Empty(_llm.Prompts);
    }
}
