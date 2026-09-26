using System.Runtime.CompilerServices;
using System.Text;
using DocuMind.Application.Common;
using DocuMind.Application.DTOs.Conversations;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DocuMind.Application.Services;

/// <summary>
/// Keeps conversations: which document a thread is about, what was asked, what was answered, and the
/// sources each answer used.
///
/// Roles are assigned here rather than accepted from the client. A client that could label its own
/// message "Assistant" could put words in the model's mouth and have them replayed as history on
/// every later question.
/// </summary>
public class ConversationService : IConversationService
{
    private readonly IConversationRepository _conversations;
    private readonly IDocumentRepository _documents;
    private readonly IRagService _rag;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(
        IConversationRepository conversations,
        IDocumentRepository documents,
        IRagService rag,
        ICurrentUser currentUser,
        ILogger<ConversationService> logger)
    {
        _conversations = conversations;
        _documents = documents;
        _rag = rag;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ConversationResponse?> StartAsync(
        StartConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.RequireUserId();
        var document = await _documents.GetByIdForUserAsync(request.DocumentId, userId);

        if (document is null)
        {
            return null;
        }

        if (document.Status != DocumentStatus.Completed)
        {
            throw new ConflictAppException(
                "This document is not ready to be asked about yet.", "DOCUMENT_NOT_READY");
        }

        // Titled after the document until the first question replaces it.
        var conversation = Conversation.Start(userId, document.Id, document.FileName);

        await _conversations.AddAsync(conversation);
        await _conversations.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Started conversation {ConversationId} about document {DocumentId}",
            conversation.Id, document.Id);

        if (!string.IsNullOrWhiteSpace(request.Question))
        {
            await AnswerAsync(conversation, request.Question, cancellationToken);
        }

        return await GetAsync(conversation.Id, cancellationToken);
    }

    public async Task<List<ConversationSummaryResponse>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var summaries = await _conversations.ListForUserAsync(
            _currentUser.RequireUserId(), cancellationToken);

        return [.. summaries.Select(summary => new ConversationSummaryResponse
        {
            Id = summary.Id,
            DocumentId = summary.DocumentId,
            Title = summary.Title,
            MessageCount = summary.MessageCount,
            CreatedAt = summary.CreatedAt,
            UpdatedAt = summary.UpdatedAt
        })];
    }

    public async Task<ConversationResponse?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.RequireUserId();
        var conversation = await _conversations.GetWithMessagesAsync(id, userId, cancellationToken);

        if (conversation is null)
        {
            return null;
        }

        // The document's current name, which may have changed since the thread started — and may be
        // gone entirely. Stored citations keep the name they were written with either way.
        var document = await _documents.GetByIdForUserAsync(conversation.DocumentId, userId);

        return new ConversationResponse
        {
            Id = conversation.Id,
            DocumentId = conversation.DocumentId,
            DocumentName = document?.FileName,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            Messages = [.. conversation.Messages.Select(ToResponse)]
        };
    }

    public async Task<List<ChatMessageResponse>?> GetMessagesAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _conversations.GetWithMessagesAsync(
            id, _currentUser.RequireUserId(), cancellationToken);

        return conversation is null ? null : [.. conversation.Messages.Select(ToResponse)];
    }

    public async Task<ChatMessageResponse?> AskAsync(
        Guid id,
        AskInConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _conversations.GetAsync(
            id, _currentUser.RequireUserId(), cancellationToken);

        if (conversation is null)
        {
            return null;
        }

        return ToResponse(await AnswerAsync(conversation, request.Question, cancellationToken));
    }

    public async IAsyncEnumerable<ConversationStreamEvent> StreamAskAsync(
        Guid id,
        AskInConversationRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var conversation = await _conversations.GetAsync(
            id, _currentUser.RequireUserId(), cancellationToken);

        if (conversation is null)
        {
            // Thrown before the first event, while the response can still be a plain 404.
            throw new NotFoundAppException("This conversation does not exist.");
        }

        var history = await _conversations.GetRecentMessagesAsync(
            conversation.Id, _rag.MaxHistoryMessages, cancellationToken);

        await _conversations.AddMessageAsync(
            conversation.AskedBy(request.Question, history.Count == 0));

        var answer = new StringBuilder();
        var citations = new List<RagCitation>();

        // Starts true and is corrected by the final event, which is the only thing that reports a
        // question nothing matched. A stopped answer never gets that event, and it was being written
        // from passages — marking it ungrounded would claim the opposite of what happened.
        var grounded = true;
        var inputTokens = 0;
        var outputTokens = 0;
        var stopped = false;
        string? model = null;

        var stream = _rag.StreamAsync(
            conversation.UserId,
            request.Question,
            conversation.DocumentId,
            [.. history.Select(message => new RagTurn(message.Role == MessageRole.User, message.Content))],
            cancellationToken);

        // Enumerated by hand so that cancellation can be caught: a yield cannot sit inside a
        // try/catch, and cancellation here is a normal outcome that still has to be saved.
        await using var events = stream.GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            try
            {
                if (!await events.MoveNextAsync())
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                stopped = true;
                break;
            }

            switch (events.Current)
            {
                case RagStreamEvent.Delta delta:
                    answer.Append(delta.Text);
                    yield return new ConversationStreamEvent.Delta(delta.Text);
                    break;

                case RagStreamEvent.Final final:
                    grounded = final.Answer.Grounded;
                    citations = [.. final.Answer.Citations];
                    model = final.Answer.Model;
                    inputTokens = final.Answer.InputTokens;
                    outputTokens = final.Answer.OutputTokens;
                    break;
            }
        }

        var text = answer.ToString().Trim();

        if (text.Length == 0)
        {
            // Stopped before a single word arrived: there is no answer to keep, and a question with
            // no answer would be worse than nothing. The whole turn is dropped.
            _logger.LogInformation("Streamed answer in {ConversationId} stopped before any text", id);

            yield break;
        }

        // A stopped answer has no final event, so its model and usage are unknown — left null and
        // zero rather than filled in with a guess. The Stopped flag says why.
        var message = conversation.AnsweredWith(
            text, grounded, model, inputTokens, outputTokens, stopped);

        foreach (var citation in citations)
        {
            message.Cite(
                citation.Marker,
                citation.DocumentId,
                citation.FileName,
                citation.PageNumber,
                citation.EndPageNumber,
                citation.Text,
                citation.Similarity);
        }

        await _conversations.AddMessageAsync(message);

        // Saved with a fresh token: the request's own token is already cancelled in the case this
        // exists to handle, and the answer has to be stored anyway.
        await _conversations.SaveChangesAsync(CancellationToken.None);

        if (stopped)
        {
            _logger.LogInformation(
                "Stored a stopped answer of {Characters} characters in {ConversationId}", text.Length, id);
        }

        yield return new ConversationStreamEvent.Final(ToResponse(message));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var conversation = await _conversations.GetAsync(
            id, _currentUser.RequireUserId(), cancellationToken);

        if (conversation is null)
        {
            return false;
        }

        await _conversations.RemoveAsync(conversation);
        await _conversations.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Records the question, answers it with the thread's recent history for context, and records the
    /// answer with its sources — all in one save, so a stored question can never be left without the
    /// answer it produced.
    /// </summary>
    private async Task<ChatMessage> AnswerAsync(
        Conversation conversation,
        string question,
        CancellationToken cancellationToken)
    {
        var history = await _conversations.GetRecentMessagesAsync(
            conversation.Id, _rag.MaxHistoryMessages, cancellationToken);

        // The history doubles as the answer to "is this the opening question?", which decides
        // whether the thread takes its title from it.
        await _conversations.AddMessageAsync(conversation.AskedBy(question, history.Count == 0));

        var answer = await _rag.AskAsync(
            conversation.UserId,
            question,
            conversation.DocumentId,
            [.. history.Select(message => new RagTurn(message.Role == MessageRole.User, message.Content))],
            cancellationToken);

        var message = conversation.AnsweredWith(
            answer.Answer, answer.Grounded, answer.Model, answer.InputTokens, answer.OutputTokens);

        await _conversations.AddMessageAsync(message);

        foreach (var citation in answer.Citations)
        {
            message.Cite(
                citation.Marker,
                citation.DocumentId,
                citation.FileName,
                citation.PageNumber,
                citation.EndPageNumber,
                citation.Text,
                citation.Similarity);
        }

        await _conversations.SaveChangesAsync(cancellationToken);

        return message;
    }

    private static ChatMessageResponse ToResponse(ChatMessage message)
    {
        return new ChatMessageResponse
        {
            Id = message.Id,
            Role = message.Role.ToString(),
            Content = message.Content,
            Grounded = message.Grounded,
            Model = message.Model,
            Stopped = message.Stopped,
            InputTokens = message.InputTokens,
            OutputTokens = message.OutputTokens,
            CreatedAt = message.CreatedAt,
            Sources = [.. message.Sources.Select(source => new MessageSourceResponse
            {
                Marker = source.Marker,
                DocumentId = source.DocumentId,
                FileName = source.FileName,
                PageNumber = source.PageNumber,
                EndPageNumber = source.EndPageNumber,
                Text = source.Text,
                Similarity = source.Similarity
            })]
        };
    }
}
