using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Seed;

public static class CategoryDefaults
{
    public static async Task EnsureAsync(AppDbContext db, TimeProvider clock, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Serialize initialization with category writes, including concurrent application instances.
        await db.Database.ExecuteSqlRawAsync("LOCK TABLE categories IN SHARE ROW EXCLUSIVE MODE", ct);
        if (!await db.Categories.AnyAsync(category => category.Name.Trim().ToLower() == "others", ct))
        {
            var now = clock.GetUtcNow().UtcDateTime;
            db.Categories.Add(new Category
            {
                Id = Guid.NewGuid(),
                Name = "Others",
                Description = "Products that do not fit into another category.",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
            await db.SaveChangesAsync(ct);
        }
        await transaction.CommitAsync(ct);
    }
}
