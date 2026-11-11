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
/// Integration Tests for Product Feedback features:
/// - Add Feedback (POST /api/feedbacks)
/// - View Feedback History (GET /api/feedbacks/history)
/// - Edit Feedback (PUT /api/feedbacks/{feedbackId})
/// - Delete Feedback (DELETE /api/feedbacks/{feedbackId})
/// Rules: BR-032, BR-033, BR-034, BR-049
/// </summary>
public class FeedbackIntegrationTests : BaseIntegrationTest
{
    public FeedbackIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<(User customer, Shop shop, Order order, OrderItem orderItem, Product product)> SetupOrderScenarioAsync(
        OrderStatus orderStatus = OrderStatus.Completed,
        User? existingCustomer = null)
    {
        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = $"owner_{Guid.NewGuid():N}@test.com",
            Phone = $"09{Random.Shared.Next(10000000, 99999999)}",
            FullName = "Shop Owner",
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

        var customer = existingCustomer;
        if (customer == null)
        {
            customer = new User
            {
                Id = Guid.NewGuid(),
                Email = $"cust_{Guid.NewGuid():N}@test.com",
                Phone = $"09{Random.Shared.Next(10000000, 99999999)}",
                FullName = "Customer Test",
                PasswordHash = "dummyhash",
                Role = UserRole.User,
                Status = UserStatus.Active,
                CustomerStatus = CustomerStatus.Active,
                ShopStatus = ShopAccessStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            DbContext.Users.Add(customer);
        }

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
            Name = "Bánh Mì Chảo",
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Sku = "BM-01",
            OriginalPrice = 40000,
            DealPrice = 20000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        product.Variants.Add(variant);
        DbContext.Products.Add(product);

        var now = DateTime.UtcNow;
        var deal = new FlashDeal
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            ProductId = product.Id,
            SaleStartTime = now.AddHours(-2),
            OrderEndTime = now.AddHours(2),
            ShopClosingTime = now.AddHours(3),
            Status = FlashDealStatus.OnSale,
            CreatedAt = now,
            UpdatedAt = now
        };

        var dealVariant = new FlashDealVariant
        {
            Id = Guid.NewGuid(),
            FlashDealId = deal.Id,
            VariantId = variant.Id,
            OriginalPrice = 40000,
            DealPrice = 20000,
            DiscountPercent = 50,
            TotalQuantity = 10,
            ReservedQuantity = 1,
            SoldQuantity = 1,
            Status = FlashDealVariantStatus.Active
        };
        deal.Variants.Add(dealVariant);
        DbContext.FlashDeals.Add(deal);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderCode = $"ORD{Random.Shared.Next(100000, 999999)}",
            CustomerId = customer.Id,
            ShopId = shop.Id,
            PaymentMethod = PaymentMethod.OnlinePayment,
            Status = orderStatus,
            TotalAmount = 20000,
            DepositAmount = 0,
            RemainingAmount = 0,
            DistanceKm = 1.0m,
            EtaMinutes = 10,
            SoftDeadlineAt = now.AddHours(1),
            HardDeadlineAt = now.AddHours(2),
            ConfirmedAt = now.AddMinutes(-30),
            CompletedAt = orderStatus == OrderStatus.Completed ? now.AddMinutes(-10) : null,
            CreatedAt = now.AddMinutes(-40),
            UpdatedAt = now
        };

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
            Subtotal = 20000
        };
        order.Items.Add(orderItem);

        DbContext.Orders.Add(order);
        await DbContext.SaveChangesAsync();

        // Always ensure Client is authenticated as the customer of this order
        SetBearerToken(customer.Id, customer.Email, customer.Role);

        return (customer, shop, order, orderItem, product);
    }

    // =========================================================================
    // 1. Add Feedback Tests (POST /api/feedbacks)
    // =========================================================================

    [Fact]
    public async Task AddFeedback_ValidRequest_CreatesFeedbackAndReturns_200()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync();
        var (_, _, _, orderItem, product) = await SetupOrderScenarioAsync(OrderStatus.Completed, customer);

        var request = new FeedbackRequests.CreateFeedbackRequest
        {
            OrderItemId = orderItem.Id,
            Rating = 5,
            Comment = "Delicious banh mi, very fresh!",
            Images = new List<string> { "https://cloudinary.com/test_image.jpg" }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/feedbacks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<FeedbackResponses.FeedbackItemResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Rating.Should().Be(5);
        body.Data.Comment.Should().Be("Delicious banh mi, very fresh!");
        body.Data.OrderItemId.Should().Be(orderItem.Id);
        body.Data.ProductId.Should().Be(product.Id);

        // Verify Database
        DbContext.ChangeTracker.Clear();
        var feedbackInDb = await DbContext.ProductFeedbacks
            .Include(f => f.Images)
            .FirstOrDefaultAsync(f => f.Id == body.Data.Id);
        feedbackInDb.Should().NotBeNull();
        feedbackInDb!.UserId.Should().Be(customer.Id);
        feedbackInDb.Status.Should().Be(FeedbackStatus.Visible);
        feedbackInDb.Images.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddFeedback_WhenOrderNotCompleted_ReturnsBadRequest_400_BR032()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync();
        var (_, _, _, orderItem, _) = await SetupOrderScenarioAsync(OrderStatus.Confirmed, customer);

        var request = new FeedbackRequests.CreateFeedbackRequest
        {
            OrderItemId = orderItem.Id,
            Rating = 5,
            Comment = "Trying to feedback early"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/feedbacks", request);

        // Assert - BR-032: Must return 400 BadRequest
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Verify Database
        DbContext.ChangeTracker.Clear();
        var hasFeedback = await DbContext.ProductFeedbacks.AnyAsync(f => f.OrderItemId == orderItem.Id);
        hasFeedback.Should().BeFalse();
    }

    [Fact]
    public async Task AddFeedback_WhenOrderItemBelongsToAnotherCustomer_ReturnsForbidden_403_BR049()
    {
        // Arrange
        var ownerCustomer = await AuthenticateAsCustomerAsync("custA@savebite.com", "Customer A");
        var (_, _, _, orderItem, _) = await SetupOrderScenarioAsync(OrderStatus.Completed, ownerCustomer);

        // Authenticate as a different customer B
        var otherCustomer = await AuthenticateAsCustomerAsync("custB@savebite.com", "Customer B");

        var request = new FeedbackRequests.CreateFeedbackRequest
        {
            OrderItemId = orderItem.Id,
            Rating = 4,
            Comment = "Attempt to review someone else's order item"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/feedbacks", request);

        // Assert - BR-049: 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddFeedback_WhenFeedbackAlreadySubmitted_ReturnsConflict_409_BR034()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync();
        var (_, _, _, orderItem, product) = await SetupOrderScenarioAsync(OrderStatus.Completed, customer);

        // Add first feedback directly to DB
        var existingFeedback = new ProductFeedback
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            OrderItemId = orderItem.Id,
            ProductId = product.Id,
            Rating = 5,
            Comment = "First feedback",
            Status = FeedbackStatus.Visible,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.ProductFeedbacks.Add(existingFeedback);
        await DbContext.SaveChangesAsync();

        var request = new FeedbackRequests.CreateFeedbackRequest
        {
            OrderItemId = orderItem.Id,
            Rating = 3,
            Comment = "Duplicate review attempt"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/feedbacks", request);

        // Assert - BR-034: 409 Conflict
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // =========================================================================
    // 2. View Feedback History Tests (GET /api/feedbacks/history)
    // =========================================================================

    [Fact]
    public async Task GetMyFeedbackHistory_ReturnsOnlyCustomerAuthoredFeedbacks_200_BR049()
    {
        // Arrange
        var customerA = await AuthenticateAsCustomerAsync("custA_hist@savebite.com", "Customer A");
        var (_, _, _, orderItemA, productA) = await SetupOrderScenarioAsync(OrderStatus.Completed, customerA);

        var feedbackA = new ProductFeedback
        {
            Id = Guid.NewGuid(),
            UserId = customerA.Id,
            OrderItemId = orderItemA.Id,
            ProductId = productA.Id,
            Rating = 5,
            Comment = "Review by Customer A",
            Status = FeedbackStatus.Visible,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.ProductFeedbacks.Add(feedbackA);
        await DbContext.SaveChangesAsync();

        // Customer B with separate order and orderItem
        var customerB = await AuthenticateAsCustomerAsync("custB_hist@savebite.com", "Customer B");
        var (_, _, _, orderItemB, productB) = await SetupOrderScenarioAsync(OrderStatus.Completed, customerB);

        var feedbackOther = new ProductFeedback
        {
            Id = Guid.NewGuid(),
            UserId = customerB.Id,
            OrderItemId = orderItemB.Id,
            ProductId = productB.Id,
            Rating = 1,
            Comment = "Review by Customer B",
            Status = FeedbackStatus.Visible,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.ProductFeedbacks.Add(feedbackOther);
        await DbContext.SaveChangesAsync();

        // Switch back to customerA token
        SetBearerToken(customerA.Id, customerA.Email, customerA.Role);

        // Act
        var response = await Client.GetAsync("/api/feedbacks/history");

        // Assert - BR-049: Customer A sees only their own feedback
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<FeedbackResponses.FeedbackItemResponse>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Items.Should().ContainSingle(f => f.Id == feedbackA.Id);
        body.Data.Items.Should().NotContain(f => f.Id == feedbackOther.Id);
    }

    // =========================================================================
    // 3. Edit Feedback Tests (PUT /api/feedbacks/{feedbackId})
    // =========================================================================

    [Fact]
    public async Task EditFeedback_WhenOwnFeedback_UpdatesAndReturns_200()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync();
        var (_, _, _, orderItem, product) = await SetupOrderScenarioAsync(OrderStatus.Completed, customer);

        var feedback = new ProductFeedback
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            OrderItemId = orderItem.Id,
            ProductId = product.Id,
            Rating = 3,
            Comment = "Initial average review",
            Status = FeedbackStatus.Visible,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        DbContext.ProductFeedbacks.Add(feedback);
        await DbContext.SaveChangesAsync();

        var updateRequest = new FeedbackRequests.UpdateFeedbackRequest
        {
            Rating = 5,
            Comment = "Updated: Outstanding experience!",
            Images = new List<string> { "https://cloudinary.com/new_photo.jpg" }
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/feedbacks/{feedback.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<FeedbackResponses.FeedbackItemResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Rating.Should().Be(5);
        body.Data.Comment.Should().Be("Updated: Outstanding experience!");

        // Verify Database
        DbContext.ChangeTracker.Clear();
        var updatedInDb = await DbContext.ProductFeedbacks
            .Include(f => f.Images)
            .FirstOrDefaultAsync(f => f.Id == feedback.Id);
        updatedInDb.Should().NotBeNull();
        updatedInDb!.Rating.Should().Be(5);
        updatedInDb.Comment.Should().Be("Updated: Outstanding experience!");
        updatedInDb.Images.Should().HaveCount(1);
    }

    [Fact]
    public async Task EditFeedback_WhenFeedbackBelongsToAnotherUser_ReturnsForbidden_403_BR049()
    {
        // Arrange
        var authorCustomer = await AuthenticateAsCustomerAsync("author@savebite.com", "Author Customer");
        var (_, _, _, orderItem, product) = await SetupOrderScenarioAsync(OrderStatus.Completed, authorCustomer);

        var feedback = new ProductFeedback
        {
            Id = Guid.NewGuid(),
            UserId = authorCustomer.Id,
            OrderItemId = orderItem.Id,
            ProductId = product.Id,
            Rating = 5,
            Comment = "Original author feedback",
            Status = FeedbackStatus.Visible,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.ProductFeedbacks.Add(feedback);
        await DbContext.SaveChangesAsync();

        // Switch to other customer
        await AuthenticateAsCustomerAsync("hacker@savebite.com", "Hacker Customer");

        var updateRequest = new FeedbackRequests.UpdateFeedbackRequest
        {
            Rating = 1,
            Comment = "Unauthorized edit attempt"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/feedbacks/{feedback.Id}", updateRequest);

        // Assert - BR-049: 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Verify Database: Original feedback unchanged
        DbContext.ChangeTracker.Clear();
        var dbFeedback = await DbContext.ProductFeedbacks.AsNoTracking().FirstOrDefaultAsync(f => f.Id == feedback.Id);
        dbFeedback.Should().NotBeNull();
        dbFeedback!.Rating.Should().Be(5);
        dbFeedback.Comment.Should().Be("Original author feedback");
    }

    // =========================================================================
    // 4. Delete Feedback Tests (DELETE /api/feedbacks/{feedbackId})
    // =========================================================================

    [Fact]
    public async Task DeleteFeedback_WhenOwnFeedback_SoftDeletes_200()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync();
        var (_, _, _, orderItem, product) = await SetupOrderScenarioAsync(OrderStatus.Completed, customer);

        var feedback = new ProductFeedback
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            OrderItemId = orderItem.Id,
            ProductId = product.Id,
            Rating = 4,
            Comment = "To be soft-deleted",
            Status = FeedbackStatus.Visible,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.ProductFeedbacks.Add(feedback);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/feedbacks/{feedback.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Database: Soft delete (Status == HiddenByCustomer, HiddenAt != null)
        DbContext.ChangeTracker.Clear();
        var deletedInDb = await DbContext.ProductFeedbacks.AsNoTracking().FirstOrDefaultAsync(f => f.Id == feedback.Id);
        deletedInDb.Should().NotBeNull();
        deletedInDb!.Status.Should().Be(FeedbackStatus.HiddenByCustomer);
        deletedInDb.HiddenAt.Should().NotBeNull();

        // Calling GetMyFeedbackHistory should no longer list the deleted feedback
        var historyResponse = await Client.GetAsync("/api/feedbacks/history");
        var historyBody = await historyResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResult<FeedbackResponses.FeedbackItemResponse>>>();
        historyBody.Should().NotBeNull();
        historyBody!.Data!.Items.Should().NotContain(f => f.Id == feedback.Id);
    }

    [Fact]
    public async Task DeleteFeedback_WhenFeedbackBelongsToAnotherUser_ReturnsForbidden_403_BR049()
    {
        // Arrange
        var authorCustomer = await AuthenticateAsCustomerAsync("author_del@savebite.com", "Author Del");
        var (_, _, _, orderItem, product) = await SetupOrderScenarioAsync(OrderStatus.Completed, authorCustomer);

        var feedback = new ProductFeedback
        {
            Id = Guid.NewGuid(),
            UserId = authorCustomer.Id,
            OrderItemId = orderItem.Id,
            ProductId = product.Id,
            Rating = 5,
            Comment = "Must not be deleted by other",
            Status = FeedbackStatus.Visible,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.ProductFeedbacks.Add(feedback);
        await DbContext.SaveChangesAsync();

        // Switch to another user
        await AuthenticateAsCustomerAsync("other_del@savebite.com", "Other Del");

        // Act
        var response = await Client.DeleteAsync($"/api/feedbacks/{feedback.Id}");

        // Assert - BR-049: 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Verify Database: Feedback status remains Visible
        DbContext.ChangeTracker.Clear();
        var dbFeedback = await DbContext.ProductFeedbacks.AsNoTracking().FirstOrDefaultAsync(f => f.Id == feedback.Id);
        dbFeedback.Should().NotBeNull();
        dbFeedback!.Status.Should().Be(FeedbackStatus.Visible);
        dbFeedback.HiddenAt.Should().BeNull();
    }
}
