using DocuMind.Domain.Entities;

namespace DocuMind.Application.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(User user);

    /// <summary>Returns the raw token to hand to the client and the hash to persist.</summary>
    (string Token, string TokenHash, DateTime ExpiresAt) CreateRefreshToken();

    /// <summary>Hashes a raw refresh token so it can be matched against stored hashes.</summary>
    string HashRefreshToken(string token);
}
