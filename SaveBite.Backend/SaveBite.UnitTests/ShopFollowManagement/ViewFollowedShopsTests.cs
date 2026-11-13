using System.Net;
using FluentAssertions;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Services.Implementations;

namespace SaveBite.UnitTests.ShopFollowManagement;

public class ViewFollowedShopsTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ShopFollowService _service;

    public ViewFollowedShopsTests()
    {
        _db = TestDbContextFactory.Create();
        _service = new ShopFollowService(_db, TimeProvider.System);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task GetFollowedShopsAsync_WhenFollowedShopsExist_ReturnsPagedShopsOrderedByFollowedAtDescThenShopIdAsc()
    {
        var baseTime = new DateTime(2026, 9, 10, 8, 0, 0, DateTimeKind.Utc);
        var customer = new User
        {
            Id = Guid.NewGuid(),
            Email = $"cust-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            FullName = "Customer View",
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            CreatedAt = baseTime,
            UpdatedAt = baseTime
        };
        var owner1 = new User
        {
            Id = Guid.NewGuid(),
            Email = $"owner1-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            FullName = "Owner 1",
            Role = UserRole.StoreOwner,
            Status = UserStatus.Active,
            CreatedAt = baseTime,
            UpdatedAt = baseTime
        };
        var owner2 = new User
        {
            Id = Guid.NewGuid(),
            Email = $"owner2-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            FullName = "Owner 2",
            Role = UserRole.StoreOwner,
            Status = UserStatus.Active,
            CreatedAt = baseTime,
            UpdatedAt = baseTime
        };
        var owner3 = new User
        {
            Id = Guid.NewGuid(),
            Email = $"owner3-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            FullName = "Owner 3",
            Role = UserRole.StoreOwner,
            Status = UserStatus.Active,
            CreatedAt = baseTime,
            UpdatedAt = baseTime
        };

        var tieLowerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var tieHigherId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var oldestId = Guid.NewGuid();

        var shopTieLow = new Shop { Id = tieLowerId, ApplicationId = Guid.NewGuid(), OwnerUserId = owner1.Id, Name = "Shop Tie Low", AddressLine = "Addr 1", Status = ShopStatus.Active, CreatedAt = baseTime, UpdatedAt = baseTime };
        var shopTieHigh = new Shop { Id = tieHigherId, ApplicationId = Guid.NewGuid(), OwnerUserId = owner2.Id, Name = "Shop Tie High", AddressLine = "Addr 2", Status = ShopStatus.Paused, CreatedAt = baseTime, UpdatedAt = baseTime };
        var shopOldest = new Shop { Id = oldestId, ApplicationId = Guid.NewGuid(), OwnerUserId = owner3.Id, Name = "Shop Oldest", AddressLine = "Addr 3", Status = ShopStatus.Active, CreatedAt = baseTime, UpdatedAt = baseTime };

        _db.Users.AddRange(customer, owner1, owner2, owner3);
        _db.Shops.AddRange(shopTieLow, shopTieHigh, shopOldest);
        _db.AddRange(
            new ShopFollow { CustomerId = customer.Id, ShopId = oldestId, NotificationStatus = ShopNotificationStatus.Enabled, FollowedAt = baseTime.AddHours(-2), UpdatedAt = baseTime },
            new ShopFollow { CustomerId = customer.Id, ShopId = tieHigherId, NotificationStatus = ShopNotificationStatus.Disabled, FollowedAt = baseTime, UpdatedAt = baseTime },
            new ShopFollow { CustomerId = customer.Id, ShopId = tieLowerId, NotificationStatus = ShopNotificationStatus.Enabled, FollowedAt = baseTime, UpdatedAt = baseTime }
        );
        await _db.SaveChangesAsync();

        var page1 = await _service.GetFollowedShopsAsync(customer.Id, page: 1, pageSize: 2);
        page1.TotalItems.Should().Be(3);
        page1.Page.Should().Be(1);
        page1.PageSize.Should().Be(2);
        page1.Items.Should().HaveCount(2);
        page1.Items[0].ShopId.Should().Be(tieLowerId);
        page1.Items[1].ShopId.Should().Be(tieHigherId);
        page1.Items[1].NotificationStatus.Should().Be(ShopNotificationStatus.Disabled);

        var page2 = await _service.GetFollowedShopsAsync(customer.Id, page: 2, pageSize: 2);
        page2.TotalItems.Should().Be(3);
        page2.Items.Should().ContainSingle();
        page2.Items[0].ShopId.Should().Be(oldestId);
    }

    [Fact]
    public async Task GetFollowedShopsAsync_WhenCustomerSuspendedButAccountActive_StillReturnsFollowedShops()
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
            Name = "Organic Hub",
            AddressLine = "12 Le Loi",
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

        var result = await _service.GetFollowedShopsAsync(customer.Id, 1, 20);

        result.TotalItems.Should().Be(1);
        result.Items[0].ShopName.Should().Be("Organic Hub");
    }

    [Theory]
    [InlineData(UserRole.Admin, UserStatus.Active)]
    [InlineData(UserRole.User, UserStatus.Suspended)]
    public async Task GetFollowedShopsAsync_WhenAdminOrInactiveAccount_ThrowsForbidden(UserRole role, UserStatus status)
    {
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"u-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            FullName = "User",
            Role = role,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var act = () => _service.GetFollowedShopsAsync(user.Id, 1, 20);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("An active account is required to view followed shops.");
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(100001, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetFollowedShopsAsync_WhenInvalidPagination_ThrowsBadRequest(int page, int pageSize)
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
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Users.Add(customer);
        await _db.SaveChangesAsync();

        var act = () => _service.GetFollowedShopsAsync(customer.Id, page, pageSize);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("Page must be 1\u2013100000; pageSize must be 1\u2013100.");
    }
}
