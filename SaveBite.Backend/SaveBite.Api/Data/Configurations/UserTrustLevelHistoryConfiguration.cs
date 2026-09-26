using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class UserTrustLevelHistoryConfiguration
    : IEntityTypeConfiguration<UserTrustLevelHistory>
{
    public void Configure(
        EntityTypeBuilder<UserTrustLevelHistory> builder)
    {
        builder.ToTable("user_trust_level_history");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.PreviousTrustLevelId)
            .HasColumnName("previous_trust_level_id");

        builder.Property(x => x.NewTrustLevelId)
            .HasColumnName("new_trust_level_id")
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasColumnName("reason")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.OrderId)
            .HasColumnName("order_id");

        builder.Property(x => x.NoShowPenaltyId)
            .HasColumnName("no_show_penalty_id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.TrustLevelHistories)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.PreviousTrustLevel)
            .WithMany(x => x.PreviousLevelHistories)
            .HasForeignKey(x => x.PreviousTrustLevelId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.NewTrustLevel)
            .WithMany(x => x.NewLevelHistories)
            .HasForeignKey(x => x.NewTrustLevelId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Order)
            .WithMany(x => x.TrustLevelHistories)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.NoShowPenalty)
            .WithMany(x => x.TrustLevelHistories)
            .HasForeignKey(x => x.NoShowPenaltyId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
