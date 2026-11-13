using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Services.Implementations;

namespace SaveBite.UnitTests.ShopFollowManagement;

public class FollowShopTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly DateTimeOffset _fixedNow = new(2026, 9, 1, 9, 30, 0, TimeSpan.Zero);
    private readonly ShopFollowService _service;

    public FollowShopTests()
    {
        _db = TestDbContextFactory.Create();
        _service = new ShopFollowService(_db, new FixedTimeProvider(_fixedNow));
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task<(User Customer, User Owner, Shop Shop)> SeedUsersAndShopAsync(
        ShopStatus shopStatus = ShopStatus.Active,
        UserRole customerRole = UserRole.User,
        UserStatus customerUserStatus = UserStatus.Active,
        CustomerStatus customerStatus = CustomerStatus.Active)
    {
        var now = _fixedNow.UtcDateTime;
        var customer = new User
        {
            Id = Guid.NewGuid(),
            Email = $"cust-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            FullName = "Customer A",
            Role = customerRole,
            Status = customerUserStatus,
            CustomerStatus = customerStatus,
            CreatedAt = now,
            UpdatedAt = now
        };
        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = $"owner-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            FullName = "Owner A",
            Role = UserRole.StoreOwner,
            Status = UserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
        var shop = new Shop
        {
            Id = Guid.NewGuid(),
            OwnerUserId = owner.Id,
            Name = "Eco Deli",
            LogoUrl = "https://cdn.example.com/eco.png",
            AddressLine = "100 Nguyen Hue",
            Status = shopStatus,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Users.AddRange(customer, owner);
        _db.Shops.Add(shop);
        await _db.SaveChangesAsync();
        return (customer, owner, shop);
    }

    [Fact]
    public async Task FollowAsync_WhenCustomerAndShopActive_CreatesFollowWithEnabledNotifications()
    {
        var (customer, _, shop) = await SeedUsersAndShopAsync();

        var response = await _service.FollowAsync(customer.Id, shop.Id);

        response.ShopId.Should().Be(shop.Id);
        response.ShopName.Should().Be("Eco Deli");
        response.LogoUrl.Should().Be("https://cdn.example.com/eco.png");
        response.AddressLine.Should().Be("100 Nguyen Hue");
        response.ShopStatus.Should().Be(ShopStatus.Active);
        response.NotificationStatus.Should().Be(ShopNotificationStatus.Enabled);
        response.FollowedAt.Should().Be(_fixedNow.UtcDateTime);

        var persisted = await _db.Set<ShopFollow>().SingleAsync(f => f.CustomerId == customer.Id && f.ShopId == shop.Id);
        persisted.NotificationStatus.Should().Be(ShopNotificationStatus.Enabled);
        persisted.FollowedAt.Should().Be(_fixedNow.UtcDateTime);
    }

    [Fact]
    public async Task FollowAsync_WhenAlreadyFollowed_PreservesNotificationPreferenceAndFollowedAt()
    {
        var (customer, _, shop) = await SeedUsersAndShopAsync();
        var originalTime = _fixedNow.UtcDateTime.AddDays(-7);

        _db.Add(new ShopFollow
        {
            CustomerId = customer.Id,
            ShopId = shop.Id,
            NotificationStatus = ShopNotificationStatus.Disabled,
            FollowedAt = originalTime,
            UpdatedAt = originalTime
        });
        await _db.SaveChangesAsync();

        var response = await _service.FollowAsync(customer.Id, shop.Id);

        response.NotificationStatus.Should().Be(ShopNotificationStatus.Disabled);
        response.FollowedAt.Should().Be(originalTime);
        (await _db.Set<ShopFollow>().CountAsync(f => f.CustomerId == customer.Id && f.ShopId == shop.Id)).Should().Be(1);
    }

    [Fact]
    public async Task FollowAsync_WhenShopIdEmpty_ThrowsBadRequest()
    {
        var act = () => _service.FollowAsync(Guid.NewGuid(), Guid.Empty);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("Shop ID is required.");
    }

    [Fact]
    public async Task FollowAsync_WhenCustomerNotFound_ThrowsUnauthorized()
    {
        var (_, _, shop) = await SeedUsersAndShopAsync();

        var act = () => _service.FollowAsync(Guid.NewGuid(), shop.Id);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(UserRole.Admin, UserStatus.Active, CustomerStatus.Active)]
    [InlineData(UserRole.User, UserStatus.Suspended, CustomerStatus.Active)]
    [InlineData(UserRole.User, UserStatus.Active, CustomerStatus.Suspended)]
    public async Task FollowAsync_WhenAccountNotActiveCustomer_ThrowsForbidden(
        UserRole role, UserStatus userStatus, CustomerStatus customerStatus)
    {
        var (customer, _, shop) = await SeedUsersAndShopAsync(
            customerRole: role,
            customerUserStatus: userStatus,
            customerStatus: customerStatus);

        var act = () => _service.FollowAsync(customer.Id, shop.Id);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("An active customer account is required.");
    }

    [Fact]
    public async Task FollowAsync_WhenShopNotFound_ThrowsNotFound()
    {
        var (customer, _, _) = await SeedUsersAndShopAsync();

        var act = () => _service.FollowAsync(customer.Id, Guid.NewGuid());

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Shop not found.");
    }

    [Fact]
    public async Task FollowAsync_WhenFollowingOwnShop_ThrowsForbidden()
    {
        var (_, owner, shop) = await SeedUsersAndShopAsync();

        var act = () => _service.FollowAsync(owner.Id, shop.Id);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("You cannot follow your own shop.");
    }

    [Theory]
    [InlineData(ShopStatus.Paused)]
    [InlineData(ShopStatus.Suspended)]
    public async Task FollowAsync_WhenShopNotActiveAndNotYetFollowed_ThrowsConflict(ShopStatus status)
    {
        var (customer, _, shop) = await SeedUsersAndShopAsync(shopStatus: status);

        var act = () => _service.FollowAsync(customer.Id, shop.Id);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("Only an active shop can be followed.");
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
