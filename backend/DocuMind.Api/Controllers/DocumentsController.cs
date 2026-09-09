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

    [HttpPost]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DocumentResponse>> Create(
        CreateDocumentRequest request)
    {
        var document =
            await _documentService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetById),
            new { id = document.Id },
            document
        );
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<DocumentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<DocumentResponse>>> GetAll()
    {
        var documents =
            await _documentService.GetAllAsync();

        return Ok(documents);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentResponse>> GetById(Guid id)
    {
        var document =
            await _documentService.GetByIdAsync(id);

        // A document owned by another user is reported as missing, never as forbidden.
        if (document is null)
        {
            return NotFound();
        }

        return Ok(document);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted =
            await _documentService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
