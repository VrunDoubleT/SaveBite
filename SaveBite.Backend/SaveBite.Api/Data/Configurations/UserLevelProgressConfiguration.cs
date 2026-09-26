using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class UserLevelProgressConfiguration
    : IEntityTypeConfiguration<UserLevelProgress>
{
    public void Configure(EntityTypeBuilder<UserLevelProgress> builder)
    {
        builder.ToTable("user_level_progress");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.CompletedOrdersCount)
            .HasColumnName("completed_orders_count")
            .IsRequired();

        builder.Property(x => x.AccumulatedOrderValue)
            .HasColumnName("accumulated_order_value")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithOne(x => x.LevelProgress)
            .HasForeignKey<UserLevelProgress>(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.UserId)
            .IsUnique();
    }
}
