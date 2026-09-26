using DocuMind.Application.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.Api.Extensions;

public static class ModelValidationExtensions
{
    /// <summary>
    /// Makes a rejected request read like every other error: ProblemDetails, with the reason in
    /// <c>detail</c> and an <c>errorCode</c> beside it.
    ///
    /// The default fills in a generic title and leaves the reason only in the per-field list, so a
    /// client that shows <c>detail</c> — as this one's does — would show nothing at all. The field
    /// list is still returned, because a form needs to know which input to mark.
    /// </summary>
    public static IServiceCollection AddConsistentValidationErrors(this IServiceCollection services)
    {
        // Validators are found by scanning the Application project, so adding one is adding a
        // file. Presence is still the framework's job, via [Required] on the request itself;
        // everything about what a value may contain lives in a validator.
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var problem = new ValidationProblemDetails(context.ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid request",
                    Detail = FirstMessage(context) ?? "Some of the submitted values are not valid.",
                    Instance = context.HttpContext.Request.Path
                };

                problem.Extensions["errorCode"] = "VALIDATION_FAILED";
                problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                return new BadRequestObjectResult(problem);
            };
        });

        return services;
    }

    /// <summary>
    /// The most useful message of the lot.
    ///
    /// A body that could not be deserialized produces two errors: one about the whole request being
    /// missing, and one naming the field that failed. The second is the reason, so anything reported
    /// against a JSON path wins over the generic complaint about the request itself.
    /// </summary>
    private static string? FirstMessage(ActionContext context)
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors.Select(error => (entry.Key, error.ErrorMessage)))
            .Where(item => !string.IsNullOrWhiteSpace(item.ErrorMessage))
            .ToList();

        return errors
            .Where(item => item.Key.StartsWith('$'))
            .Select(item => item.ErrorMessage)
            .Concat(errors.Select(item => item.ErrorMessage))
            .FirstOrDefault();
    }
}
