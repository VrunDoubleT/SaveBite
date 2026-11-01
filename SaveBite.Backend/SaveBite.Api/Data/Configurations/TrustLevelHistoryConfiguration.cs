using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class TrustLevelHistoryConfiguration : IEntityTypeConfiguration<TrustLevelHistory>
{
    public void Configure(EntityTypeBuilder<TrustLevelHistory> builder)
    {
        builder.ToTable("trust_level_histories", table =>
        {
            table.HasCheckConstraint("ck_trust_level_history_version", "\"version_no\" >= 1");
            table.HasCheckConstraint("ck_trust_level_history_affected_users", "\"affected_user_count\" >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.TrustLevelId)
            .HasColumnName("trust_level_id")
            .IsRequired();

        builder.Property(x => x.VersionNo)
            .HasColumnName("version_no")
            .IsRequired();

        builder.Property(x => x.ChangeType)
            .HasColumnName("change_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(x => x.Rank)
            .HasColumnName("rank")
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(x => x.MinScore)
            .HasColumnName("min_score")
            .IsRequired();

        builder.Property(x => x.MinCompletedOrders)
            .HasColumnName("min_completed_orders")
            .IsRequired();

        builder.Property(x => x.MinTotalOrderValue)
            .HasColumnName("min_total_order_value")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.DepositPolicySnapshotJson)
            .HasColumnName("deposit_policy_snapshot")
            .HasColumnType("jsonb");

        builder.Property(x => x.AffectedUserCount)
            .HasColumnName("affected_user_count")
            .IsRequired();

        builder.Property(x => x.ChangedByUserId)
            .HasColumnName("changed_by_user_id");

        builder.Property(x => x.ChangedAt)
            .HasColumnName("changed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.TrustLevel)
            .WithMany(x => x.Histories)
            .HasForeignKey(x => x.TrustLevelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ChangedByUser)
            .WithMany(x => x.ChangedTrustLevelHistories)
            .HasForeignKey(x => x.ChangedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.TrustLevelId, x.VersionNo })
            .IsUnique()
            .HasDatabaseName("ux_trust_level_histories_level_version");

        builder.HasIndex(x => new { x.TrustLevelId, x.ChangedAt })
            .HasDatabaseName("ix_trust_level_histories_level_changed_at");
    }
}