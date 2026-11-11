using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SaveBite.Backend.Data;

// Migration generation must not boot the API (which migrates and seeds on startup).
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var projectDirectory = FindProjectDirectory();
        var envPath = Path.GetFullPath(Path.Combine(projectDirectory, "../.env"));
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(projectDirectory)
            .AddJsonFile("appsettings.json", optional: false);

        if (!string.IsNullOrWhiteSpace(environment))
        {
            configurationBuilder.AddJsonFile(
                $"appsettings.{environment}.json",
                optional: true);
        }

        configurationBuilder.AddEnvironmentVariables();
        var configuration = configurationBuilder.Build();
        var connectionString = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Postgres connection string is not configured. Set ConnectionStrings:Postgres in appsettings.json or ConnectionStrings__Postgres in the environment.");
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }

    private static string FindProjectDirectory()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SaveBite.Api.csproj")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate SaveBite.Api.csproj from the current directory.");
    }
}
