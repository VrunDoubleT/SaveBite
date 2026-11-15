using DotNetEnv;

namespace SaveBite.Backend.Extensions;

public static class EnvironmentExtensions
{
    public static WebApplicationBuilder AddEnvironmentConfiguration(
        this WebApplicationBuilder builder)
    {
        var envPath = Path.GetFullPath(
            Path.Combine(
                builder.Environment.ContentRootPath,
                "../.env"));

        if (File.Exists(envPath))
        {
            // Docker/host environment takes precedence over the local fallback file.
            Env.NoClobber().Load(envPath);
        }

        // Reload environment variables after DotNetEnv has populated them.
        // Nested configuration keys use double underscores, for example.
        // Cloudinary__CloudName.
        builder.Configuration.AddEnvironmentVariables();

        // Read application port.
        var port = builder.Configuration["API_PORT"];

        if (string.IsNullOrWhiteSpace(builder.Configuration["ASPNETCORE_URLS"])
            && string.IsNullOrWhiteSpace(builder.Configuration["DOTNET_URLS"])
            && string.IsNullOrWhiteSpace(builder.Configuration["urls"])
            && !string.IsNullOrWhiteSpace(port))
        {
            if (!int.TryParse(port, out var portNumber))
            {
                throw new InvalidOperationException(
                    $"Invalid PORT value: '{port}'.");
            }

            if (portNumber is < 1 or > 65535)
            {
                throw new InvalidOperationException(
                    $"PORT must be between 1 and 65535. Current value: {portNumber}.");
            }

            builder.WebHost.UseUrls(
                $"http://0.0.0.0:{portNumber}");
        }

        return builder;
    }
}
