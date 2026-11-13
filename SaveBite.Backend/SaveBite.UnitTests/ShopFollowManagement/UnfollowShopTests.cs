using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Services.Implementations;

namespace SaveBite.UnitTests.ShopFollowManagement;

public class UnfollowShopTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ShopFollowService _service;

    public UnfollowShopTests()
    {
        _db = TestDbContextFactory.Create();
        _service = new ShopFollowService(_db, TimeProvider.System);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task UnfollowAsync_WhenFollowExists_RemovesShopFollow()
    {
        var now = DateTime.UtcNow;
        var customer = new User
        {
            Id = Guid.NewGuid(),
            Email = $"cust-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            FullName = "Customer",
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = $"owner-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            FullName = "Owner",
            Role = UserRole.StoreOwner,
            Status = UserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
        var shop = new Shop
        {
            Id = Guid.NewGuid(),
            OwnerUserId = owner.Id,
            Name = "Green Corner",
            AddressLine = "25 Hai Ba Trung",
            Status = ShopStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Users.AddRange(customer, owner);
        _db.Shops.Add(shop);
        _db.Add(new ShopFollow
        {
            CustomerId = customer.Id,
            ShopId = shop.Id,
            NotificationStatus = ShopNotificationStatus.Enabled,
            FollowedAt = now,
            UpdatedAt = now
        });
        await _db.SaveChangesAsync();

        await _service.UnfollowAsync(customer.Id, shop.Id);

        (await _db.Set<ShopFollow>().AnyAsync(f => f.CustomerId == customer.Id && f.ShopId == shop.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task UnfollowAsync_WhenNotFollowingOrShopMissing_RemainsIdempotent()
    {
        var now = DateTime.UtcNow;
        var customer = new User
        {
            Id = Guid.NewGuid(),
            Email = $"cust-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            FullName = "Customer",
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Users.Add(customer);
        await _db.SaveChangesAsync();

        var act = () => _service.UnfollowAsync(customer.Id, Guid.NewGuid());

        await act.Should().NotThrowAsync();
        (await _db.Set<ShopFollow>().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task UnfollowAsync_WhenShopIdEmpty_ThrowsBadRequest()
    {
        var act = () => _service.UnfollowAsync(Guid.NewGuid(), Guid.Empty);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("Shop ID is required.");
    }

    [Fact]
    public async Task UnfollowAsync_WhenCustomerNotFound_ThrowsUnauthorized()
    {
        var act = () => _service.UnfollowAsync(Guid.NewGuid(), Guid.NewGuid());

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnfollowAsync_WhenCustomerSuspended_ThrowsForbidden()
    {
        var now = DateTime.UtcNow;
        var customer = new User
        {
            Id = Guid.NewGuid(),
            Email = $"cust-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            FullName = "Suspended Customer",
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Suspended,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Users.Add(customer);
        await _db.SaveChangesAsync();

        var act = () => _service.UnfollowAsync(customer.Id, Guid.NewGuid());

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("An active customer account is required.");
    }
}
