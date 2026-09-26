using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.OpenApi.Models;
using SaveBite.Backend.Configurations;

namespace SaveBite.Backend.Extensions;

public static class ApiExtensions
{
    public static IServiceCollection AddApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddControllers()
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.DefaultIgnoreCondition =
                    JsonIgnoreCondition.WhenWritingNull;

                o.JsonSerializerOptions.PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase;
            });

        services.Configure<JsonOptions>(o =>
        {
            o.SerializerOptions.DefaultIgnoreCondition =
                JsonIgnoreCondition.WhenWritingNull;

            o.SerializerOptions.PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase;
        });

        services.AddEndpointsApiExplorer();

        services.Configure<SwaggerOptions>(
            configuration.GetSection("Swagger"));

        services.AddSwaggerGen(options =>
        {
            var swaggerOptions = configuration
                .GetSection("Swagger")
                .Get<SwaggerOptions>() ?? new SwaggerOptions();

            options.SwaggerDoc(swaggerOptions.Version, new()
            {
                Title = swaggerOptions.Title,
                Version = swaggerOptions.Version
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter a valid JWT access token."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                }] = Array.Empty<string>()
            });
        });

        return services;
    }
}
