using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using DocuMind.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly DocuMindDbContext _context;

    public DocumentRepository(DocuMindDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Document document)
    {
        await _context.Documents.AddAsync(document);
    }

    public async Task<List<Document>> GetAllForUserAsync(Guid userId)
    {
        return await _context.Documents
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<Document?> GetByIdAsync(Guid id)
    {
        return await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Document?> GetByIdForUserAsync(Guid id, Guid userId)
    {
        // The owner predicate is part of the lookup, so there is no window in which a document
        // belonging to someone else has been loaded and is waiting to be checked.
        return await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
    }

    public async Task DeleteAsync(Document document)
    {
        _context.Documents.Remove(document);

        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
