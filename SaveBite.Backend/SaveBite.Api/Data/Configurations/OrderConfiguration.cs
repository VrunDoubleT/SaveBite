using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.OrderCode)
            .HasColumnName("order_code")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.OrderDate).HasColumnName("order_date")
            .HasComputedColumnSql("(created_at AT TIME ZONE 'Asia/Ho_Chi_Minh')::date", stored: true);
        builder.HasIndex(x => new { x.ShopId, x.OrderDate, x.OrderCode })
            .IsUnique().HasDatabaseName("ux_orders_shop_date_code");

        builder.Property(x => x.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(x => x.ShopId)
            .HasColumnName("shop_id")
            .IsRequired();

        builder.Property(x => x.PaymentMethod)
            .HasColumnName("payment_method")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.DepositAmount)
            .HasColumnName("deposit_amount")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.RemainingAmount)
            .HasColumnName("remaining_amount")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.DistanceKm)
            .HasColumnName("distance_km")
            .HasPrecision(8, 3);

        builder.Property(x => x.EtaMinutes)
            .HasColumnName("eta_minutes");

        builder.Property(x => x.SoftDeadlineAt)
            .HasColumnName("soft_deadline_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.HardDeadlineAt)
            .HasColumnName("hard_deadline_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.IsLateArrival)
            .HasColumnName("is_late_arrival")
            .IsRequired();

        builder.Property(x => x.NoShow)
            .HasColumnName("no_show")
            .IsRequired();

        builder.Property(x => x.CheckoutQuoteId).HasColumnName("checkout_quote_id");
        builder.Property(x => x.CheckoutRequestId).HasColumnName("checkout_request_id");
        builder.Property(x => x.CheckoutPolicySnapshotJson).HasColumnName("checkout_policy_snapshot_json").HasColumnType("jsonb");
        builder.Property(x => x.RequiredOnlineAmount).HasColumnName("required_online_amount").HasPrecision(12, 2);
        builder.Property(x => x.PaymentExpiresAt).HasColumnName("payment_expires_at");
        builder.Property(x => x.ConfirmationExpirationJobId).HasColumnName("confirmation_expiration_job_id").HasMaxLength(100);
        builder.Property(x => x.PaymentExpirationJobId).HasColumnName("payment_expiration_job_id").HasMaxLength(100);
        builder.Property(x => x.InventoryReservationActive).HasColumnName("inventory_reservation_active");
        builder.Property(x => x.PaymentReviewRequired).HasColumnName("payment_review_required");
        builder.HasIndex(x => new { x.CustomerId, x.CheckoutRequestId }).IsUnique();
        builder.HasIndex(x => x.CheckoutQuoteId).IsUnique();
        builder.HasIndex(x => new { x.InventoryReservationActive, x.PaymentExpiresAt });

        builder.Property(x => x.CancelReason)
            .HasColumnName("cancel_reason")
            .HasColumnType("text");

        builder.Property(x => x.CancelledBy)
            .HasColumnName("cancelled_by");

        builder.Property(x => x.ConfirmedAt)
            .HasColumnName("confirmed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ReadyAt)
            .HasColumnName("ready_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CompletedAt)
            .HasColumnName("completed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CancelledAt)
            .HasColumnName("cancelled_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ExpiredAt)
            .HasColumnName("expired_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.CancelledByUser)
            .WithMany(x => x.CancelledOrders)
            .HasForeignKey(x => x.CancelledBy)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Shop)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.ShopId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
