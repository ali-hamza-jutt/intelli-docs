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

    public async Task<List<Document>> GetAllAsync()
    {
        return await _context.Documents
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Document?> GetByIdAsync(Guid id)
    {
        return await _context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);
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