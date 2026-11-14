using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using Xunit;

namespace SaveBite.Tests;
public class FlashDealStockBreakdownTests
{
    [Fact]
    public void PendingRestockIsSeparateFromSoldAndPaymentReservationsWithoutChangingAvailableStock()
    {
        var variant = new FlashDealVariant { TotalQuantity = 50, SoldQuantity = 12, ReservedQuantity = 3 };
        var stock = FlashDealStockBreakdown.From(variant, new(2, 4));
        Assert.Equal(6, stock.Sold); Assert.Equal(3, stock.Reserved);
        Assert.Equal(4, stock.PendingRestock); Assert.Equal(2, stock.Stopped); Assert.Equal(35, stock.Remaining);
        var response = new ShopFlashDealVariantDetailResponse { TotalQuantity = 50, SoldQuantity = stock.Sold,
            ReservedQuantity = stock.Reserved, PendingRestockQuantity = stock.PendingRestock, UnsellableQuantity = stock.Stopped };
        Assert.Equal(stock.Remaining, response.AvailableQuantity);
        var customer = new FlashDealVariantResponse { TotalQuantity = 50, SoldQuantity = stock.Sold,
            ReservedQuantity = stock.Reserved, PendingRestockQuantity = stock.PendingRestock, UnsellableQuantity = stock.Stopped };
        Assert.Equal(stock.Remaining, customer.AvailableQuantity);
        Assert.Equal(12, variant.SoldQuantity);
    }
    [Fact]
    public void PendingDecisionBecomesStoppedAtDeadlineWithoutBecomingSoldOrAvailable()
    {
        var now = DateTime.UtcNow;
        var item = new OrderItem { FlashDealVariantId = Guid.NewGuid(), Quantity = 4, ResaleDecisionStatus = ResaleDecisionStatus.Pending,
            Order = new Order { ResaleDecisionDeadlineAt = now.AddMinutes(1) } };
        var before = FlashDealStockBreakdown.Summarize(new[] { item }.AsQueryable(), now).Single();
        var after = FlashDealStockBreakdown.Summarize(new[] { item }.AsQueryable(), now.AddMinutes(1)).Single();
        Assert.Equal(4, before.Pending); Assert.Equal(0, before.Stopped);
        Assert.Equal(0, after.Pending); Assert.Equal(4, after.Stopped);
        var variant = new FlashDealVariant { TotalQuantity = 10, SoldQuantity = 6 };
        var stock = FlashDealStockBreakdown.From(variant, new(after.Stopped, after.Pending));
        Assert.Equal(2, stock.Sold); Assert.Equal(4, stock.Remaining);
    }
    [Fact]
    public void RestockedUnitsBecomeAvailableAndNeverRemainPending()
    {
        var variant = new FlashDealVariant { TotalQuantity = 10, SoldQuantity = 2 };
        var stock = FlashDealStockBreakdown.From(variant, new(0, 0));
        Assert.Equal(2, stock.Sold); Assert.Equal(8, stock.Remaining); Assert.Equal(0, stock.PendingRestock);
    }
    [Fact]
    public void MixedDecisionsCountEachUnitOnceAfterPartialRestock()
    {
        var now = DateTime.UtcNow;
        var id = Guid.NewGuid();
        OrderItem Item(int quantity, ResaleDecisionStatus status, int stopped, DateTime deadline)
            => new() { FlashDealVariantId = id, Quantity = quantity, ResaleDecisionStatus = status,
                UnsellableQuantity = stopped, Order = new Order { ResaleDecisionDeadlineAt = deadline } };
        var items = new[] {
            Item(4, ResaleDecisionStatus.Pending, 0, now.AddMinutes(1)),
            Item(3, ResaleDecisionStatus.Pending, 0, now),
            Item(5, ResaleDecisionStatus.Decided, 2, now.AddMinutes(1)),
            Item(2, ResaleDecisionStatus.Expired, 2, now.AddMinutes(-1))
        };
        var row = FlashDealStockBreakdown.Summarize(items.AsQueryable(), now).Single();
        var stock = FlashDealStockBreakdown.From(new FlashDealVariant {
            TotalQuantity = 30, SoldQuantity = 17, ReservedQuantity = 2 }, new(row.Stopped, row.Pending));
        Assert.Equal(4, stock.PendingRestock);
        Assert.Equal(7, stock.Stopped);
        Assert.Equal(6, stock.Sold);
        Assert.Equal(11, stock.Remaining);
        Assert.Equal(30, stock.Sold + stock.Reserved + stock.PendingRestock + stock.Stopped + stock.Remaining);
    }
}
