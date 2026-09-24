using System.Text.Json;
using DocuMind.Application.Common;
using DocuMind.Application.Interfaces;

namespace DocuMind.Api.Middleware;

/// <summary>
/// Turns application exceptions into a consistent JSON shape and stops anything unexpected from
/// leaking a stack trace, a connection string or a file path to the client.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            // Expected failures: the message is written for the user and is safe to return.
            _logger.LogInformation("Handled {ErrorCode}: {Message}", ex.ErrorCode, ex.Message);
            await WriteAsync(context, StatusFor(ex), ex.Message, ex.ErrorCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteAsync(context, StatusCodes.Status500InternalServerError,
                "Something went wrong. Please try again.", "INTERNAL_ERROR");
        }
    }

    private static int StatusFor(AppException exception) => exception switch
    {
        ValidationAppException => StatusCodes.Status400BadRequest,
        UnauthorizedAppException => StatusCodes.Status401Unauthorized,
        NotFoundAppException => StatusCodes.Status404NotFound,
        ConflictAppException => StatusCodes.Status409Conflict,
        AiUnavailableAppException => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };

    private static async Task WriteAsync(HttpContext context, int status, string message, string errorCode)
    {
        if (context.Response.HasStarted)
        {
            // Too late to change the response — a streaming endpoint has already begun writing.
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(new { success = false, message, errorCode }, JsonOptions));
    }
}
