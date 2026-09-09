namespace DocuMind.Application.Interfaces;

/// <summary>
/// Kept as an abstraction so the hashing algorithm can be replaced without touching AuthService,
/// and so tests can substitute a fast fake.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
