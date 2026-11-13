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

namespace SaveBite.IntegrationTests.Features.Feedbacks;

public class ShopFeedbackReplyIntegrationTests : BaseIntegrationTest
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ShopFeedbackReplyIntegrationTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    private async Task<ProductFeedback> SeedFeedbackForShopAsync(
        Guid shopId,
        int rating = 5,
        string comment = "Fresh pastries",
        DateTime? createdAt = null)
    {
        var now = createdAt ?? DateTime.UtcNow;

        var customer = new User
        {
            Id = Guid.NewGuid(),
            Email = $"cust-{Guid.NewGuid():N}@savebite.test",
            PasswordHash = "hashed",
            FullName = "Reviewer Customer",
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            EmailVerifiedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = $"Cat-{Guid.NewGuid():N}"[..20],
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            CategoryId = category.Id,
            Name = "Artisan Croissant",
            Status = ProductStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Sku = $"SKU-{Guid.NewGuid():N}"[..16],
            OriginalPrice = 60000m,
            DealPrice = 30000m,
            IsActive = true,
            CreatedAt = now
        };

        var flashDeal = new FlashDeal
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            ProductId = product.Id,
            SaleStartTime = now.AddHours(-2),
            OrderEndTime = now.AddHours(2),
            ShopClosingTime = now.AddHours(4),
            Status = FlashDealStatus.OnSale,
            FeedPublishedAt = now.AddHours(-2),
            CreatedAt = now,
            UpdatedAt = now
        };

        var flashDealVariant = new FlashDealVariant
        {
            Id = Guid.NewGuid(),
            FlashDealId = flashDeal.Id,
            VariantId = variant.Id,
            OriginalPrice = 60000m,
            DealPrice = 30000m,
            DiscountPercent = 50m,
            TotalQuantity = 10,
            ReservedQuantity = 0,
            SoldQuantity = 1,
            Status = FlashDealVariantStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderCode = $"ORD-{Guid.NewGuid():N}"[..12],
            CustomerId = customer.Id,
            ShopId = shopId,
            PaymentMethod = PaymentMethod.OnlinePayment,
            Status = OrderStatus.Completed,
            TotalAmount = 30000m,
            DepositAmount = 0m,
            RemainingAmount = 0m,
            CompletedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            FlashDealVariantId = flashDealVariant.Id,
            ProductNameSnapshot = product.Name,
            VariantNameSnapshot = "Box of 2",
            OriginalPriceSnapshot = 60000m,
            DealPriceSnapshot = 30000m,
            UnitPrice = 30000m,
            Quantity = 1,
            Subtotal = 30000m,
            ResaleDecisionStatus = ResaleDecisionStatus.NotRequired
        };

        var feedback = new ProductFeedback
        {
            Id = Guid.NewGuid(),
            OrderItemId = orderItem.Id,
            ProductId = product.Id,
            UserId = customer.Id,
            Rating = rating,
            Comment = comment,
            Status = FeedbackStatus.Visible,
            CreatedAt = now,
            UpdatedAt = now
        };

        DbContext.Users.Add(customer);
        DbContext.Categories.Add(category);
        DbContext.Products.Add(product);
        DbContext.ProductVariants.Add(variant);
        DbContext.FlashDeals.Add(flashDeal);
        DbContext.FlashDealVariants.Add(flashDealVariant);
        DbContext.Orders.Add(order);
        DbContext.OrderItems.Add(orderItem);
        DbContext.ProductFeedbacks.Add(feedback);
        await DbContext.SaveChangesAsync();

        return feedback;
    }

    [Fact]
    public async Task GetShopFeedbacks_WhenUnauthenticatedOrCustomer_Returns401And403()
    {
        Client.DefaultRequestHeaders.Authorization = null;
        var unauthResponse = await Client.GetAsync("/api/feedbacks/shop");
        unauthResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        await AuthenticateAsCustomerAsync();
        var forbiddenResponse = await Client.GetAsync("/api/feedbacks/shop");
        forbiddenResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetShopFeedbacks_WhenStoreOwnerHasNoShop_ReturnsForbidden_403()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"noshop-{Guid.NewGuid():N}@savebite.test",
            PasswordHash = "hashed",
            FullName = "Owner Without Shop",
            Role = UserRole.StoreOwner,
            Status = UserStatus.Active,
            ShopStatus = ShopAccessStatus.Active,
            EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();
        SetBearerToken(user.Id, user.Email, UserRole.StoreOwner);

        var response = await Client.GetAsync("/api/feedbacks/shop");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetShopFeedbacks_WhenStoreOwnerAuthenticated_ReturnsShopFeedbacksWithFilters()
    {
        var owner = await AuthenticateAsStoreOwnerAsync();
        var shop = await DbContext.Shops.AsNoTracking().SingleAsync(s => s.OwnerUserId == owner.Id);

        var otherOwner = await AuthenticateAsStoreOwnerAsync();
        var otherShop = await DbContext.Shops.AsNoTracking().SingleAsync(s => s.OwnerUserId == otherOwner.Id);

        var f5 = await SeedFeedbackForShopAsync(shop.Id, rating: 5, comment: "Amazing", createdAt: DateTime.UtcNow.AddMinutes(-2));
        var f3 = await SeedFeedbackForShopAsync(shop.Id, rating: 3, comment: "Okay", createdAt: DateTime.UtcNow.AddMinutes(-1));
        await SeedFeedbackForShopAsync(otherShop.Id, rating: 5, comment: "Other shop feedback");

        SetBearerToken(owner.Id, owner.Email, owner.Role);

        var allResponse = await Client.GetAsync("/api/feedbacks/shop?page=1&pageSize=10");
        allResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var allPayload = await allResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResult<FeedbackResponses.FeedbackItemResponse>>>(JsonOptions);
        allPayload!.Success.Should().BeTrue();
        allPayload.Data!.Items.Select(i => i.Id).Should().BeEquivalentTo(new[] { f5.Id, f3.Id });

        var filteredResponse = await Client.GetAsync("/api/feedbacks/shop?rating=5&hasReply=false");
        filteredResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var filteredPayload = await filteredResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResult<FeedbackResponses.FeedbackItemResponse>>>(JsonOptions);
        filteredPayload!.Data!.Items.Should().ContainSingle(i => i.Id == f5.Id);
    }

    [Fact]
    public async Task ReplyFeedback_WhenValid_CreatesReplyActivityLogAndRejectsDuplicate_409()
    {
        var owner = await AuthenticateAsStoreOwnerAsync();
        var shop = await DbContext.Shops.AsNoTracking().SingleAsync(s => s.OwnerUserId == owner.Id);
        var feedback = await SeedFeedbackForShopAsync(shop.Id);

        var badReply = await Client.PostAsJsonAsync(
            $"/api/feedbacks/{feedback.Id}/reply",
            new FeedbackRequests.CreateFeedbackReplyRequest { Content = "   " });
        badReply.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var createResponse = await Client.PostAsJsonAsync(
            $"/api/feedbacks/{feedback.Id}/reply",
            new FeedbackRequests.CreateFeedbackReplyRequest { Content = "  Thank you very much!  " });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await createResponse.Content.ReadFromJsonAsync<ApiResponse<FeedbackResponses.FeedbackReplyResponse>>(JsonOptions);
        payload!.Success.Should().BeTrue();
        payload.Data!.Content.Should().Be("Thank you very much!");
        payload.Data.RepliedByUserId.Should().Be(owner.Id);

        var persistedReply = await DbContext.ProductFeedbackReplies.AsNoTracking()
            .SingleOrDefaultAsync(r => r.ProductFeedbackId == feedback.Id);
        persistedReply.Should().NotBeNull();
        persistedReply!.Content.Should().Be("Thank you very much!");

        var activityLog = await DbContext.StaffActivityLogs.AsNoTracking()
            .SingleOrDefaultAsync(a => a.ShopId == shop.Id && a.Action == StaffActivityAction.FeedbackReplied);
        activityLog.Should().NotBeNull();
        activityLog!.ActorRoleSnapshot.Should().Be("StoreOwner");

        var duplicateResponse = await Client.PostAsJsonAsync(
            $"/api/feedbacks/{feedback.Id}/reply",
            new FeedbackRequests.CreateFeedbackReplyRequest { Content = "Duplicate reply" });
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ReplyFeedback_WhenOtherShopOwner_Returns403()
    {
        var owner = await AuthenticateAsStoreOwnerAsync();
        var shop = await DbContext.Shops.AsNoTracking().SingleAsync(s => s.OwnerUserId == owner.Id);
        var feedback = await SeedFeedbackForShopAsync(shop.Id);

        await AuthenticateAsStoreOwnerAsync();

        var response = await Client.PostAsJsonAsync(
            $"/api/feedbacks/{feedback.Id}/reply",
            new FeedbackRequests.CreateFeedbackReplyRequest { Content = "Not my shop" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EditReply_WhenAuthor_UpdatesReplyAndRejectsOtherUser_403_OrMissing_404()
    {
        var owner = await AuthenticateAsStoreOwnerAsync();
        var shop = await DbContext.Shops.AsNoTracking().SingleAsync(s => s.OwnerUserId == owner.Id);
        var feedback = await SeedFeedbackForShopAsync(shop.Id);

        var notFoundResponse = await Client.PutAsJsonAsync(
            $"/api/feedbacks/{feedback.Id}/reply",
            new FeedbackRequests.UpdateFeedbackReplyRequest { Content = "Updated reply" });
        notFoundResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var postResponse = await Client.PostAsJsonAsync(
            $"/api/feedbacks/{feedback.Id}/reply",
            new FeedbackRequests.CreateFeedbackReplyRequest { Content = "Original reply" });
        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var editResponse = await Client.PutAsJsonAsync(
            $"/api/feedbacks/{feedback.Id}/reply",
            new FeedbackRequests.UpdateFeedbackReplyRequest { Content = "  Edited reply  " });
        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await editResponse.Content.ReadFromJsonAsync<ApiResponse<FeedbackResponses.FeedbackReplyResponse>>(JsonOptions);
        payload!.Data!.Content.Should().Be("Edited reply");

        var persisted = await DbContext.ProductFeedbackReplies.AsNoTracking()
            .SingleAsync(r => r.ProductFeedbackId == feedback.Id);
        persisted.Content.Should().Be("Edited reply");

        var staffUser = new User
        {
            Id = Guid.NewGuid(),
            Email = $"staff-{Guid.NewGuid():N}@savebite.test",
            PasswordHash = "hashed",
            FullName = "Shop Staff",
            Role = UserRole.User,
            Status = UserStatus.Active,
            EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Users.Add(staffUser);
        DbContext.ShopStaffMembers.Add(new ShopStaff
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            UserId = staffUser.Id,
            DisplayName = "Shop Staff",
            Status = ShopStaffStatus.Active,
            JoinedAt = DateTime.UtcNow
        });
        await DbContext.SaveChangesAsync();

        SetBearerToken(staffUser.Id, staffUser.Email, staffUser.Role);
        var staffEditResponse = await Client.PutAsJsonAsync(
            $"/api/feedbacks/{feedback.Id}/reply",
            new FeedbackRequests.UpdateFeedbackReplyRequest { Content = "Staff overwrite" });
        staffEditResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteReply_WhenAuthorOrOwnerRemovesStaffReply_DeletesFromDatabase()
    {
        var owner = await AuthenticateAsStoreOwnerAsync();
        var shop = await DbContext.Shops.AsNoTracking().SingleAsync(s => s.OwnerUserId == owner.Id);
        var feedback = await SeedFeedbackForShopAsync(shop.Id);

        var missingDelete = await Client.DeleteAsync($"/api/feedbacks/{feedback.Id}/reply");
        missingDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var staffUser = new User
        {
            Id = Guid.NewGuid(),
            Email = $"staff-{Guid.NewGuid():N}@savebite.test",
            PasswordHash = "hashed",
            FullName = "Cashier Lan",
            Role = UserRole.User,
            Status = UserStatus.Active,
            EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Users.Add(staffUser);
        DbContext.ShopStaffMembers.Add(new ShopStaff
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            UserId = staffUser.Id,
            DisplayName = "Cashier Lan",
            Status = ShopStaffStatus.Active,
            JoinedAt = DateTime.UtcNow
        });
        await DbContext.SaveChangesAsync();

        SetBearerToken(staffUser.Id, staffUser.Email, staffUser.Role);
        var staffReplyResponse = await Client.PostAsJsonAsync(
            $"/api/feedbacks/{feedback.Id}/reply",
            new FeedbackRequests.CreateFeedbackReplyRequest { Content = "Staff thank you!" });
        staffReplyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        SetBearerToken(owner.Id, owner.Email, owner.Role);
        var ownerDeleteResponse = await Client.DeleteAsync($"/api/feedbacks/{feedback.Id}/reply");
        ownerDeleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var remainingReply = await DbContext.ProductFeedbackReplies.AsNoTracking()
            .SingleOrDefaultAsync(r => r.ProductFeedbackId == feedback.Id);
        remainingReply.Should().BeNull();
    }
}
