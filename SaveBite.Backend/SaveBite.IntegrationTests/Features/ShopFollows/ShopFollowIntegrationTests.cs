using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.IntegrationTests.Infrastructure;

namespace SaveBite.IntegrationTests.Features.ShopFollows;

public class ShopFollowIntegrationTests : BaseIntegrationTest
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ShopFollowIntegrationTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task FollowAndUnfollowShop_FullLifecycle_PersistsIsIdempotentAndEnforcesRules()
    {
        Client.DefaultRequestHeaders.Authorization = null;
        (await Client.PutAsync($"/api/shops/{Guid.NewGuid()}/follow", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var owner = await AuthenticateAsStoreOwnerAsync();
        var shop = await DbContext.Shops.AsNoTracking().SingleAsync(s => s.OwnerUserId == owner.Id);

        // Store owner cannot follow own shop
        (await Client.PutAsync($"/api/shops/{shop.Id}/follow", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var customer = await AuthenticateAsCustomerAsync();

        // Missing shop -> 404
        (await Client.PutAsync($"/api/shops/{Guid.NewGuid()}/follow", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Follow active shop -> 200 Enabled
        var followResponse = await Client.PutAsync($"/api/shops/{shop.Id}/follow", null);
        followResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var followPayload = await followResponse.Content.ReadFromJsonAsync<ApiResponse<ShopFollowResponse>>(JsonOptions);
        followPayload!.Success.Should().BeTrue();
        followPayload.Data!.ShopId.Should().Be(shop.Id);
        followPayload.Data.ShopName.Should().Be(shop.Name);
        followPayload.Data.NotificationStatus.Should().Be(ShopNotificationStatus.Enabled);

        // Disable notifications, then re-follow -> preserves Disabled preference (idempotent)
        var disableResponse = await Client.PutAsJsonAsync(
            $"/api/shops/{shop.Id}/follow/notifications",
            new ShopNotificationSettingsRequest { NotificationStatus = ShopNotificationStatus.Disabled });
        disableResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refollowResponse = await Client.PutAsync($"/api/shops/{shop.Id}/follow", null);
        refollowResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refollowPayload = await refollowResponse.Content.ReadFromJsonAsync<ApiResponse<ShopFollowResponse>>(JsonOptions);
        refollowPayload!.Data!.NotificationStatus.Should().Be(ShopNotificationStatus.Disabled);

        // Unfollow -> 200 and removed from DB; repeated unfollow remains 200
        var unfollowResponse = await Client.DeleteAsync($"/api/shops/{shop.Id}/follow");
        unfollowResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await DbContext.Set<ShopFollow>().AsNoTracking()
            .AnyAsync(f => f.CustomerId == customer.Id && f.ShopId == shop.Id)).Should().BeFalse();

        (await Client.DeleteAsync($"/api/shops/{shop.Id}/follow")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FollowShop_WhenShopPaused_ReturnsConflict_409()
    {
        var owner = await AuthenticateAsStoreOwnerAsync();
        var shop = await DbContext.Shops.SingleAsync(s => s.OwnerUserId == owner.Id);
        shop.Status = ShopStatus.Paused;
        await DbContext.SaveChangesAsync();

        await AuthenticateAsCustomerAsync();
        var response = await Client.PutAsync($"/api/shops/{shop.Id}/follow", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ManageNotificationSettings_WhenFollowedOrNotFollowedOrSuspended_BehavesAsExpected()
    {
        var owner = await AuthenticateAsStoreOwnerAsync();
        var shop = await DbContext.Shops.AsNoTracking().SingleAsync(s => s.OwnerUserId == owner.Id);
        var customer = await AuthenticateAsCustomerAsync();

        // Not following yet -> 404
        var notFollowing = await Client.PutAsJsonAsync(
            $"/api/shops/{shop.Id}/follow/notifications",
            new ShopNotificationSettingsRequest { NotificationStatus = ShopNotificationStatus.Disabled });
        notFollowing.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Follow then disable -> 200
        (await Client.PutAsync($"/api/shops/{shop.Id}/follow", null)).EnsureSuccessStatusCode();
        var disableResponse = await Client.PutAsJsonAsync(
            $"/api/shops/{shop.Id}/follow/notifications",
            new ShopNotificationSettingsRequest { NotificationStatus = ShopNotificationStatus.Disabled });
        disableResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var disabledPayload = await disableResponse.Content.ReadFromJsonAsync<ApiResponse<ShopFollowResponse>>(JsonOptions);
        disabledPayload!.Data!.NotificationStatus.Should().Be(ShopNotificationStatus.Disabled);

        var dbFollow = await DbContext.Set<ShopFollow>().AsNoTracking()
            .SingleAsync(f => f.CustomerId == customer.Id && f.ShopId == shop.Id);
        dbFollow.NotificationStatus.Should().Be(ShopNotificationStatus.Disabled);

        // Suspended customer cannot modify notifications -> 403
        var trackedCustomer = await DbContext.Users.SingleAsync(u => u.Id == customer.Id);
        trackedCustomer.CustomerStatus = CustomerStatus.Suspended;
        await DbContext.SaveChangesAsync();

        var suspendedUpdate = await Client.PutAsJsonAsync(
            $"/api/shops/{shop.Id}/follow/notifications",
            new ShopNotificationSettingsRequest { NotificationStatus = ShopNotificationStatus.Enabled });
        suspendedUpdate.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ViewFollowedShops_WhenActiveOrSuspendedCustomer_ReturnsPagedListOrderedByFollowedAtDesc()
    {
        var owner1 = await AuthenticateAsStoreOwnerAsync();
        var shop1 = await DbContext.Shops.AsNoTracking().SingleAsync(s => s.OwnerUserId == owner1.Id);

        var owner2 = await AuthenticateAsStoreOwnerAsync();
        var shop2 = await DbContext.Shops.AsNoTracking().SingleAsync(s => s.OwnerUserId == owner2.Id);

        var customer = await AuthenticateAsCustomerAsync();

        var t1 = DateTime.UtcNow.AddHours(-2);
        var t2 = DateTime.UtcNow.AddHours(-1);
        DbContext.AddRange(
            new ShopFollow
            {
                CustomerId = customer.Id,
                ShopId = shop1.Id,
                NotificationStatus = ShopNotificationStatus.Enabled,
                FollowedAt = t1,
                UpdatedAt = t1
            },
            new ShopFollow
            {
                CustomerId = customer.Id,
                ShopId = shop2.Id,
                NotificationStatus = ShopNotificationStatus.Disabled,
                FollowedAt = t2,
                UpdatedAt = t2
            });
        await DbContext.SaveChangesAsync();

        var badPaging = await Client.GetAsync("/api/me/followed-shops?page=0&pageSize=20");
        badPaging.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var page1Response = await Client.GetAsync("/api/me/followed-shops?page=1&pageSize=1");
        page1Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page1Payload = await page1Response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ShopFollowResponse>>>(JsonOptions);
        page1Payload!.Data!.TotalItems.Should().Be(2);
        page1Payload.Data.Items.Should().ContainSingle(x => x.ShopId == shop2.Id && x.NotificationStatus == ShopNotificationStatus.Disabled);

        // Suspended customer still permitted to view followed shops
        var trackedCustomer = await DbContext.Users.SingleAsync(u => u.Id == customer.Id);
        trackedCustomer.CustomerStatus = CustomerStatus.Suspended;
        await DbContext.SaveChangesAsync();

        var suspendedViewResponse = await Client.GetAsync("/api/me/followed-shops?page=1&pageSize=20");
        suspendedViewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var suspendedPayload = await suspendedViewResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ShopFollowResponse>>>(JsonOptions);
        suspendedPayload!.Data!.Items.Select(x => x.ShopId).Should().ContainInOrder(shop2.Id, shop1.Id);
    }
}
