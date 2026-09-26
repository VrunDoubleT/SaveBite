using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class NoShowPenaltyOrderHistoryConfiguration
    : IEntityTypeConfiguration<NoShowPenaltyOrderHistory>
{
    public void Configure(
        EntityTypeBuilder<NoShowPenaltyOrderHistory> builder)
    {
        builder.ToTable("no_show_penalty_order_history");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.NoShowPenaltyId)
            .HasColumnName("no_show_penalty_id")
            .IsRequired();

        builder.Property(x => x.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(x => x.OrderValue)
            .HasColumnName("order_value")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.NoShowPenalty)
            .WithMany(x => x.RecoveryOrders)
            .HasForeignKey(x => x.NoShowPenaltyId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Order)
            .WithOne(x => x.RecoveryPenaltyHistory)
            .HasForeignKey<NoShowPenaltyOrderHistory>(x => x.OrderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.OrderId)
            .IsUnique();
    }
}
