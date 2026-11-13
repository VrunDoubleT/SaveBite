using System.Net;
using FluentAssertions;
using Moq;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.UnitTests.FeedbackManagement;

public class ViewShopFeedbacksTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IFeedbackRepository> _feedbackRepoMock = new();
    private readonly Mock<INotificationQueue> _notificationsMock = new();
    private readonly Mock<IAdminAuditWriter> _auditMock = new();
    private readonly FeedbackService _service;

    public ViewShopFeedbacksTests()
    {
        _db = TestDbContextFactory.Create();
        _service = new FeedbackService(
            _feedbackRepoMock.Object,
            _notificationsMock.Object,
            _db,
            _auditMock.Object);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task GetShopFeedbacksAsync_WhenShopExists_ReturnsPagedMappedFeedbacks()
    {
        var userId = Guid.NewGuid();
        var shopId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var orderItemId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var feedbackId = Guid.NewGuid();
        var replyUserId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var shop = new Shop
        {
            Id = shopId,
            OwnerUserId = userId,
            Name = "Green Bakery",
            LogoUrl = "https://cdn.example.com/logo.png",
            AddressLine = "12 Le Loi"
        };

        var order = new Order
        {
            Id = orderId,
            OrderCode = "SB-2026-001",
            ShopId = shopId,
            Shop = shop
        };

        var orderItem = new OrderItem
        {
            Id = orderItemId,
            OrderId = orderId,
            Order = order,
            ProductNameSnapshot = "Croissant Box",
            VariantNameSnapshot = "2 pcs"
        };

        var product = new Product
        {
            Id = productId,
            ShopId = shopId,
            Shop = shop,
            Name = "Croissant",
            Images = new List<ProductImage>
            {
                new() { Id = Guid.NewGuid(), ProductId = productId, ImageUrl = "https://cdn.example.com/prod.png" }
            }
        };

        var customer = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Minh Anh",
            AvatarUrl = "https://cdn.example.com/avatar.png"
        };

        var reply = new ProductFeedbackReply
        {
            Id = Guid.NewGuid(),
            ProductFeedbackId = feedbackId,
            RepliedByUserId = replyUserId,
            Content = "Thank you for supporting SaveBite!",
            CreatedAt = now.AddMinutes(-5),
            UpdatedAt = now.AddMinutes(-5),
            RepliedByUser = new User { Id = replyUserId, FullName = "Owner Green" }
        };

        var feedback = new ProductFeedback
        {
            Id = feedbackId,
            OrderItemId = orderItemId,
            ProductId = productId,
            UserId = customer.Id,
            Rating = 5,
            Comment = "Fresh and delicious!",
            Status = FeedbackStatus.Visible,
            CreatedAt = now.AddMinutes(-10),
            UpdatedAt = now.AddMinutes(-10),
            OrderItem = orderItem,
            Product = product,
            User = customer,
            Reply = reply,
            Images = new List<ProductFeedbackImage>
            {
                new() { Id = Guid.NewGuid(), ProductFeedbackId = feedbackId, ImageUrl = "https://cdn.example.com/fb2.png", SortOrder = 2 },
                new() { Id = Guid.NewGuid(), ProductFeedbackId = feedbackId, ImageUrl = "https://cdn.example.com/fb1.png", SortOrder = 1 }
            }
        };

        var request = new FeedbackRequests.ShopFeedbacksQueryRequest
        {
            Page = 1,
            PageSize = 10,
            Rating = 5,
            HasReply = true
        };

        _feedbackRepoMock
            .Setup(r => r.GetShopIdForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shopId);

        _feedbackRepoMock
            .Setup(r => r.GetShopFeedbacksAsync(shopId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PagedResult<ProductFeedback>.Create(new[] { feedback }, 1, 10, 1));

        var result = await _service.GetShopFeedbacksAsync(userId, request);

        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalItems.Should().Be(1);
        result.Items.Should().ContainSingle();

        var item = result.Items[0];
        item.Id.Should().Be(feedbackId);
        item.ProductName.Should().Be("Croissant Box");
        item.VariantName.Should().Be("2 pcs");
        item.Rating.Should().Be(5);
        item.Comment.Should().Be("Fresh and delicious!");
        item.OrderCode.Should().Be("SB-2026-001");
        item.ShopName.Should().Be("Green Bakery");
        item.ShopId.Should().Be(shopId);
        item.CustomerName.Should().Be("Minh Anh");
        item.ImageUrl.Should().Be("https://cdn.example.com/prod.png");
        item.Images.Should().ContainInOrder("https://cdn.example.com/fb1.png", "https://cdn.example.com/fb2.png");
        item.Reply.Should().NotBeNull();
        item.Reply!.Content.Should().Be("Thank you for supporting SaveBite!");
        item.Reply.RepliedByName.Should().Be("Owner Green");
        item.Reply.RepliedByUserId.Should().Be(replyUserId);
    }

    [Fact]
    public async Task GetShopFeedbacksAsync_WhenFeedbackHasNoReply_MapsReplyAsNull()
    {
        var userId = Guid.NewGuid();
        var shopId = Guid.NewGuid();
        var feedback = new ProductFeedback
        {
            Id = Guid.NewGuid(),
            OrderItemId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Rating = 4,
            Comment = "Good value",
            Status = FeedbackStatus.Visible,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Product = new Product { Id = Guid.NewGuid(), ShopId = shopId, Name = "Baguette" },
            Reply = null
        };

        var request = new FeedbackRequests.ShopFeedbacksQueryRequest { Page = 2, PageSize = 5, HasReply = false };

        _feedbackRepoMock
            .Setup(r => r.GetShopIdForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shopId);

        _feedbackRepoMock
            .Setup(r => r.GetShopFeedbacksAsync(shopId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PagedResult<ProductFeedback>.Create(new[] { feedback }, 2, 5, 6));

        var result = await _service.GetShopFeedbacksAsync(userId, request);

        result.Page.Should().Be(2);
        result.TotalItems.Should().Be(6);
        result.Items[0].Reply.Should().BeNull();
        result.Items[0].ProductName.Should().Be("Baguette");
    }

    [Fact]
    public async Task GetShopFeedbacksAsync_WhenUserHasNoShop_ThrowsNotFound()
    {
        var userId = Guid.NewGuid();
        _feedbackRepoMock
            .Setup(r => r.GetShopIdForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var act = () => _service.GetShopFeedbacksAsync(userId, new FeedbackRequests.ShopFeedbacksQueryRequest());

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("No shop found associated with your account.");
        _feedbackRepoMock.Verify(r => r.GetShopFeedbacksAsync(It.IsAny<Guid>(), It.IsAny<FeedbackRequests.ShopFeedbacksQueryRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
