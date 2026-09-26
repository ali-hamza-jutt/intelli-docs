using DocuMind.Application.DTOs.Conversations;
using DocuMind.Application.Interfaces;
using DocuMind.Application.Services;
using DocuMind.Domain.Entities;
using DocuMind.Tests.Doubles;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocuMind.Tests.Unit;

/// <summary>
/// What a stopped answer leaves behind.
///
/// Tested here rather than over HTTP because a client hanging up is a cancelled token, and the rule
/// it triggers — keep the words that arrived, mark them incomplete, claim no model or cost — lives
/// in this service. An in-memory test server cannot hang up convincingly; a CancellationToken can.
/// </summary>
public class ConversationStreamTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeConversationRepository _conversations = new();
    private readonly FakeDocumentRepository _documents = new();

    private ConversationService Service(IRagService rag) =>
        new(_conversations, _documents, rag, new FakeCurrentUser(_userId), NullLogger<ConversationService>.Instance);

    private Conversation StartConversation()
    {
        var document = _documents.Add(_userId);

        return _conversations.Add(_userId, document.Id);
    }

    [Fact]
    public async Task Keeps_the_words_that_arrived_and_marks_them_incomplete()
    {
        var conversation = StartConversation();
        var rag = new ScriptedRagService("one two three four five six seven eight", TimeSpan.FromMilliseconds(20));

        using var reader = new CancellationTokenSource();
        var seen = new List<string>();

        try
        {
            await foreach (var change in Service(rag).StreamAskAsync(
                conversation.Id, new AskInConversationRequest { Question = "Count for me" }, reader.Token))
            {
                if (change is ConversationStreamEvent.Delta delta)
                {
                    seen.Add(delta.Text);

                    if (seen.Count == 3)
                    {
                        reader.Cancel();
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The reader hung up. Everything below is about what the server did with that.
        }

        var answer = Assert.Single(_conversations.Messages, message => message.Role == MessageRole.Assistant);

        Assert.True(answer.Stopped);
        Assert.Equal("one two three", answer.Content);
        Assert.DoesNotContain("eight", answer.Content);

        // No model and no cost are claimed for an answer nobody finished writing.
        Assert.Null(answer.Model);
        Assert.Equal(0, answer.InputTokens);
        Assert.Equal(0, answer.OutputTokens);

        // The question it answers is stored with it, and the pair was written in one save.
        Assert.Contains(_conversations.Messages, message => message.Role == MessageRole.User);
        Assert.Equal(1, _conversations.Saves);
    }

    [Fact]
    public async Task Stores_nothing_at_all_when_it_is_stopped_before_the_first_word()
    {
        var conversation = StartConversation();
        var rag = new ScriptedRagService("one two three", TimeSpan.FromMilliseconds(50));

        using var reader = new CancellationTokenSource();
        await reader.CancelAsync();

        try
        {
            await foreach (var _ in Service(rag).StreamAskAsync(
                conversation.Id, new AskInConversationRequest { Question = "Count for me" }, reader.Token))
            {
                // nothing arrives
            }
        }
        catch (OperationCanceledException)
        {
        }

        // A question with no answer would be worse than nothing, so the whole turn is dropped.
        Assert.Empty(_conversations.Messages);
        Assert.Equal(0, _conversations.Saves);
    }

    [Fact]
    public async Task A_finished_answer_is_stored_whole_and_not_marked_stopped()
    {
        var conversation = StartConversation();
        var rag = new ScriptedRagService("twenty days of annual leave");

        await foreach (var _ in Service(rag).StreamAskAsync(
            conversation.Id, new AskInConversationRequest { Question = "How much leave?" }))
        {
        }

        var answer = Assert.Single(_conversations.Messages, message => message.Role == MessageRole.Assistant);

        Assert.False(answer.Stopped);
        Assert.Equal("twenty days of annual leave", answer.Content);
        Assert.Equal("scripted-model", answer.Model);
    }

    [Fact]
    public async Task Replays_the_thread_so_a_follow_up_makes_sense()
    {
        var conversation = StartConversation();
        var rag = new ScriptedRagService("carried over for one quarter");

        var service = Service(rag);

        await foreach (var _ in service.StreamAskAsync(
            conversation.Id, new AskInConversationRequest { Question = "How much annual leave?" }))
        {
        }

        await foreach (var _ in service.StreamAskAsync(
            conversation.Id, new AskInConversationRequest { Question = "And carrying it over?" }))
        {
        }

        Assert.NotNull(rag.SeenHistory);
        Assert.Contains(rag.SeenHistory!, turn => turn.Text == "How much annual leave?");
        Assert.Contains(rag.SeenHistory!, turn => turn.FromUser == false);
    }

    [Fact]
    public async Task Another_users_conversation_is_not_found()
    {
        var conversation = StartConversation();

        var someoneElse = new ConversationService(
            _conversations,
            _documents,
            new ScriptedRagService("anything"),
            new FakeCurrentUser(Guid.NewGuid()),
            NullLogger<ConversationService>.Instance);

        await Assert.ThrowsAsync<Application.Common.NotFoundAppException>(async () =>
        {
            await foreach (var _ in someoneElse.StreamAskAsync(
                conversation.Id, new AskInConversationRequest { Question = "Tell me everything" }))
            {
            }
        });

        Assert.Empty(_conversations.Messages);
    }
}
