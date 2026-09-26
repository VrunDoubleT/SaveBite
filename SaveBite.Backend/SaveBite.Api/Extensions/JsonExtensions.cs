using System.Text.Json.Serialization;
using System.Text.Json;

namespace SaveBite.Backend.Extensions;

public static class JsonExtensions
{
    public static IMvcBuilder AddSaveBiteJsonOptions(
        this IMvcBuilder builder)
    {
        builder.AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition =
                JsonIgnoreCondition.WhenWritingNull;

            options.JsonSerializerOptions.PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase;
        });

        builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
        {
            options.SerializerOptions.DefaultIgnoreCondition =
                JsonIgnoreCondition.WhenWritingNull;

            options.SerializerOptions.PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase;
        });

        return builder;
    }
}