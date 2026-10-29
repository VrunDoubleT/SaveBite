using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;
namespace SaveBite.Backend.Data.Configurations;

public class RefundItemConfiguration : IEntityTypeConfiguration<RefundItem>
{
    public void Configure(EntityTypeBuilder<RefundItem> b)
    {
        b.ToTable("refund_items", table => table.HasCheckConstraint("ck_refund_items_values", "quantity > 0 AND quantity <= purchased_quantity AND unit_price >= 0 AND amount >= 0"));
        b.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(1000).IsRequired();
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.RefundId).HasColumnName("refund_id");
        b.Property(x => x.OrderItemId).HasColumnName("order_item_id");
        b.Property(x => x.ProductName).HasColumnName("product_name").HasMaxLength(200);
        b.Property(x => x.VariantName).HasColumnName("variant_name").HasMaxLength(200);
        b.Property(x => x.ImageUrl).HasColumnName("image_url");
        b.Property(x => x.PurchasedQuantity).HasColumnName("purchased_quantity");
        b.Property(x => x.Quantity).HasColumnName("quantity");
        b.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(12, 2);
        b.Property(x => x.Amount).HasColumnName("amount").HasPrecision(12, 2);
        b.HasIndex(x => new { x.RefundId, x.OrderItemId }).IsUnique();
        b.HasOne(x => x.Refund).WithMany(x => x.Items).HasForeignKey(x => x.RefundId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.OrderItem).WithMany().HasForeignKey(x => x.OrderItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
