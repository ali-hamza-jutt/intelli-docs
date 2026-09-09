using DocuMind.Application.DTOs.Auth;

namespace DocuMind.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request);

    Task<AuthResult> LoginAsync(LoginRequest request);

    /// <summary>Rotates the token: the presented one is revoked and a fresh pair issued.</summary>
    Task<AuthResult> RefreshAsync(string refreshToken);

    Task LogoutAsync(string? refreshToken);

    Task<UserResponse> GetCurrentUserAsync();

    Task<UserResponse> UpdateProfileAsync(UpdateProfileRequest request);

    Task ChangePasswordAsync(ChangePasswordRequest request);
}
