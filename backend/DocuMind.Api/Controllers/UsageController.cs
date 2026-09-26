using DocuMind.Application.DTOs.Usage;
using DocuMind.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsageController : ControllerBase
{
    private readonly IUsageRecorder _usage;
    private readonly ICurrentUser _currentUser;

    public UsageController(IUsageRecorder usage, ICurrentUser currentUser)
    {
        _usage = usage;
        _currentUser = currentUser;
    }

    /// <summary>
    /// What the caller has used this calendar month, or since <paramref name="since"/>.
    ///
    /// Always the caller's own usage: there is no parameter for whose to read, because there is no
    /// case where one account should see another's.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(UsageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UsageResponse>> Get(
        [FromQuery] DateTime? since,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var from = since?.ToUniversalTime() ?? new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var summary = await _usage.SummariseAsync(
            _currentUser.RequireUserId(), from, cancellationToken);

        return Ok(new UsageResponse
        {
            Since = summary.Since,
            EmbeddingCalls = summary.EmbeddingCalls,
            EmbeddingInputTokens = summary.EmbeddingInputTokens,
            ChatCalls = summary.ChatCalls,
            ChatInputTokens = summary.ChatInputTokens,
            ChatOutputTokens = summary.ChatOutputTokens
        });
    }
}
