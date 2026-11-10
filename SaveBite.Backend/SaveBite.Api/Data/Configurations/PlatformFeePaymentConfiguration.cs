using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class PlatformFeePaymentConfiguration
    : IEntityTypeConfiguration<PlatformFeePayment>
{
    public void Configure(EntityTypeBuilder<PlatformFeePayment> builder)
    {
        builder.ToTable("platform_fee_payments", table =>
        {
            table.HasCheckConstraint(
                "ck_platform_fee_payment_amount",
                "\"amount\" > 0");
        });

        builder.Property(x => x.ProtectedPayOsCredentials).HasColumnName("protected_payos_credentials").HasColumnType("text");
        builder.Property(x => x.GatewayCheckoutUrl).HasColumnName("gateway_checkout_url").HasMaxLength(2000);
        builder.Property(x => x.GatewayQrCode).HasColumnName("gateway_qr_code").HasColumnType("text");
        builder.Property(x => x.GatewayExpiresAt).HasColumnName("gateway_expires_at");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.StatementId)
            .HasColumnName("statement_id")
            .IsRequired();

        builder.Property(x => x.ShopId)
            .HasColumnName("shop_id")
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasColumnName("amount")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(x => x.PaymentMethod)
            .HasColumnName("payment_method")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.GatewayTransactionId)
            .HasColumnName("gateway_transaction_id")
            .HasMaxLength(150);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

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

        builder.HasIndex(x => x.StatementId);
        builder.HasIndex(x => x.ShopId);
        builder.HasIndex(x => x.GatewayTransactionId)
            .IsUnique();

        builder.HasOne(x => x.Statement)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.StatementId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Shop)
            .WithMany(x => x.PlatformFeePayments)
            .HasForeignKey(x => x.ShopId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
