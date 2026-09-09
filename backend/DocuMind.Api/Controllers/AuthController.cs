using DocuMind.Application.DTOs.Auth;
using DocuMind.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    /// <summary>
    /// The refresh token travels in this cookie rather than the response body, so no script in the
    /// browser — including an injected one — can read it.
    /// </summary>
    private const string RefreshCookieName = "documind_refresh";

    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        IAuthService authService,
        IWebHostEnvironment environment)
    {
        _authService = authService;
        _environment = environment;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        SetRefreshCookie(result.RefreshToken, result.RefreshTokenExpiresAt);

        return Ok(result.Response);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        SetRefreshCookie(result.RefreshToken, result.RefreshTokenExpiresAt);

        return Ok(result.Response);
    }

    /// <summary>Exchanges the refresh cookie for a new access token and rotates the cookie.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh()
    {
        var token = Request.Cookies[RefreshCookieName];
        var result = await _authService.RefreshAsync(token ?? string.Empty);

        SetRefreshCookie(result.RefreshToken, result.RefreshTokenExpiresAt);

        return Ok(result.Response);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync(Request.Cookies[RefreshCookieName]);
        Response.Cookies.Delete(RefreshCookieName, BuildCookieOptions(DateTimeOffset.UnixEpoch));

        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> Me()
    {
        return Ok(await _authService.GetCurrentUserAsync());
    }

    [HttpPut("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserResponse>> UpdateProfile(UpdateProfileRequest request)
    {
        return Ok(await _authService.UpdateProfileAsync(request));
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        await _authService.ChangePasswordAsync(request);

        // Every session was just invalidated, so drop this browser's cookie too.
        Response.Cookies.Delete(RefreshCookieName, BuildCookieOptions(DateTimeOffset.UnixEpoch));

        return NoContent();
    }

    private void SetRefreshCookie(string token, DateTime expiresAt)
    {
        Response.Cookies.Append(RefreshCookieName, token, BuildCookieOptions(expiresAt));
    }

    private CookieOptions BuildCookieOptions(DateTimeOffset expiresAt)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Expires = expiresAt,

            // The cookie is only ever sent to the auth endpoints, so it is not attached to every
            // request in the app.
            Path = "/api/auth",

            // The SPA runs on a different origin in development, and a cross-site cookie must be
            // SameSite=None, which browsers only accept when it is also Secure. Over plain HTTP
            // that combination is rejected, so development falls back to Lax.
            Secure = !_environment.IsDevelopment(),
            SameSite = _environment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None
        };
    }
}
