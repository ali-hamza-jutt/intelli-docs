using Microsoft.AspNetCore.Http;

namespace DocuMind.Api.Extensions;

/// <summary>
/// Fills in what a bare status result leaves out.
///
/// <c>return NotFound()</c> produces a ProblemDetails with a title and nothing else — no code to
/// branch on, no sentence to show. Rather than rewriting every action to throw instead, the gap is
/// closed here, so a client sees one shape whether a failure was returned or raised.
/// </summary>
public static class ProblemDetailsDefaults
{
    /// <summary>
    /// A code and a readable sentence per status. Deliberately vague where being specific would say
    /// something about data the caller cannot see — a 404 never hints at whether the id exists.
    /// </summary>
    private static readonly Dictionary<int, (string ErrorCode, string Detail)> Defaults = new()
    {
        [StatusCodes.Status400BadRequest] = ("BAD_REQUEST", "That request could not be understood."),
        [StatusCodes.Status401Unauthorized] = ("UNAUTHORIZED", "Sign in to continue."),
        [StatusCodes.Status403Forbidden] = ("FORBIDDEN", "You do not have access to that."),
        [StatusCodes.Status404NotFound] = ("NOT_FOUND", "That was not found."),
        [StatusCodes.Status405MethodNotAllowed] = ("METHOD_NOT_ALLOWED", "That is not allowed here."),
        [StatusCodes.Status409Conflict] = ("CONFLICT", "That conflicts with the current state."),
        [StatusCodes.Status413PayloadTooLarge] = ("TOO_LARGE", "That file is too large."),
        [StatusCodes.Status415UnsupportedMediaType] = ("UNSUPPORTED_MEDIA_TYPE", "That format is not supported."),
        [StatusCodes.Status429TooManyRequests] = ("RATE_LIMITED", "You are doing that too often. Wait a moment and try again."),
        [StatusCodes.Status500InternalServerError] = ("INTERNAL_ERROR", "Something went wrong. Please try again."),
        [StatusCodes.Status503ServiceUnavailable] = ("SERVICE_UNAVAILABLE", "That service is unavailable. Try again shortly.")
    };

    public static void Apply(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;

        // Quotable in a support message, and the same value the log line for this request carries.
        problem.Extensions["traceId"] ??= context.HttpContext.TraceIdentifier;
        problem.Instance ??= context.HttpContext.Request.Path;

        var status = problem.Status ?? context.HttpContext.Response.StatusCode;

        if (!Defaults.TryGetValue(status, out var fallback))
        {
            return;
        }

        if (!problem.Extensions.ContainsKey("errorCode"))
        {
            problem.Extensions["errorCode"] = fallback.ErrorCode;
        }

        // Only when the failure did not already explain itself: a message written for this exact
        // case is always better than the generic one.
        if (string.IsNullOrWhiteSpace(problem.Detail))
        {
            problem.Detail = fallback.Detail;
        }
    }
}
