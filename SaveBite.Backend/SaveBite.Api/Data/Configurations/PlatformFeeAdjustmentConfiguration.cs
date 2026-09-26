using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class PlatformFeeAdjustmentConfiguration
    : IEntityTypeConfiguration<PlatformFeeAdjustment>
{
    public void Configure(
        EntityTypeBuilder<PlatformFeeAdjustment> builder)
    {
        builder.ToTable("platform_fee_adjustments", table =>
        {
            table.HasCheckConstraint(
                "ck_platform_fee_adjustment_negative",
                "\"amount\" < 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.ShopId)
            .HasColumnName("shop_id")
            .IsRequired();

        builder.Property(x => x.OrderFeeId)
            .HasColumnName("order_fee_id")
            .IsRequired();

        builder.Property(x => x.RefundId)
            .HasColumnName("refund_id")
            .IsRequired();
        
        builder.HasIndex(x => x.RefundId)
            .IsUnique();

        builder.Property(x => x.StatementId)
            .HasColumnName("statement_id");

        builder.Property(x => x.Amount)
            .HasColumnName("amount")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasColumnName("reason")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.Shop)
            .WithMany(x => x.PlatformFeeAdjustments)
            .HasForeignKey(x => x.ShopId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.OrderFee)
            .WithMany(x => x.Adjustments)
            .HasForeignKey(x => x.OrderFeeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Refund)
            .WithOne(x => x.PlatformFeeAdjustment)
            .HasForeignKey<PlatformFeeAdjustment>(x => x.RefundId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Statement)
            .WithMany(x => x.Adjustments)
            .HasForeignKey(x => x.StatementId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
