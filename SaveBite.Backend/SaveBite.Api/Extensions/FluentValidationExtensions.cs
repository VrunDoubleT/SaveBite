using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Filters;

namespace SaveBite.Backend.Extensions;

public static class FluentValidationExtensions
{
    public static IServiceCollection AddFluentValidation(
        this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Scoped);

        services.Configure<MvcOptions>(options =>
        {
            options.Filters.Add<ValidationFilter>();
        });

        ValidatorOptions.Global.PropertyNameResolver = (type, member, expression) =>
        {
            if (member is null) return null;
            var name = member.Name;
            return char.ToLowerInvariant(name[0]) + name[1..];
        };

        ValidatorOptions.Global.DisplayNameResolver = (type, member, expression) =>
            member?.Name;

        return services;
    }
}