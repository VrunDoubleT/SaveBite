using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("refunds", table =>
        {
            table.HasCheckConstraint(
                "ck_refunds_requested_amount",
                "\"requested_amount\" > 0");
            
            table.HasCheckConstraint(
                "ck_refunds_shop_cancellation_not_rejected",
                "\"refund_type\" <> 'ShopCancellation' OR " +
                "\"status\" <> 'Rejected'");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(x => x.RefundType)
            .HasColumnName("refund_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.InitiatedBy)
            .HasColumnName("initiated_by")
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasColumnName("reason")
            .HasColumnType("text");

        builder.Property(x => x.RequestedAmount)
            .HasColumnName("requested_amount")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.ReceiverMethod)
            .HasColumnName("receiver_method")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.ReceiverBankName)
            .HasColumnName("receiver_bank_name")
            .HasMaxLength(100);

        builder.Property(x => x.ReceiverAccountNumber)
            .HasColumnName("receiver_account_number")
            .HasMaxLength(50);

        builder.Property(x => x.ReceiverAccountHolder)
            .HasColumnName("receiver_account_holder")
            .HasMaxLength(150);

        builder.Property(x => x.ReceiverQrImageUrl)
            .HasColumnName("receiver_qr_image_url")
            .HasColumnType("text");

        builder.Property(x => x.ReviewedBy)
            .HasColumnName("reviewed_by");

        builder.Property(x => x.ReviewedAt)
            .HasColumnName("reviewed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.RejectReason)
            .HasColumnName("reject_reason")
            .HasColumnType("text");

        builder.Property(x => x.TransferReference)
            .HasColumnName("transfer_reference")
            .HasMaxLength(150);

        builder.Property(x => x.TransferredAt)
            .HasColumnName("transferred_at")
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
            .WithMany(x => x.Refunds)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.InitiatedByUser)
            .WithMany(x => x.InitiatedRefunds)
            .HasForeignKey(x => x.InitiatedBy)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.ReviewedByUser)
            .WithMany(x => x.ReviewedRefunds)
            .HasForeignKey(x => x.ReviewedBy)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasIndex(x => x.OrderId)
            .IsUnique()
            .HasDatabaseName(
                "ux_refunds_shop_cancellation_per_order")
            .HasFilter(
                "\"refund_type\" = 'ShopCancellation'");
    }
}
