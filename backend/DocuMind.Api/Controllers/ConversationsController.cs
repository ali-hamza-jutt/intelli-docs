using System.Text.Json;
using DocuMind.Api.Extensions;
using DocuMind.Application.DTOs.Conversations;
using DocuMind.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DocuMind.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ConversationsController : ControllerBase
{
    /// <summary>camelCase, matching every other response the browser receives.</summary>
    private static readonly JsonSerializerOptions SseJson = new(JsonSerializerDefaults.Web);

    private readonly IConversationService _conversations;

    public ConversationsController(IConversationService conversations)
    {
        _conversations = conversations;
    }

    /// <summary>The caller's conversations, most recently used first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ConversationSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ConversationSummaryResponse>>> List(
        CancellationToken cancellationToken)
    {
        return Ok(await _conversations.ListAsync(cancellationToken));
    }

    /// <summary>
    /// Starts a conversation about a document. An opening question may be included, in which case the
    /// returned thread already holds the question and its answer.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ConversationResponse>> Start(
        StartConversationRequest request,
        CancellationToken cancellationToken)
    {
        var conversation = await _conversations.StartAsync(request, cancellationToken);

        if (conversation is null)
        {
            return NotFound();
        }

        return CreatedAtAction(nameof(Get), new { id = conversation.Id }, conversation);
    }

    /// <summary>One conversation with its messages and their stored sources.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var conversation = await _conversations.GetAsync(id, cancellationToken);

        return conversation is null ? NotFound() : Ok(conversation);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        return await _conversations.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/messages")]
    [ProducesResponseType(typeof(List<ChatMessageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ChatMessageResponse>>> GetMessages(
        Guid id,
        CancellationToken cancellationToken)
    {
        var messages = await _conversations.GetMessagesAsync(id, cancellationToken);

        return messages is null ? NotFound() : Ok(messages);
    }

    /// <summary>
    /// Asks a question and streams the answer as it is written, as Server-Sent Events.
    ///
    /// Two event types: <c>delta</c> carries the next piece of text, and <c>final</c> carries the
    /// stored message with its citations once the answer is complete. Citations come last because
    /// they are the passages the answer cited, which is not known until it has finished writing.
    ///
    /// Closing the connection cancels the request, which cancels the call to the provider. Whatever
    /// text had arrived by then is still saved, marked as stopped.
    ///
    /// Kept out of the OpenAPI document: the response is an event stream, not the JSON body a
    /// generated client would expect, so the browser calls it directly.
    /// </summary>
    [HttpPost("{id:guid}/messages/stream")]
    [EnableRateLimiting(HardeningExtensions.AiPolicy)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task StreamAsk(
        Guid id,
        AskInConversationRequest request,
        CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";

        // Tells a reverse proxy not to buffer the stream; without it the answer can arrive in one
        // lump at the end, which defeats the point.
        Response.Headers["X-Accel-Buffering"] = "no";

        await foreach (var change in _conversations.StreamAskAsync(id, request, cancellationToken))
        {
            // The reader may already have gone. Writing to a closed connection would throw, and
            // there is nothing left to tell them anyway.
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            switch (change)
            {
                case ConversationStreamEvent.Delta delta:
                    await SendAsync("delta", new { text = delta.Text }, cancellationToken);
                    break;

                case ConversationStreamEvent.Final final:
                    await SendAsync("final", final.Message, cancellationToken);
                    break;
            }
        }
    }

    /// <summary>One SSE frame, flushed immediately so it reaches the browser as it is produced.</summary>
    private async Task SendAsync(string name, object payload, CancellationToken cancellationToken)
    {
        var data = JsonSerializer.Serialize(payload, SseJson);

        await Response.WriteAsync($"event: {name}\ndata: {data}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Asks a question in this conversation and returns the answer.
    ///
    /// The request carries only the question: the role is assigned by the server, so a client cannot
    /// write a message as the assistant and have it replayed as history on later questions.
    /// </summary>
    [HttpPost("{id:guid}/messages")]
    [EnableRateLimiting(HardeningExtensions.AiPolicy)]
    [ProducesResponseType(typeof(ChatMessageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ChatMessageResponse>> Ask(
        Guid id,
        AskInConversationRequest request,
        CancellationToken cancellationToken)
    {
        var message = await _conversations.AskAsync(id, request, cancellationToken);

        if (message is null)
        {
            return NotFound();
        }

        return CreatedAtAction(nameof(GetMessages), new { id }, message);
    }
}
