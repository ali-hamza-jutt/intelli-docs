using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace DocuMind.Api.Extensions;

/// <summary>
/// Runs the validator for each argument an action was handed, if there is one.
///
/// Written as a filter rather than using FluentValidation's own ASP.NET integration, which its
/// authors deprecated: doing it here is a dozen lines, keeps validation on one code path, and answers
/// with the same ProblemDetails shape as every other rejected request — by reusing the very factory
/// that shapes those.
/// </summary>
public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _services;

    public ValidationFilter(IServiceProvider services)
    {
        _services = services;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());

            if (_services.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(
                validationContext, context.HttpContext.RequestAborted);

            foreach (var failure in result.Errors)
            {
                context.ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }
        }

        if (!context.ModelState.IsValid)
        {
            var behaviour = _services.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;

            context.Result = behaviour.InvalidModelStateResponseFactory(context);

            return;
        }

        await next();
    }
}
