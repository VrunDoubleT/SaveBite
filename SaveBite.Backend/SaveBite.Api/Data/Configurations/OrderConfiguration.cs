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

        builder.HasIndex(x => x.OrderCode).IsUnique();

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
