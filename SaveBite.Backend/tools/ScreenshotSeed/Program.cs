using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Data.Seed;
using SaveBite.Backend.Models.Enums;

// Run from SaveBite.Api so the existing design-time factory loads local settings.
await using var db = new AppDbContextFactory().CreateDbContext(args);
if(args.Contains("--capture-refinements")){
 var connection=new Npgsql.NpgsqlConnectionStringBuilder(db.Database.GetConnectionString()){Database="savebite_uc050"};db.Database.SetConnectionString(connection.ConnectionString);await RefinedCaptureData.ApplyAsync(db);return;
}
if(args.Contains("--extended-usecases")){
 var connection=new Npgsql.NpgsqlConnectionStringBuilder(db.Database.GetConnectionString()){Database="savebite_uc050"};
 db.Database.SetConnectionString(connection.ConnectionString);
 await ExtendedUseCaseData.ApplyAsync(db);return;
}
await db.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS unaccent;");
await db.Database.MigrateAsync();
// The upstream seed assumes that the base dataset is either complete or absent.
if (await db.Users.AnyAsync(x => x.Email == "admin@gmail.com")
    && !await db.Users.AnyAsync(x => x.Email == "owner@gmail.com"))
    throw new InvalidOperationException("Partial existing dataset: use an empty local database for screenshot seeding.");
if (!args.Contains("--english-only") && !args.Contains("--customer-usecases")) await DbSeeder.SeedAsync(db);
// The upstream Staff example is suspended as a customer. Enable its demo access.
var staff = await db.Users.SingleAsync(x => x.Email == "customer.locked@gmail.com");
staff.CustomerStatus = CustomerStatus.Active;
staff.UpdatedAt = DateTime.UtcNow;
await db.SaveChangesAsync();
if (args.Contains("--english-only")) await EnglishScreenshotData.ApplyAsync(db);
if (args.Contains("--customer-usecases")) await CustomerUseCaseData.ApplyAsync(db);
Console.WriteLine($"Screenshot seed ready: {await db.Users.CountAsync()} users, {await db.Shops.CountAsync()} shops.");
