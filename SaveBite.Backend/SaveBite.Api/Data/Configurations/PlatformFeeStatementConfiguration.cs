using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Data.Configurations;

public class PlatformFeeStatementConfiguration
    : IEntityTypeConfiguration<PlatformFeeStatement>
{
    public void Configure(EntityTypeBuilder<PlatformFeeStatement> builder)
    {
        builder.ToTable("platform_fee_statements", table =>
        {
            table.HasCheckConstraint(
                "ck_platform_fee_statement_month",
                "\"billing_month\" BETWEEN 1 AND 12");

            table.HasCheckConstraint(
                "ck_platform_fee_statement_total",
                "\"total_fee_amount\" >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.ShopId)
            .HasColumnName("shop_id")
            .IsRequired();

        builder.Property(x => x.BillingYear)
            .HasColumnName("billing_year")
            .IsRequired();

        builder.Property(x => x.BillingMonth)
            .HasColumnName("billing_month")
            .IsRequired();
        
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.TotalFeeAmount)
            .HasColumnName("total_fee_amount")
            .HasPrecision(14, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.IssuedAt)
            .HasColumnName("issued_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.DueAt)
            .HasColumnName("due_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.PaidAt)
            .HasColumnName("paid_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        
        builder.HasIndex(x => new
            {
                x.ShopId,
                x.BillingYear,
                x.BillingMonth
            })
            .IsUnique();

        builder.HasOne(x => x.Shop)
            .WithMany(x => x.PlatformFeeStatements)
            .HasForeignKey(x => x.ShopId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
