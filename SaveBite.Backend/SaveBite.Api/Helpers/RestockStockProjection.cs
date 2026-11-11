using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Services.Implementations;

namespace SaveBite.Backend.Helpers;

public static class RestockStockProjection
{
    // An expired decision has the same stock outcome before and after its background job commits.
    public static ResaleItemResponse Item(OrderItem item, DateTime? deadline, DateTime now)
    {
        var expired = item.ResaleDecisionStatus == ResaleDecisionStatus.Pending && deadline.HasValue && deadline <= now;
        return new(item.Id, item.ProductNameSnapshot, item.Quantity,
            (expired ? ResaleDecisionStatus.Expired : item.ResaleDecisionStatus).ToString(),
            expired ? 0 : item.ResaleQuantity,
            expired ? item.Quantity : item.UnsellableQuantity,
            expired ? deadline : item.ResaleDecidedAt, expired ? null : item.ResaleDecidedBy,
            item.FlashDealVariant?.FlashDeal?.Product?.Images.OrderBy(image => image.SortOrder).Select(image => image.ImageUrl).FirstOrDefault(),
            item.VariantNameSnapshot, item.FlashDealVariant?.FlashDealId ?? Guid.Empty, item.UnitPrice);
    }
}
