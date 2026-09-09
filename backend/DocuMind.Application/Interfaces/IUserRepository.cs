using DocuMind.Domain.Entities;

namespace DocuMind.Application.Interfaces;

public interface IUserRepository
{
    Task AddAsync(User user);

    Task<User?> GetByIdAsync(Guid id);

    /// <summary>Case-insensitive: the implementation normalises before comparing.</summary>
    Task<User?> GetByEmailAsync(string email);

    Task<bool> EmailExistsAsync(string email);

    Task SaveChangesAsync();
}
