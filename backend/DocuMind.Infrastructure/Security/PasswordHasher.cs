using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace DocuMind.Infrastructure.Security;

/// <summary>
/// Wraps ASP.NET Core Identity's PBKDF2 hasher. Using the framework implementation rather than a
/// hand-rolled one means salting, iteration count and the constant-time comparison are all handled,
/// and the format carries its own version byte so the algorithm can be upgraded later.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password)
    {
        // The user argument is unused by the PBKDF2 implementation; it exists for custom hashers.
        return _hasher.HashPassword(null!, password);
    }

    public bool Verify(string password, string passwordHash)
    {
        try
        {
            var result = _hasher.VerifyHashedPassword(null!, passwordHash, password);

            return result is PasswordVerificationResult.Success
                or PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (FormatException)
        {
            // A malformed stored hash must fail closed, not throw a 500.
            return false;
        }
    }
}
