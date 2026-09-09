using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using DocuMind.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly DocuMindDbContext _context;

    public RefreshTokenRepository(DocuMindDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RefreshToken token)
    {
        await _context.RefreshTokens.AddAsync(token);
    }

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash)
    {
        // Tracked, because the caller revokes the entity it gets back.
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
    }

    public async Task RevokeAllForUserAsync(Guid userId)
    {
        var active = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();

        foreach (var token in active)
        {
            token.Revoke();
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
