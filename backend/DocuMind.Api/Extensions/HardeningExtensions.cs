using System.Globalization;
using System.Threading.RateLimiting;
using DocuMind.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using Serilog.Events;

namespace DocuMind.Api.Extensions;

public static class HardeningExtensions
{
    /// <summary>Limits the endpoints that cost money or disk, named so controllers can opt in.</summary>
    public const string UploadPolicy = "upload";
    public const string AiPolicy = "ai";

    /// <summary>
    /// Structured logging.
    ///
    /// Nothing that identifies a person or unlocks anything is written: no tokens, no keys, no
    /// document text, no questions. What is written is the shape of the traffic — who, what endpoint,
    /// what status, how long — which is what makes a failure on a background thread findable.
    /// </summary>
    public static void AddStructuredLogging(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, configuration) => configuration
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            // EF Core narrates every query it runs. Useful while building a query, noise once it
            // works, and it prints enough of the SQL to be worth keeping out of a shared log.
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .WriteTo.Console(
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                formatProvider: CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// One line per request, with the signed-in user attached so a report of "it failed for me" can
    /// be traced. The user id is an opaque identifier, not a name or an email.
    /// </summary>
    public static void UseRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "{RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0} ms";

            options.EnrichDiagnosticContext = (diagnostics, context) =>
            {
                var user = context.RequestServices.GetService<ICurrentUser>();

                if (user?.UserId is { } userId)
                {
                    diagnostics.Set("UserId", userId);
                }
            };

            // A 4xx is the API working as designed; only a 5xx deserves an error line.
            options.GetLevel = (_, _, exception) => exception is null
                ? LogEventLevel.Information
                : LogEventLevel.Error;
        });
    }

    /// <summary>
    /// The AI limit sits just under what the provider itself allows a free account per minute, so a
    /// burst is refused here — with a sentence written for the reader — rather than upstream.
    ///
    /// Rate limits on the endpoints where abuse is expensive rather than merely rude: uploads write
    /// to storage and start a pipeline, and anything that reaches the AI provider is billed per call.
    ///
    /// Partitioned per signed-in user, falling back to the remote address for anonymous calls, so one
    /// account cannot spend another's allowance.
    /// </summary>
    public static IServiceCollection AddEndpointRateLimits(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(UploadPolicy, PerCaller(permitLimit: 20, window: TimeSpan.FromMinutes(1)));
            options.AddPolicy(AiPolicy, PerCaller(permitLimit: 12, window: TimeSpan.FromMinutes(1)));

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                // Same shape as every other error, so a client needs no special case for it.
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests",
                    Detail = "You are doing that too often. Wait a moment and try again.",
                    Instance = context.HttpContext.Request.Path,
                    Extensions =
                    {
                        ["errorCode"] = "RATE_LIMITED",
                        ["traceId"] = context.HttpContext.TraceIdentifier
                    }
                };

                await context.HttpContext.Response.WriteAsJsonAsync(
                    problem, cancellationToken: cancellationToken);
            };
        });

        return services;
    }

    /// <summary>
    /// A fixed window per caller. Fixed rather than sliding because the limit is about cost, and a
    /// simple "this many per minute" is what a user can be told in a message.
    /// </summary>
    private static Func<HttpContext, RateLimitPartition<string>> PerCaller(
        int permitLimit,
        TimeSpan window)
    {
        return context => RateLimitPartition.GetFixedWindowLimiter(
            PartitionKeyFor(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueLimit = 0
            });
    }

    private static string PartitionKeyFor(HttpContext context)
    {
        var userId = context.RequestServices.GetService<ICurrentUser>()?.UserId;

        return userId?.ToString()
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";
    }
}
