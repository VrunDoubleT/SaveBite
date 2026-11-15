using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Data;

public static class AdminDefaults
{
    public const string DefaultEmail = "admin@gmail.com";
    public const string DefaultPassword = "SaveBite@123";

    public static async Task EnsureAsync(
        AppDbContext db,
        TimeProvider clock,
        IPasswordHasherService passwordHasher,
        IConfiguration configuration,
        CancellationToken ct = default)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(728194603)", ct);

            if (!await db.Users.AnyAsync(x => x.Role == UserRole.Admin, ct))
            {
                var email = (configuration["BootstrapAdmin:Email"] ?? DefaultEmail).Trim().ToLowerInvariant();
                var password = configuration["BootstrapAdmin:Password"];
                if (configuration.GetValue<bool>("Deployment:Containerized") && string.IsNullOrWhiteSpace(password))
                    throw new InvalidOperationException("Configure BootstrapAdmin:Password before initializing a container database.");
                password ??= DefaultPassword;
                if (await db.Users.AnyAsync(x => x.Email.ToLower() == email, ct))
                {
                    throw new InvalidOperationException(
                        "Cannot initialize the bootstrap admin: its email belongs to a non-admin account.");
                }

                var now = clock.GetUtcNow().UtcDateTime;
                db.Users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    PasswordHash = passwordHasher.HashPassword(password),
                    FullName = "SaveBite Admin",
                    EmailVerifiedAt = now,
                    Status = UserStatus.Active,
                    CustomerStatus = CustomerStatus.Active,
                    ShopStatus = ShopAccessStatus.Active,
                    Role = UserRole.Admin,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                await db.SaveChangesAsync(ct);
            }

            await transaction.CommitAsync(ct);
        });
    }
}
