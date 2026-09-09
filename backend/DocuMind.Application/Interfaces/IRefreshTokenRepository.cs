using DocuMind.Domain.Entities;

namespace DocuMind.Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);

    /// <summary>Looks up by hash — the raw token is never stored, so it cannot be queried directly.</summary>
    Task<RefreshToken?> GetByHashAsync(string tokenHash);

    /// <summary>Used on logout-everywhere and on reuse detection.</summary>
    Task RevokeAllForUserAsync(Guid userId);

    Task SaveChangesAsync();
}
