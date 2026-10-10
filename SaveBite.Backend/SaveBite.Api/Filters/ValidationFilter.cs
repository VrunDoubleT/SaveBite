using SaveBite.Backend.Models.Common;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SaveBite.Backend.Filters;

public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationFilter(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider;

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var errors = new Dictionary<string, List<string>>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());

            if (_serviceProvider.GetService(validatorType) is not IValidator validator)
                continue; 
            
            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(
                validationContext, context.HttpContext.RequestAborted);

            if (result.IsValid) continue;

            foreach (var failure in result.Errors)
            {
                var key = failure.PropertyName;

                if (!errors.TryGetValue(key, out var messages))
                {
                    messages = new List<string>();
                    errors[key] = messages;
                }

                messages.Add(failure.ErrorMessage);
            }
        }

        if (errors.Count > 0)
        {
            var payload = ApiResponse.ValidationFail(
                errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));

            context.Result = new BadRequestObjectResult(payload);
            return;
        }

        await next();
    }
}