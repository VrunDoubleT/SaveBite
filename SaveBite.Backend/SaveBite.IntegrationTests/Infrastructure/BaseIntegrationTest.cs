using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using Xunit;

namespace SaveBite.IntegrationTests.Infrastructure;

public abstract class BaseIntegrationTest : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;
    private IServiceScope _scope = null!;
    protected AppDbContext DbContext = null!;

    protected BaseIntegrationTest(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    public virtual async Task InitializeAsync()
    {
        _scope = Factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Ensure database exists and schema is migrated
        await DbContext.Database.MigrateAsync();
    }

    public virtual async Task DisposeAsync()
    {
        _scope.Dispose();
        await DatabaseFixture.ResetDatabaseAsync();
    }

    /// <summary>
    /// Attach a JWT Bearer token to client requests.
    /// </summary>
    protected void SetBearerToken(Guid userId, string email, UserRole role)
    {
        var token = AuthHelper.GenerateAccessToken(userId, email, role);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Create an active Admin user in the test database and attach its JWT token.
    /// </summary>
    protected async Task<User> AuthenticateAsAdminAsync(
        string email = "test.admin@savebite.com",
        string fullName = "Test Admin")
    {
        var targetEmail = email == "test.admin@savebite.com" ? $"test.admin.{Guid.NewGuid():N}@savebite.com" : email;
        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = targetEmail,
            Phone = $"09{Random.Shared.Next(10000000, 99999999)}",
            FullName = fullName,
            PasswordHash = "dummy-hash",
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            ShopStatus = ShopAccessStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Users.Add(admin);
        await DbContext.SaveChangesAsync();

        SetBearerToken(admin.Id, admin.Email, admin.Role);
        return admin;
    }

    /// <summary>
    /// Create an active Store Owner user in the test database and attach its JWT token.
    /// </summary>
    protected async Task<User> AuthenticateAsStoreOwnerAsync(
        string email = "test.owner@savebite.com",
        string fullName = "Test Owner")
    {
        var targetEmail = email == "test.owner@savebite.com" ? $"test.owner.{Guid.NewGuid():N}@savebite.com" : email;
        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = targetEmail,
            Phone = $"09{Random.Shared.Next(10000000, 99999999)}",
            FullName = fullName,
            PasswordHash = "dummy-hash",
            Role = UserRole.StoreOwner,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            ShopStatus = ShopAccessStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Users.Add(owner);
        
        var appId = Guid.NewGuid();
        var application = new ShopApplication
        {
            Id = appId,
            ApplicantUserId = owner.Id,
            Name = "Test Bakery Shop",
            AddressLine = "123 Test St",
            PayosClientId = "client-id",
            PayosApiKey = "api-key",
            PayosChecksumKey = "checksum-key",
            Status = ShopApplicationStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.ShopApplications.Add(application);

        var shop = new Shop
        {
            Id = Guid.NewGuid(),
            OwnerUserId = owner.Id,
            ApplicationId = appId,
            Name = "Test Bakery Shop",
            AddressLine = "123 Test St",
            Status = ShopStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Shops.Add(shop);

        await DbContext.SaveChangesAsync();

        SetBearerToken(owner.Id, owner.Email, owner.Role);
        return owner;
    }

    /// <summary>
    /// Create an active Customer user in the test database and attach its JWT token.
    /// </summary>
    protected async Task<User> AuthenticateAsCustomerAsync(
        string email = "test.customer@savebite.com",
        string fullName = "Test Customer")
    {
        var targetEmail = email == "test.customer@savebite.com" ? $"test.customer.{Guid.NewGuid():N}@savebite.com" : email;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = targetEmail,
            Phone = $"09{Random.Shared.Next(10000000, 99999999)}",
            FullName = fullName,
            PasswordHash = "dummy-hash",
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            ShopStatus = ShopAccessStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        SetBearerToken(user.Id, user.Email, user.Role);
        return user;
    }
}
