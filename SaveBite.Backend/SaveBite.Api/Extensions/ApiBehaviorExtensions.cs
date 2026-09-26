using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Extensions;

public static class ApiBehaviorExtensions
{
    public static IServiceCollection AddApiBehavior(
        this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(x => x.Value is { Errors.Count: > 0 })
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value!.Errors
                            .Select(error =>
                                string.IsNullOrWhiteSpace(error.ErrorMessage)
                                    ? "Invalid value."
                                    : error.ErrorMessage)
                            .ToArray());

                return new BadRequestObjectResult(
                    ApiResponse.ValidationFail(errors));
            };
        });

        return services;
    }
}