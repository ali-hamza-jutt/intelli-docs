using DocuMind.Api.Extensions;
using DocuMind.Application.DTOs.Search;
using DocuMind.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DocuMind.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly IVectorSearchService _search;
    private readonly ICurrentUser _currentUser;

    public SearchController(IVectorSearchService search, ICurrentUser currentUser)
    {
        _search = search;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Finds the passages closest in meaning to a question, across the caller's documents or within
    /// one of them.
    ///
    /// POST rather than GET: the query is the user's content, and a question does not belong in a
    /// URL that gets logged and cached.
    /// </summary>
    [HttpPost("semantic")]
    [EnableRateLimiting(HardeningExtensions.AiPolicy)]
    [ProducesResponseType(typeof(SemanticSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<SemanticSearchResponse>> Semantic(
        SemanticSearchRequest request,
        CancellationToken cancellationToken)
    {
        // The account searched comes from the token, never from the request — a document id in the
        // body can only narrow the search, never widen it to someone else's documents.
        var matches = await _search.SearchAsync(
            request.Query,
            _currentUser.RequireUserId(),
            request.TopK,
            request.DocumentId,
            cancellationToken);

        return Ok(new SemanticSearchResponse
        {
            Query = request.Query,
            SimilarityThreshold = _search.SimilarityThreshold,
            Matches = [.. matches.Select(match => new SemanticMatchResponse
            {
                DocumentId = match.DocumentId,
                FileName = match.FileName,
                ChunkIndex = match.ChunkIndex,
                PageNumber = match.PageNumber,
                EndPageNumber = match.EndPageNumber,
                Similarity = match.Similarity,
                Text = match.Text
            })]
        });
    }
}
