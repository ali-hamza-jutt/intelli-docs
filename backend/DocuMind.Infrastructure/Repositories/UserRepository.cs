using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using DocuMind.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DocuMindDbContext _context;

    public UserRepository(DocuMindDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var normalised = email.Trim().ToLowerInvariant();

        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalised);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        var normalised = email.Trim().ToLowerInvariant();

        return await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == normalised);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
