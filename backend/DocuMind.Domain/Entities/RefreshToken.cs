namespace DocuMind.Domain.Entities;

/// <summary>
/// A long-lived token that buys new access tokens. Only the SHA-256 hash is stored, so a
/// leaked database dump cannot be replayed against the API.
/// </summary>
public class RefreshToken
{
    private RefreshToken() { }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public DateTime ExpiresAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    /// <summary>Set when the token is used or explicitly revoked; a token is single-use.</summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>Points at the token issued in its place, so a whole rotation chain is traceable.</summary>
    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;

    public static RefreshToken Issue(Guid userId, string tokenHash, DateTime expiresAt)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Revoke(Guid? replacedByTokenId = null)
    {
        RevokedAt = DateTime.UtcNow;
        ReplacedByTokenId = replacedByTokenId;
    }
}
