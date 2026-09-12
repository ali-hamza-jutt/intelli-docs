using Microsoft.AspNetCore.Mvc;

namespace DocuMind.Api.Extensions;

public static class ModelValidationExtensions
{
    /// <summary>
    /// Makes DataAnnotations failures return the same envelope as every other error
    /// — <c>{ success, message, errorCode }</c> — instead of RFC-9110 ProblemDetails.
    ///
    /// Without this the client has to understand two error formats, and the default message is
    /// written for a developer ("must be between 1 and 9.2233720368547758E+18") rather than a user.
    /// </summary>
    public static IServiceCollection AddConsistentValidationErrors(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var firstError = context.ModelState
                    .Where(entry => entry.Value?.Errors.Count > 0)
                    .Select(entry => entry.Value!.Errors[0].ErrorMessage)
                    .FirstOrDefault();

                // Field names are included so a multi-field form can highlight the right input.
                var fields = context.ModelState
                    .Where(entry => entry.Value?.Errors.Count > 0)
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

                return new BadRequestObjectResult(new
                {
                    success = false,
                    message = string.IsNullOrWhiteSpace(firstError)
                        ? "Some of the submitted values are not valid."
                        : firstError,
                    errorCode = "VALIDATION_FAILED",
                    fields
                });
            };
        });

        return services;
    }
}
