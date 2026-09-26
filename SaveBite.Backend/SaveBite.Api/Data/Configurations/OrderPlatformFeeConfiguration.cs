using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class OrderPlatformFeeConfiguration
    : IEntityTypeConfiguration<OrderPlatformFee>
{
    public void Configure(EntityTypeBuilder<OrderPlatformFee> builder)
    {
        builder.ToTable("order_platform_fees", table =>
        {
            table.HasCheckConstraint(
                "ck_order_platform_fee_gross",
                "\"gross_amount\" >= 0");

            table.HasCheckConstraint(
                "ck_order_platform_fee_rate",
                "\"fee_rate\" BETWEEN 0 AND 1");

            table.HasCheckConstraint(
                "ck_order_platform_fee_amount",
                "\"fee_amount\" >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.HasIndex(x => x.OrderId).IsUnique();

        builder.Property(x => x.ShopId)
            .HasColumnName("shop_id")
            .IsRequired();

        builder.Property(x => x.FeeConfigId)
            .HasColumnName("fee_config_id")
            .IsRequired();

        builder.Property(x => x.StatementId)
            .HasColumnName("statement_id");

        builder.Property(x => x.GrossAmount)
            .HasColumnName("gross_amount")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(x => x.FeeRate)
            .HasColumnName("fee_rate")
            .HasPrecision(7, 6)
            .IsRequired();

        builder.Property(x => x.FeeAmount)
            .HasColumnName("fee_amount")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(x => x.CalculatedAt)
            .HasColumnName("calculated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.Order)
            .WithOne(x => x.PlatformFee)
            .HasForeignKey<OrderPlatformFee>(x => x.OrderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Shop)
            .WithMany(x => x.OrderPlatformFees)
            .HasForeignKey(x => x.ShopId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.FeeConfig)
            .WithMany(x => x.OrderPlatformFees)
            .HasForeignKey(x => x.FeeConfigId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Statement)
            .WithMany(x => x.OrderPlatformFees)
            .HasForeignKey(x => x.StatementId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
