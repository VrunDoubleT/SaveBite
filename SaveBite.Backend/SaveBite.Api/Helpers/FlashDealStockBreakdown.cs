using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
namespace SaveBite.Backend.Helpers;

public sealed class FlashDealStockDecisionRow
{
    public Guid Id { get; init; }
    public int Stopped { get; init; }
    public int Pending { get; init; }
}
public sealed record FlashDealStockDecisions(int StoppedQuantity, int PendingQuantity);
public sealed record FlashDealStockBreakdown(int Sold, int Reserved, int Stopped, int Remaining, int PendingRestock = 0)
{
    public static IQueryable<FlashDealStockDecisionRow> Summarize(IQueryable<OrderItem> items, DateTime now)
        => items.GroupBy(i => i.FlashDealVariantId).Select(g => new FlashDealStockDecisionRow {
            Id = g.Key,
            Stopped = g.Sum(i => i.ResaleDecisionStatus == ResaleDecisionStatus.Pending && i.Order.ResaleDecisionDeadlineAt <= now
                ? i.Quantity : i.UnsellableQuantity),
            Pending = g.Sum(i => i.ResaleDecisionStatus == ResaleDecisionStatus.Pending && i.Order.ResaleDecisionDeadlineAt > now
                ? i.Quantity : 0)
        });

    // Stored SoldQuantity is allocated stock, including terminal orders awaiting disposal/restock.
    // Display each unit once without changing the allocation used to prevent overselling.
    public static FlashDealStockBreakdown From(FlashDealVariant variant, FlashDealStockDecisions? decisions)
    {
        var stopped = decisions?.StoppedQuantity ?? 0;
        var pending = decisions?.PendingQuantity ?? 0;
        return new(Math.Max(0, variant.SoldQuantity - stopped - pending),
            variant.ReservedQuantity, stopped,
            Math.Max(0, variant.TotalQuantity - variant.SoldQuantity - variant.ReservedQuantity), pending);
    }
}
