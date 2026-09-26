using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items", table =>
        {
            table.HasCheckConstraint(
                "order_items_valid",
                "\"unit_price\" >= 0 AND " +
                "\"quantity\" > 0 AND " +
                "(\"subtotal\" IS NULL OR \"subtotal\" >= 0)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(x => x.FlashDealVariantId)
            .HasColumnName("flash_deal_variant_id")
            .IsRequired();

        builder.Property(x => x.ProductNameSnapshot)
            .HasColumnName("product_name_snapshot")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.VariantNameSnapshot)
            .HasColumnName("variant_name_snapshot")
            .HasMaxLength(200);
        
        builder.Property(x => x.OriginalPriceSnapshot)
            .HasColumnName("original_price_snapshot")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.DealPriceSnapshot)
            .HasColumnName("deal_price_snapshot")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.UnitPrice)
            .HasColumnName("unit_price")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(x => x.Subtotal)
            .HasColumnName("subtotal")
            .HasPrecision(12, 2);

        builder.HasOne(x => x.Order)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.FlashDealVariant)
            .WithMany(x => x.OrderItems)
            .HasForeignKey(x => x.FlashDealVariantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
