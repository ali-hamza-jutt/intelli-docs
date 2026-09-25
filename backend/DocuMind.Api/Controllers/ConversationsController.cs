using DocuMind.Application.DTOs.Conversations;
using DocuMind.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ConversationsController : ControllerBase
{
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
    /// Asks a question in this conversation and returns the answer.
    ///
    /// The request carries only the question: the role is assigned by the server, so a client cannot
    /// write a message as the assistant and have it replayed as history on later questions.
    /// </summary>
    [HttpPost("{id:guid}/messages")]
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
