using SaveBite.Backend.Configurations;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Extensions;

public static class CloudinaryExtensions
{
    public static IServiceCollection AddCloudinary(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<CloudinarySettings>()
            .Bind(configuration.GetSection(
                CloudinarySettings.SectionName))
            .Validate(
                settings => IsConfiguredValue(settings.CloudName),
                "Cloudinary CloudName is missing or is still a placeholder.")
            .Validate(
                settings => IsConfiguredValue(settings.ApiKey),
                "Cloudinary ApiKey is missing or is still a placeholder.")
            .Validate(
                settings => IsConfiguredValue(settings.ApiSecret),
                "Cloudinary ApiSecret is missing or is still a placeholder.")
            .Validate(
                settings => !string.IsNullOrWhiteSpace(
                    settings.UploadFolder),
                "Cloudinary UploadFolder is required.")
            .Validate(
                settings => settings.MaxFileSizeBytes > 0,
                "Cloudinary MaxFileSizeBytes must be greater than zero.")
            .Validate(
                settings => settings.AllowedContentTypes is { Length: > 0 } &&
                            settings.AllowedContentTypes.All(
                                contentType => contentType.StartsWith(
                                    "image/",
                                    StringComparison.OrdinalIgnoreCase)),
                "Cloudinary AllowedContentTypes must contain image MIME types.")
            .ValidateOnStart();

        services.AddSingleton<ICloudinaryService, CloudinaryService>();

        return services;
    }

    private static bool IsConfiguredValue(string value)
        => !string.IsNullOrWhiteSpace(value) &&
           !value.StartsWith(
               "your_",
               StringComparison.OrdinalIgnoreCase);
}
