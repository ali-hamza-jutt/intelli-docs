using System.ComponentModel.DataAnnotations;

namespace DocuMind.Application.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    public string NewPassword { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
}

public class UserResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required DateTime CreatedAt { get; set; }
}

/// <summary>
/// What the API hands back after a successful register/login/refresh. The refresh token is
/// deliberately absent — it travels in an httpOnly cookie the browser cannot read.
/// </summary>
public class AuthResponse
{
    public required string AccessToken { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public required UserResponse User { get; set; }
}

/// <summary>Internal result carrying the raw refresh token so the API can set the cookie.</summary>
public class AuthResult
{
    public required AuthResponse Response { get; set; }
    public required string RefreshToken { get; set; }
    public required DateTime RefreshTokenExpiresAt { get; set; }
}
