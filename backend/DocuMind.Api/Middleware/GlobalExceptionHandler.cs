using DocuMind.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.Api.Middleware;

/// <summary>
/// Turns an exception into a ProblemDetails response.
///
/// The rule this enforces is that nothing internal reaches the client. An expected failure carries a
/// message written for a user and is returned as-is; anything else becomes a flat "something went
/// wrong", with the real exception left in the log. That matters most for the failures nobody plans
/// for: a Postgres error message quotes the host and database it failed to reach, and an unhandled
/// exception would otherwise hand a stack trace to whoever asked for it.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetails,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            // The reader hung up — a stopped answer, a closed tab. Nothing to report and nobody to
            // report it to.
            _logger.LogDebug("Request aborted by the client: {Method} {Path}",
                context.Request.Method, context.Request.Path);

            return true;
        }

        var (status, title, detail, errorCode) = Describe(exception);

        if (exception is not AppException)
        {
            // Nobody planned for this one, so it gets the stack trace and the loudest level.
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
        else if (status >= StatusCodes.Status500InternalServerError)
        {
            // Something outside this system failed — the AI provider, typically. A warning, not an
            // error: the code behaved correctly, and calling it "unhandled" would send whoever reads
            // the log looking for a bug that is not there.
            _logger.LogWarning("{ErrorCode} on {Method} {Path}: {Message}",
                errorCode, context.Request.Method, context.Request.Path, detail);
        }
        else
        {
            // Expected outcomes are not errors. Logged at information so a stream of 404s is
            // visible without drowning the real failures.
            _logger.LogInformation("Handled {ErrorCode} on {Method} {Path}: {Message}",
                errorCode, context.Request.Method, context.Request.Path, detail);
        }

        context.Response.StatusCode = status;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Extensions = { ["errorCode"] = errorCode }
            }
        });
    }

    /// <summary>
    /// The only place an exception type decides a status code. An <see cref="AppException"/> was
    /// raised deliberately and its message is safe to show; anything else is not.
    /// </summary>
    private static (int Status, string Title, string Detail, string ErrorCode) Describe(
        Exception exception) => exception switch
    {
        ValidationAppException app =>
            (StatusCodes.Status400BadRequest, "Invalid request", app.Message, app.ErrorCode),
        UnauthorizedAppException app =>
            (StatusCodes.Status401Unauthorized, "Not signed in", app.Message, app.ErrorCode),
        NotFoundAppException app =>
            (StatusCodes.Status404NotFound, "Not found", app.Message, app.ErrorCode),
        ConflictAppException app =>
            (StatusCodes.Status409Conflict, "Conflict", app.Message, app.ErrorCode),
        AiUnavailableAppException app =>
            (StatusCodes.Status503ServiceUnavailable, "AI service unavailable", app.Message, app.ErrorCode),
        AppException app =>
            (StatusCodes.Status500InternalServerError, "Server error", app.Message, app.ErrorCode),
        _ => (
            StatusCodes.Status500InternalServerError,
            "Server error",
            "Something went wrong. Please try again.",
            "INTERNAL_ERROR")
    };
}
