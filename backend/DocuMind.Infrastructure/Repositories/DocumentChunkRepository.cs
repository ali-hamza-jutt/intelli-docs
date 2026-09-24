using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using DocuMind.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Repositories;

public class DocumentChunkRepository : IDocumentChunkRepository
{
    private readonly DocuMindDbContext _context;

    public DocumentChunkRepository(DocuMindDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IEnumerable<DocumentChunk> chunks)
    {
        await _context.DocumentChunks.AddRangeAsync(chunks);
    }

    public async Task<List<DocumentChunk>> GetForDocumentAsync(Guid documentId, int offset, int limit)
    {
        return await _context.DocumentChunks
            .AsNoTracking()
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.ChunkIndex)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> CountForDocumentAsync(Guid documentId)
    {
        return await _context.DocumentChunks
            .CountAsync(c => c.DocumentId == documentId);
    }

    public async Task RemoveForDocumentAsync(Guid documentId)
    {
        await _context.DocumentChunks
            .Where(c => c.DocumentId == documentId)
            .ExecuteDeleteAsync();
    }
}
