using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Services.Implementations;

namespace SaveBite.UnitTests.ShopFollowManagement;

public class ManageShopNotificationSettingsTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly DateTimeOffset _fixedNow = new(2026, 9, 5, 14, 0, 0, TimeSpan.Zero);
    private readonly ShopFollowService _service;

    public ManageShopNotificationSettingsTests()
    {
        _db = TestDbContextFactory.Create();
        _service = new ShopFollowService(_db, new FixedTimeProvider(_fixedNow));
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task<(User Customer, Shop Shop)> SeedFollowAsync(
        ShopNotificationStatus initialStatus = ShopNotificationStatus.Enabled,
        CustomerStatus customerStatus = CustomerStatus.Active)
    {
        var created = _fixedNow.UtcDateTime.AddDays(-2);
        var customer = new User
        {
            Id = Guid.NewGuid(),
            Email = $"cust-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            FullName = "Customer",
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = customerStatus,
            CreatedAt = created,
            UpdatedAt = created
        };
        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = $"owner-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            FullName = "Owner",
            Role = UserRole.StoreOwner,
            Status = UserStatus.Active,
            CreatedAt = created,
            UpdatedAt = created
        };
        var shop = new Shop
        {
            Id = Guid.NewGuid(),
            OwnerUserId = owner.Id,
            Name = "Bistro Fresh",
            AddressLine = "48 Tran Hung Dao",
            Status = ShopStatus.Active,
            CreatedAt = created,
            UpdatedAt = created
        };

        _db.Users.AddRange(customer, owner);
        _db.Shops.Add(shop);
        _db.Add(new ShopFollow
        {
            CustomerId = customer.Id,
            ShopId = shop.Id,
            NotificationStatus = initialStatus,
            FollowedAt = created,
            UpdatedAt = created
        });
        await _db.SaveChangesAsync();
        return (customer, shop);
    }

    [Fact]
    public async Task UpdateSettingsAsync_WhenFollowed_TogglesNotificationStatusAndUpdatedAt()
    {
        var (customer, shop) = await SeedFollowAsync(ShopNotificationStatus.Enabled);

        var disabledResponse = await _service.UpdateSettingsAsync(customer.Id, shop.Id, ShopNotificationStatus.Disabled);
        disabledResponse.NotificationStatus.Should().Be(ShopNotificationStatus.Disabled);
        disabledResponse.ShopName.Should().Be("Bistro Fresh");

        var persisted = await _db.Set<ShopFollow>().SingleAsync(f => f.CustomerId == customer.Id && f.ShopId == shop.Id);
        persisted.NotificationStatus.Should().Be(ShopNotificationStatus.Disabled);
        persisted.UpdatedAt.Should().Be(_fixedNow.UtcDateTime);

        var enabledResponse = await _service.UpdateSettingsAsync(customer.Id, shop.Id, ShopNotificationStatus.Enabled);
        enabledResponse.NotificationStatus.Should().Be(ShopNotificationStatus.Enabled);
    }

    [Fact]
    public async Task UpdateSettingsAsync_WhenNotFollowingShop_ThrowsNotFound()
    {
        var now = _fixedNow.UtcDateTime;
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

        var act = () => _service.UpdateSettingsAsync(customer.Id, Guid.NewGuid(), ShopNotificationStatus.Disabled);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Follow this shop before changing notification settings.");
    }

    [Fact]
    public async Task UpdateSettingsAsync_WhenInvalidEnumStatus_ThrowsBadRequest()
    {
        var (customer, shop) = await SeedFollowAsync();

        var act = () => _service.UpdateSettingsAsync(customer.Id, shop.Id, (ShopNotificationStatus)999);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("NotificationStatus must be Enabled or Disabled.");
    }

    [Fact]
    public async Task UpdateSettingsAsync_WhenShopIdEmpty_ThrowsBadRequest()
    {
        var act = () => _service.UpdateSettingsAsync(Guid.NewGuid(), Guid.Empty, ShopNotificationStatus.Disabled);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("Shop ID is required.");
    }

    [Fact]
    public async Task UpdateSettingsAsync_WhenCustomerSuspended_ThrowsForbidden()
    {
        var (customer, shop) = await SeedFollowAsync(customerStatus: CustomerStatus.Suspended);

        var act = () => _service.UpdateSettingsAsync(customer.Id, shop.Id, ShopNotificationStatus.Disabled);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("An active customer account is required.");
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
