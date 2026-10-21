using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.Property(x => x.GatewayQrCode).HasColumnName("gateway_qr_code").HasColumnType("text");
        builder.HasIndex(x => x.OrderId).HasDatabaseName("IX_payments_order_id");
        builder.HasIndex(x => x.OrderId, "ux_payments_remaining_per_order").IsUnique().HasFilter("\"payment_type\" = 'Remaining' AND \"status\" = 'Pending'").HasDatabaseName("ux_payments_remaining_per_order");
        builder.Property(x => x.CollectedAmount).HasColumnName("collected_amount").HasPrecision(12, 2);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(x => x.PaymentType)
            .HasColumnName("payment_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Method)
            .HasColumnName("method")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasColumnName("amount")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.GatewayCreationAttempted).HasColumnName("gateway_creation_attempted");
        builder.Property(x => x.GatewayOrderCode).HasColumnName("gateway_order_code");
        builder.Property(x => x.GatewayCheckoutUrl).HasColumnName("gateway_checkout_url").HasMaxLength(2000);
        builder.HasIndex(x => x.GatewayOrderCode).IsUnique();

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

        builder.HasOne(x => x.Order)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
