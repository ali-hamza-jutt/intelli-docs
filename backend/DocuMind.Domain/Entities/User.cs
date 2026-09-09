namespace DocuMind.Domain.Entities;

public class User
{
    // EF Core materialises through this; application code uses Create.
    private User() { }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    /// <summary>Always stored lower-cased so lookups and the unique index are case-insensitive.</summary>
    public string Email { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public static User Create(string name, string email, string passwordHash)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProfile(string name)
    {
        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
