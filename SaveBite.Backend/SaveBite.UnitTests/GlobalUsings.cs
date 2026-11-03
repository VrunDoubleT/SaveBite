using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
namespace SaveBite.UnitTests;
public static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}