using DocuMind.Application.DTOs.Documents;
using DocuMind.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentsController(
        IDocumentService documentService)
    {
        _documentService = documentService;
    }

    /// <summary>
    /// Signs a short-lived permission for the browser to upload straight to the storage provider.
    /// The response carries no secret — only a signature over the exact upload it authorises.
    /// </summary>
    [HttpPost("upload-ticket")]
    [ProducesResponseType(typeof(UploadTicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<UploadTicketResponse> CreateUploadTicket(UploadTicketRequest request)
    {
        return Ok(_documentService.CreateUploadTicket(request));
    }

    /// <summary>
    /// Registers a document after the browser finished uploading, then queues it for processing.
    /// The asset is verified with the provider, so nothing here depends on the client telling the
    /// truth.
    /// </summary>
    [HttpPost("confirm")]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentResponse>> ConfirmUpload(
        ConfirmUploadRequest request,
        CancellationToken cancellationToken)
    {
        var document = await _documentService.ConfirmUploadAsync(request, cancellationToken);

        return AcceptedAtAction(nameof(GetById), new { id = document.Id }, document);
    }

    /// <summary>
    /// Accepts a PDF and queues it for processing. Returns 202 rather than 201 because the
    /// document is not usable yet — the client polls /status until it settles.
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DocumentResponse>> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "No file was uploaded.",
                errorCode = "FILE_REQUIRED"
            });
        }

        await using var content = file.OpenReadStream();

        // IFormFile stays in the API layer; the application receives a plain stream.
        var document = await _documentService.UploadAsync(
            new UploadDocumentRequest
            {
                Content = content,
                FileName = file.FileName,
                DeclaredContentType = file.ContentType,
                Length = file.Length
            },
            cancellationToken);

        return AcceptedAtAction(nameof(GetById), new { id = document.Id }, document);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<DocumentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<DocumentResponse>>> GetAll()
    {
        return Ok(await _documentService.GetAllAsync());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentResponse>> GetById(Guid id)
    {
        var document = await _documentService.GetByIdAsync(id);

        // A document owned by another user is reported as missing, never as forbidden.
        if (document is null)
        {
            return NotFound();
        }

        return Ok(document);
    }

    /// <summary>Polled by the documents screen while a file is being processed.</summary>
    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(typeof(DocumentStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentStatusResponse>> GetStatus(Guid id)
    {
        var status = await _documentService.GetStatusAsync(id);

        if (status is null)
        {
            return NotFound();
        }

        return Ok(status);
    }

    /// <summary>Extracted text for the document detail preview.</summary>
    [HttpGet("{id:guid}/text")]
    [ProducesResponseType(typeof(DocumentTextResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentTextResponse>> GetText(Guid id)
    {
        var text = await _documentService.GetTextAsync(id);

        // Also 404 while the document is still processing — there is genuinely nothing yet.
        if (text is null)
        {
            return NotFound();
        }

        return Ok(text);
    }

    /// <summary>
    /// The document's chunks in reading order, paged. Mainly for inspecting how a document was
    /// split — where the boundaries fell and how much each chunk repeats of the one before.
    /// </summary>
    [HttpGet("{id:guid}/chunks")]
    [ProducesResponseType(typeof(DocumentChunksResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentChunksResponse>> GetChunks(
        Guid id,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 20)
    {
        // A long document can produce thousands of chunks; never return them all at once.
        offset = Math.Max(0, offset);
        limit = Math.Clamp(limit, 1, 100);

        var chunks = await _documentService.GetChunksAsync(id, offset, limit);

        if (chunks is null)
        {
            return NotFound();
        }

        return Ok(chunks);
    }

    /// <summary>Re-runs extraction, discarding any previous result.</summary>
    [HttpPost("{id:guid}/reprocess")]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentResponse>> Reprocess(Guid id, CancellationToken cancellationToken)
    {
        if (!await _documentService.ReprocessAsync(id, cancellationToken))
        {
            return NotFound();
        }

        return Ok(await _documentService.GetByIdAsync(id));
    }

    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var download = await _documentService.DownloadAsync(id, cancellationToken);

        if (download is null)
        {
            return NotFound();
        }

        return File(download.Content, download.ContentType, download.FileName);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _documentService.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
