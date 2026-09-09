using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DocuMind.Application.Common;
using DocuMind.Application.Interfaces;

namespace DocuMind.Api.Authentication;

/// <summary>
/// Reads the caller's id from the validated bearer token. This is the only place a user id enters
/// the application, which is what makes "never trust a client-supplied UserId" enforceable.
/// </summary>
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid? UserId
    {
        get
        {
            var principal = _accessor.HttpContext?.User;

            // ASP.NET Core remaps "sub" to ClaimTypes.NameIdentifier by default, so check both.
            var value = principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);

            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => UserId is not null;

    public Guid RequireUserId()
    {
        return UserId ?? throw new UnauthorizedAppException(
            "This operation requires an authenticated user.",
            "AUTHENTICATION_REQUIRED");
    }
}
