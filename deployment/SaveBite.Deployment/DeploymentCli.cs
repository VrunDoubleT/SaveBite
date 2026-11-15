using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SaveBite.Deployment;

public static class DeploymentCli
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            if (args is ["--runtime-env"])
            {
                await RuntimeEnvironment.PrepareAsync();
                return 0;
            }

            var settings = TunnelSettings.FromEnvironment();
            if (args is ["--health"] or ["--status"])
            {
                var snapshot = await new EndpointStore(settings.StatePath, TimeProvider.System).ReadAsync();
                var healthy = snapshot?.IsHealthy(TimeProvider.System) == true;
                if (args[0] == "--status")
                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        snapshot?.Url, Ready = snapshot?.Ready ?? false, snapshot?.Session,
                        snapshot?.UpdatedAtUnix, Healthy = healthy
                    }, new JsonSerializerOptions { WriteIndented = true }));
                return healthy ? 0 : 1;
            }
            if (args.Length > 0 && args[0] == "--prepare")
            {
                string? root = null;
                var refresh = false;
                for (var i = 1; i < args.Length; i++)
                {
                    if (args[i] == "--refresh") refresh = true;
                    else if (args[i] == "--root" && i + 1 < args.Length) root = args[++i];
                    else throw new PreparationException("Usage: --prepare [--root DIRECTORY] [--refresh]");
                }
                root ??= DeploymentPreparation.FindRoot(Directory.GetCurrentDirectory());
                var changed = await DeploymentPreparation.PrepareAsync(root, refresh);
                Console.WriteLine(changed
                    ? "Prepared root .env from existing credentials; no secret values displayed."
                    : "Private deployment configuration already matches backend .env.");
                return 0;
            }
            if (args.Length > 0)
            {
                Console.WriteLine("Usage: SaveBite.Deployment [--health | --status | --prepare [--root DIRECTORY] [--refresh] | --runtime-env]");
                return args is ["--help"] ? 0 : 2;
            }

            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddSingleton(settings);
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddSingleton(new EndpointStore(settings.StatePath, TimeProvider.System));
            builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(20));
            builder.Services.AddHttpClient<CloudflareWatcher>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(5);
                client.MaxResponseContentBufferSize = 1024;
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
                .RemoveAllLoggers();
            builder.Services.AddHostedService(provider => provider.GetRequiredService<CloudflareWatcher>());
            using var host = builder.Build();
            await host.RunAsync();
            return 0;
        }
        catch (PreparationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            Console.Error.WriteLine($"Deployment command failed ({ex.GetType().Name}). Check configuration files and permissions.");
            return 1;
        }
    }
}
