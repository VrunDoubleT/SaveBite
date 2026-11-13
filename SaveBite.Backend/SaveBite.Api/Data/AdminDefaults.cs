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
        CancellationToken ct = default)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(728194603)", ct);

            if (!await db.Users.AnyAsync(x => x.Role == UserRole.Admin, ct))
            {
                if (await db.Users.AnyAsync(x => x.Email.ToLower() == DefaultEmail, ct))
                {
                    throw new InvalidOperationException(
                        "Cannot initialize the default admin: admin@gmail.com belongs to a non-admin account.");
                }

                var now = clock.GetUtcNow().UtcDateTime;
                db.Users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Email = DefaultEmail,
                    PasswordHash = passwordHasher.HashPassword(DefaultPassword),
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
