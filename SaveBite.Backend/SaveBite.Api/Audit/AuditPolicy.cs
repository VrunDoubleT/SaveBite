using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Audit;

// Explicit business roots: newly introduced entities are not audited accidentally.
public static class AuditPolicy
{
    private static readonly HashSet<Type> Roots =
    [
        typeof(Product), typeof(FlashDeal), typeof(ShopApplication), typeof(Shop),
        typeof(ProductFeedback), typeof(ProductFeedbackReply), typeof(User), typeof(UserAddress),
        typeof(Order), typeof(Refund), typeof(ShopStaff), typeof(StaffInvitation)
    ];

    internal static IEnumerable<Type> RootTypes => Roots;

    public static (string Type, Guid Id)? Target(EntityEntry entry, DbContext db)
    {
        var type = entry.Metadata.ClrType;
        if (Roots.Contains(type))
            return entry.Property("Id").CurrentValue is Guid id && id != Guid.Empty ? (type.Name, id) : null;
        return entry.Entity switch
        {
            ProductContentRevision x => (nameof(Product), x.ProductId),
            ProductImage x => (nameof(Product), x.ProductId),
            ProductAttribute x => (nameof(Product), x.ProductId),
            ProductVariant x => (nameof(Product), x.ProductId),
            ProductAttributeValue x => AttributeProduct(x, db),
            ProductFeedbackImage x => (nameof(ProductFeedback), x.ProductFeedbackId),
            ShopApplicationDocument x => (nameof(ShopApplication), x.ApplicationId),
            FlashDealVariant x => (nameof(FlashDeal), x.FlashDealId),
            RefundItem x => (nameof(Refund), x.RefundId),
            RefundEvidenceImage x => (nameof(Refund), x.RefundId),
            _ => null
        };
    }

    public static (string Type, Guid Id) ManualTarget(AuditLog log, DbContext db)
    {
        if (log.TargetType is "ProductRevision" or nameof(ProductContentRevision))
        {
            var revision = db.ChangeTracker.Entries<ProductContentRevision>()
                .FirstOrDefault(x => x.Entity.Id == log.TargetId)?.Entity;
            if (revision != null) return (nameof(Product), revision.ProductId);
        }
        return (log.TargetType, log.TargetId);
    }

    private static (string, Guid)? AttributeProduct(ProductAttributeValue value, DbContext db)
    {
        var attribute = db.ChangeTracker.Entries<ProductAttribute>().FirstOrDefault(x => x.Entity.Id == value.AttributeId)?.Entity;
        var id = attribute?.ProductId ?? db.Set<ProductAttribute>().AsNoTracking().Where(x => x.Id == value.AttributeId).Select(x => x.ProductId).FirstOrDefault();
        return id == Guid.Empty ? null : (nameof(Product), id);
    }

    public static bool Ignore(string property) => property is "UpdatedAt" or "CreatedAt" or "PublicId"
        or "EditCount" or "LastEditedAt" or "ReservedQuantity" or "SoldQuantity" || property.EndsWith("JobId", StringComparison.Ordinal);
}
