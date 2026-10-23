using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Api.Tests.UnitTests.Support;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Api.Tests.UnitTests.Support;

// Configure Moq responses and mutation callbacks; no concrete repository or database is used.
internal sealed class CartTestContext
{
    public Guid UserId { get; } = Guid.NewGuid();
    public Mock<ICartRepository> Repo { get; } = new();
    public Mock<IRealtimePublisher> Realtime { get; } = new();
    public List<CartItem> Items { get; } = new();
    public List<FlashDealVariant> Variants { get; } = new();
    public CartService Service { get; }
    public CartTestContext()
    {
        Service = new CartService(Repo.Object, Realtime.Object);
        Repo.Setup(x => x.WithUserLockAsync(It.IsAny<Guid>(), It.IsAny<Func<Task<CartResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns((Guid _, Func<Task<CartResponse>> action, CancellationToken _) => action());
        Repo.Setup(x => x.WithUserLockAsync(It.IsAny<Guid>(), It.IsAny<Func<Task<CartMutationResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns((Guid _, Func<Task<CartMutationResponse>> action, CancellationToken _) => action());
        Repo.Setup(x => x.GetVariantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => Variants.SingleOrDefault(x => x.Id == id));
        Repo.Setup(x => x.FindVariantItemAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid user, Guid variant, CancellationToken _) => Items.SingleOrDefault(x => x.UserId == user && x.FlashDealVariantId == variant));
        Repo.Setup(x => x.FindItemAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid user, Guid id, CancellationToken _) => Items.SingleOrDefault(x => x.UserId == user && x.Id == id));
        Repo.Setup(x => x.GetItemsAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid user, Guid? shop, CancellationToken _) => (IReadOnlyList<CartItem>)Items.Where(x => x.UserId == user
                && (!shop.HasValue || x.FlashDealVariant.FlashDeal.ShopId == shop)).ToList());
        Repo.Setup(x => x.GetSummaryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid user, CancellationToken _) => new CartSummaryResponse { TotalQuantity = Items.Where(x => x.UserId == user).Sum(x => x.Quantity),
                TotalFlashDeals = Items.Where(x => x.UserId == user).Select(x => x.FlashDealVariant.FlashDealId).Distinct().Count() });
        Repo.Setup(x => x.Add(It.IsAny<CartItem>())).Callback<CartItem>(item =>
        { item.FlashDealVariant = Variants.Single(x => x.Id == item.FlashDealVariantId); Items.Add(item); });
        Repo.Setup(x => x.Remove(It.IsAny<CartItem>())).Callback<CartItem>(item => Items.Remove(item));
    }
    public CartItem Seed(bool inCart = true, int quantity = 1)
    {
        var item = UnitTestData.CartItem(UserId, DateTime.UtcNow, quantity: quantity);
        Variants.Add(item.FlashDealVariant); if (inCart) Items.Add(item); return item;
    }
}
