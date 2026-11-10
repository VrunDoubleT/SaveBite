using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Audit;
using SaveBite.Backend.Data;
using SaveBite.Backend.Data.Seed;

namespace SaveBite.Backend.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Postgres connection string is not configured.");
        }

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        return services;
    }

    public static async Task InitializeDatabaseAsync(this WebApplication app, CancellationToken ct = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var auditContext = scope.ServiceProvider.GetRequiredService<IAuditContext>();
        auditContext.Enabled = false;

        try
        {
            await context.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS unaccent;", ct);
        }
        catch
        {
            // Ignore if already exists or permission denied
        }

        await context.Database.MigrateAsync(ct);
        await DbSeeder.SeedAsync(context);
    }
}
