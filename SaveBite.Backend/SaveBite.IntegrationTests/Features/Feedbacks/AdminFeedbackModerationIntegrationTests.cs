using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.IntegrationTests.Infrastructure;
using Xunit;

namespace SaveBite.IntegrationTests.Features.Feedbacks;

/// <summary>
/// Integration Tests for Admin Feedback Moderation:
/// - View Customer Feedback (GET /api/feedback)
/// - Remove Violating Feedback (DELETE /api/feedback/{feedbackId})
/// </summary>
public class AdminFeedbackModerationIntegrationTests : BaseIntegrationTest
{
    public AdminFeedbackModerationIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<(User customer, Shop shop, Product product, ProductFeedback feedback)> SetupFeedbackScenarioAsync()
    {
        var owner = await AuthenticateAsStoreOwnerAsync(
            email: $"owner_{Guid.NewGuid():N}@savebite.com",
            fullName: "Shop Owner");

        var shop = await DbContext.Shops.FirstAsync(s => s.OwnerUserId == owner.Id);

        var customer = await AuthenticateAsCustomerAsync(
            email: $"cust_{Guid.NewGuid():N}@savebite.com",
            fullName: "Reviewer Customer");

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = $"Category_{Guid.NewGuid():N}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Categories.Add(category);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            CategoryId = category.Id,
            Name = "Bánh Mì Thịt Xíu",
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Products.Add(product);

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Sku = "BM-Xiu",
            OriginalPrice = 35000,
            DealPrice = 18000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.ProductVariants.Add(variant);

        var deal = new FlashDeal
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            ProductId = product.Id,
            SaleStartTime = DateTime.UtcNow.AddHours(-2),
            OrderEndTime = DateTime.UtcNow.AddHours(2),
            ShopClosingTime = DateTime.UtcNow.AddHours(3),
            Status = FlashDealStatus.OnSale,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.FlashDeals.Add(deal);

        var dealVariant = new FlashDealVariant
        {
            Id = Guid.NewGuid(),
            FlashDealId = deal.Id,
            VariantId = variant.Id,
            OriginalPrice = 35000,
            DealPrice = 18000,
            DiscountPercent = 50,
            TotalQuantity = 10,
            ReservedQuantity = 1,
            SoldQuantity = 1,
            Status = FlashDealVariantStatus.Active
        };
        DbContext.FlashDealVariants.Add(dealVariant);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderCode = $"ORD{Random.Shared.Next(100000, 999999)}",
            CustomerId = customer.Id,
            ShopId = shop.Id,
            PaymentMethod = PaymentMethod.OnlinePayment,
            Status = OrderStatus.Completed,
            TotalAmount = 18000,
            DepositAmount = 0,
            RemainingAmount = 0,
            DistanceKm = 1.0m,
            EtaMinutes = 10,
            SoftDeadlineAt = DateTime.UtcNow.AddHours(1),
            HardDeadlineAt = DateTime.UtcNow.AddHours(2),
            CreatedAt = DateTime.UtcNow.AddMinutes(-40),
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Orders.Add(order);

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            FlashDealVariantId = dealVariant.Id,
            ProductNameSnapshot = product.Name,
            VariantNameSnapshot = variant.Sku,
            OriginalPriceSnapshot = variant.OriginalPrice,
            DealPriceSnapshot = variant.DealPrice,
            UnitPrice = variant.DealPrice,
            Quantity = 1,
            Subtotal = 18000
        };
        DbContext.OrderItems.Add(orderItem);

        var feedback = new ProductFeedback
        {
            Id = Guid.NewGuid(),
            OrderItemId = orderItem.Id,
            ProductId = product.Id,
            UserId = customer.Id,
            Rating = 1,
            Comment = "Violation test comment containing bad words",
            Status = FeedbackStatus.Visible,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.ProductFeedbacks.Add(feedback);
        await DbContext.SaveChangesAsync();

        return (customer, shop, product, feedback);
    }

    // =========================================================================
    // 1. View Customer Feedback (GET /api/feedback)
    // =========================================================================

    [Fact]
    public async Task ViewFeedback_WhenAdmin_Returns200_WithPagedList()
    {
        // Arrange
        var (_, _, _, feedback) = await SetupFeedbackScenarioAsync();
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/feedback?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<FeedbackResponse>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Items.Should().Contain(f => f.Id == feedback.Id);
    }

    [Fact]
    public async Task ViewFeedback_WhenCustomer_Returns403Forbidden()
    {
        // Arrange
        await AuthenticateAsCustomerAsync();

        // Act
        var response = await Client.GetAsync("/api/feedback");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // =========================================================================
    // 2. Remove Violating Feedback (DELETE /api/feedback/{feedbackId})
    // =========================================================================

    [Fact]
    public async Task RemoveViolatingFeedback_WhenAdmin_SetsHiddenByAdminAndReturns200()
    {
        // Arrange
        var (_, _, _, feedback) = await SetupFeedbackScenarioAsync();
        await AuthenticateAsAdminAsync();

        var request = new HideViolatingFeedbackRequest
        {
            Reason = "Violates community harassment policy"
        };

        // Act
        var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/feedback/{feedback.Id}")
        {
            Content = JsonContent.Create(request)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ModerationActionResponse>>();
        body.Should().NotBeNull();
        body!.Data!.Action.Should().Be("FEEDBACK_REMOVED_VIOLATION");

        // Verify Database
        DbContext.ChangeTracker.Clear();
        var feedbackInDb = await DbContext.ProductFeedbacks.FindAsync(feedback.Id);
        feedbackInDb.Should().NotBeNull();
        feedbackInDb!.Status.Should().Be(FeedbackStatus.HiddenByAdmin);
        feedbackInDb.HiddenAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RemoveViolatingFeedback_WhenCustomer_Returns403Forbidden()
    {
        // Arrange
        var (_, _, _, feedback) = await SetupFeedbackScenarioAsync();
        await AuthenticateAsCustomerAsync();

        var request = new HideViolatingFeedbackRequest
        {
            Reason = "Customer attempt"
        };

        // Act
        var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/feedback/{feedback.Id}")
        {
            Content = JsonContent.Create(request)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
