using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace SaveBite.Backend.Extensions;

public static class LoggingExtensions
{
    public static IHostBuilder AddSerilogLogging(
        this IHostBuilder hostBuilder)
    {
        hostBuilder.UseSerilog((ctx, services, cfg) =>
        {
            cfg
                .ReadFrom.Configuration(ctx.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    theme: AnsiConsoleTheme.Literate,
                    applyThemeToRedirectedOutput: true);
        });

        return hostBuilder;
    }
}