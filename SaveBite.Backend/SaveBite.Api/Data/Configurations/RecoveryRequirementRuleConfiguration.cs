using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class RecoveryRequirementRuleConfiguration
    : IEntityTypeConfiguration<RecoveryRequirementRule>
{
    public void Configure(EntityTypeBuilder<RecoveryRequirementRule> builder)
    {
        builder.ToTable("recovery_requirement_rules", table =>
        {
            table.HasCheckConstraint(
                "ck_recovery_requirement_rule_occurrence",
                "\"minimum_occurrence\" > 0");

            table.HasCheckConstraint(
                "ck_recovery_requirement_rule_score_deduction",
                "\"score_deduction\" > 0");

            table.HasCheckConstraint(
                "ck_recovery_requirement_rule_requirements",
                "\"required_online_orders\" >= 0 AND " +
                "\"required_online_order_value\" >= 0");

            table.HasCheckConstraint(
                "ck_recovery_requirement_rule_period",
                "\"effective_to\" IS NULL OR " +
                "\"effective_to\" > \"effective_from\"");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.MinimumOccurrence)
            .HasColumnName("minimum_occurrence")
            .IsRequired();

        builder.Property(x => x.ScoreDeduction)
            .HasColumnName("score_deduction")
            .IsRequired();

        builder.Property(x => x.RequiredOnlineOrders)
            .HasColumnName("required_online_orders")
            .IsRequired();

        builder.Property(x => x.RequiredOnlineOrderValue)
            .HasColumnName("required_online_order_value")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.PermanentOfflineLock)
            .HasColumnName("permanent_offline_lock")
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(x => x.EffectiveFrom)
            .HasColumnName("effective_from")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.EffectiveTo)
            .HasColumnName("effective_to")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.CreatedByUser)
            .WithMany(x => x.CreatedRecoveryRequirementRules)
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
