using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class NoShowPenaltyConfiguration
    : IEntityTypeConfiguration<NoShowPenalty>
{
    public void Configure(EntityTypeBuilder<NoShowPenalty> builder)
    {
        builder.ToTable("no_show_penalties");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(x => x.RecoveryRuleId)
            .HasColumnName("recovery_rule_id")
            .IsRequired();

        builder.Property(x => x.PenaltyYear)
            .HasColumnName("penalty_year")
            .IsRequired();

        builder.Property(x => x.PenaltyMonth)
            .HasColumnName("penalty_month")
            .IsRequired();

        builder.Property(x => x.OccurrenceNumber)
            .HasColumnName("occurrence_number")
            .IsRequired();

        builder.Property(x => x.ScoreDeducted)
            .HasColumnName("score_deducted")
            .IsRequired();

        builder.Property(x => x.RequiredOnlineOrders)
            .HasColumnName("required_online_orders")
            .IsRequired();

        builder.Property(x => x.RequiredOnlineOrderValue)
            .HasColumnName("required_online_order_value")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.CompletedOnlineOrders)
            .HasColumnName("completed_online_orders")
            .IsRequired();

        builder.Property(x => x.CompletedOnlineOrderValue)
            .HasColumnName("completed_online_order_value")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.PermanentOfflineLock)
            .HasColumnName("permanent_offline_lock")
            .IsRequired();

        builder.Property(x => x.LockEndsAt)
            .HasColumnName("lock_ends_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.RecoveredAt)
            .HasColumnName("recovered_at")
            .HasColumnType("timestamp with time zone");

        builder.HasOne(x => x.User)
            .WithMany(x => x.NoShowPenalties)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Order)
            .WithOne(x => x.NoShowPenalty)
            .HasForeignKey<NoShowPenalty>(x => x.OrderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.RecoveryRule)
            .WithMany(x => x.NoShowPenalties)
            .HasForeignKey(x => x.RecoveryRuleId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.OrderId)
            .IsUnique();
    }
}
