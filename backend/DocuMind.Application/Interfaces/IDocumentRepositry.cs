using DocuMind.Domain.Entities;

namespace DocuMind.Application.Interfaces;

public interface IDocumentRepository
{
    Task AddAsync(Document document);

    Task<List<Document>> GetAllAsync();

    Task<Document?> GetByIdAsync(Guid id);

    Task DeleteAsync(Document document);

    Task SaveChangesAsync();
}