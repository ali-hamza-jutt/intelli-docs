using DocuMind.Application.DTOs.Documents;
using DocuMind.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.Api.Controllers;

[ApiController]
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
    public async Task<IActionResult> Create(
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
    public async Task<IActionResult> GetAll()
    {
        var documents =
            await _documentService.GetAllAsync();

        return Ok(documents);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var document =
            await _documentService.GetByIdAsync(id);

        if (document is null)
        {
            return NotFound();
        }

        return Ok(document);
    }

    [HttpDelete("{id:guid}")]
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