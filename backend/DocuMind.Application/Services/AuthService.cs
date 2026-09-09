using DocuMind.Application.Common;
using DocuMind.Application.DTOs.Auth;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;

namespace DocuMind.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUser _currentUser;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ICurrentUser currentUser)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _currentUser = currentUser;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _users.EmailExistsAsync(email))
        {
            throw new ConflictAppException(
                "An account with that email already exists.",
                "EMAIL_ALREADY_REGISTERED");
        }

        var user = User.Create(
            request.Name,
            email,
            _passwordHasher.Hash(request.Password));

        await _users.AddAsync(user);
        await _users.SaveChangesAsync();

        return await IssueTokensAsync(user);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var user = await _users.GetByEmailAsync(request.Email);

        // Verify against a dummy hash when the user is missing so that a wrong email and a wrong
        // password take the same amount of time — otherwise the endpoint leaks which emails exist.
        var passwordMatches = user is null
            ? _passwordHasher.Verify(request.Password, DummyHash)
            : _passwordHasher.Verify(request.Password, user.PasswordHash);

        if (user is null || !passwordMatches)
        {
            throw new UnauthorizedAppException("Email or password is incorrect.", "INVALID_CREDENTIALS");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAppException("This account has been deactivated.", "ACCOUNT_INACTIVE");
        }

        return await IssueTokensAsync(user);
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedAppException("No refresh token was supplied.", "REFRESH_TOKEN_MISSING");
        }

        var hash = _tokenService.HashRefreshToken(refreshToken);
        var stored = await _refreshTokens.GetByHashAsync(hash)
            ?? throw new UnauthorizedAppException("That refresh token is not valid.", "REFRESH_TOKEN_INVALID");

        // A token that was already used is either a replay or a stolen copy. Revoking the whole
        // chain logs the real user out too, which is the safe response to a suspected theft.
        if (stored.RevokedAt is not null)
        {
            await _refreshTokens.RevokeAllForUserAsync(stored.UserId);
            await _refreshTokens.SaveChangesAsync();

            throw new UnauthorizedAppException(
                "That refresh token has already been used. Please sign in again.",
                "REFRESH_TOKEN_REUSED");
        }

        if (!stored.IsActive)
        {
            throw new UnauthorizedAppException("That refresh token has expired.", "REFRESH_TOKEN_EXPIRED");
        }

        var user = await _users.GetByIdAsync(stored.UserId);

        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAppException("This account is no longer active.", "ACCOUNT_INACTIVE");
        }

        var result = await IssueTokensAsync(user, rotating: stored);
        return result;
    }

    public async Task LogoutAsync(string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var hash = _tokenService.HashRefreshToken(refreshToken);
        var stored = await _refreshTokens.GetByHashAsync(hash);

        if (stored is null || stored.RevokedAt is not null)
        {
            // Nothing to revoke. Logout is idempotent and never reports why.
            return;
        }

        stored.Revoke();
        await _refreshTokens.SaveChangesAsync();
    }

    public async Task<UserResponse> GetCurrentUserAsync()
    {
        var user = await LoadCurrentUserAsync();
        return MapToResponse(user);
    }

    public async Task<UserResponse> UpdateProfileAsync(UpdateProfileRequest request)
    {
        var user = await LoadCurrentUserAsync();

        user.UpdateProfile(request.Name);
        await _users.SaveChangesAsync();

        return MapToResponse(user);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request)
    {
        var user = await LoadCurrentUserAsync();

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new ValidationAppException("Your current password is incorrect.", "CURRENT_PASSWORD_INCORRECT");
        }

        user.ChangePassword(_passwordHasher.Hash(request.NewPassword));
        await _users.SaveChangesAsync();

        // Changing a password invalidates every existing session.
        await _refreshTokens.RevokeAllForUserAsync(user.Id);
        await _refreshTokens.SaveChangesAsync();
    }

    /// <summary>
    /// Issues a fresh access/refresh pair. When <paramref name="rotating"/> is supplied that token
    /// is revoked and linked to its replacement, so a rotation chain can be followed.
    /// </summary>
    private async Task<AuthResult> IssueTokensAsync(User user, RefreshToken? rotating = null)
    {
        var (accessToken, accessExpiresAt) = _tokenService.CreateAccessToken(user);
        var (refreshToken, refreshHash, refreshExpiresAt) = _tokenService.CreateRefreshToken();

        var stored = RefreshToken.Issue(user.Id, refreshHash, refreshExpiresAt);
        await _refreshTokens.AddAsync(stored);

        rotating?.Revoke(stored.Id);

        await _refreshTokens.SaveChangesAsync();

        return new AuthResult
        {
            Response = new AuthResponse
            {
                AccessToken = accessToken,
                ExpiresAt = accessExpiresAt,
                User = MapToResponse(user)
            },
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt
        };
    }

    private async Task<User> LoadCurrentUserAsync()
    {
        var userId = _currentUser.RequireUserId();

        return await _users.GetByIdAsync(userId)
            ?? throw new NotFoundAppException("User not found.", "USER_NOT_FOUND");
    }

    private static UserResponse MapToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }

    /// <summary>
    /// A real PBKDF2 hash of a throwaway value. Verifying against it costs the same as verifying a
    /// genuine password, which is what keeps login timing constant for unknown emails.
    /// </summary>
    private const string DummyHash =
        "AQAAAAIAAYagAAAAEB1nCJ0hZ6ct0Vd1AFxKz8Xk1kQ6h2h/pQ0Xh5N0kQZ1kQZ1kQZ1kQZ1kQZ1kQZ1kQ==";
}
