namespace DocuMind.Application.Interfaces;

/// <summary>
/// The authenticated caller, resolved from the bearer token by the API layer. Application code
/// reads the user id from here and never from a request body or query string.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }

    /// <summary>The caller's id, or throws if the request is anonymous.</summary>
    Guid RequireUserId();
}
