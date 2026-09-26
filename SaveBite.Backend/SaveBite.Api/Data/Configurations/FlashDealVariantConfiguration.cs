using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class FlashDealVariantConfiguration
    : IEntityTypeConfiguration<FlashDealVariant>
{
    public void Configure(EntityTypeBuilder<FlashDealVariant> builder)
    {
        builder.ToTable("flash_deal_variants", table =>
        {
            table.HasCheckConstraint(
                "flash_deal_variant_stock_valid",
                "\"total_quantity\" >= 0 AND " +
                "\"reserved_quantity\" >= 0 AND " +
                "\"sold_quantity\" >= 0 AND " +
                "\"reserved_quantity\" + \"sold_quantity\" <= " +
                "\"total_quantity\"");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.FlashDealId)
            .HasColumnName("flash_deal_id")
            .IsRequired();

        builder.Property(x => x.VariantId)
            .HasColumnName("variant_id")
            .IsRequired();

        // SQL hiện tại khai báo NUMERIC không giới hạn precision/scale.
        builder.Property(x => x.OriginalPrice)
            .HasColumnName("original_price")
            .HasColumnType("numeric")
            .IsRequired();

        builder.Property(x => x.DealPrice)
            .HasColumnName("deal_price")
            .HasColumnType("numeric")
            .IsRequired();

        builder.Property(x => x.DiscountPercent)
            .HasColumnName("discount_percent")
            .HasColumnType("numeric")
            .IsRequired();

        builder.Property(x => x.TotalQuantity)
            .HasColumnName("total_quantity")
            .IsRequired();

        builder.Property(x => x.ReservedQuantity)
            .HasColumnName("reserved_quantity")
            .IsRequired();

        builder.Property(x => x.SoldQuantity)
            .HasColumnName("sold_quantity")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(x => new { x.FlashDealId, x.VariantId })
            .IsUnique()
            .HasDatabaseName("uq_deal_variant");

        builder.HasOne(x => x.FlashDeal)
            .WithMany(x => x.Variants)
            .HasForeignKey(x => x.FlashDealId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Variant)
            .WithMany(x => x.FlashDealVariants)
            .HasForeignKey(x => x.VariantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
