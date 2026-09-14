using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using DocuMind.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Repositories;

public class DocumentTextRepository : IDocumentTextRepository
{
    private readonly DocuMindDbContext _context;

    public DocumentTextRepository(DocuMindDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(DocumentText documentText)
    {
        await _context.DocumentTexts.AddAsync(documentText);
    }

    public async Task<DocumentText?> GetByDocumentIdAsync(Guid documentId)
    {
        // No pages: the preview only needs the combined text, and the page rows would double
        // the bytes read for nothing.
        return await _context.DocumentTexts
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.DocumentId == documentId);
    }

    public async Task<DocumentText?> GetWithPagesAsync(Guid documentId)
    {
        return await _context.DocumentTexts
            .AsNoTracking()
            .Include(t => t.Pages.OrderBy(p => p.PageNumber))
            .FirstOrDefaultAsync(t => t.DocumentId == documentId);
    }

    public async Task RemoveForDocumentAsync(Guid documentId)
    {
        // Pages go with it through the cascade configured on the relationship.
        await _context.DocumentTexts
            .Where(t => t.DocumentId == documentId)
            .ExecuteDeleteAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
