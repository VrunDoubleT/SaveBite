using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class PlatformFeeConfigConfiguration
    : IEntityTypeConfiguration<PlatformFeeConfig>
{
    public void Configure(EntityTypeBuilder<PlatformFeeConfig> builder)
    {
        builder.ToTable("platform_fee_configs", table =>
        {
            table.HasCheckConstraint("ck_platform_fee_lifecycle", "lifecycle_status IN ('Published', 'Cancelled', 'Superseded')");
            table.HasCheckConstraint(
                "ck_platform_fee_rate",
                "\"fee_rate\" BETWEEN 0 AND 1");

            table.HasCheckConstraint(
                "ck_platform_fee_effective_period",
                "\"effective_to\" IS NULL OR " +
                "\"effective_to\" > \"effective_from\"");
        });

        builder.Property(x => x.LifecycleStatus).HasColumnName("lifecycle_status").HasMaxLength(20).HasDefaultValue("Published");
        builder.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500);
        builder.Property(x => x.ReplacesConfigId).HasColumnName("replaces_config_id");
        builder.Property(x => x.ClosedAt).HasColumnName("closed_at");
        builder.Property(x => x.ClosedBy).HasColumnName("closed_by");
        builder.Property(x => x.ClosureReason).HasColumnName("closure_reason").HasMaxLength(500);
        builder.Property(x => x.ActivationProcessedAt).HasColumnName("activation_processed_at");
        builder.HasOne<PlatformFeeConfig>().WithMany().HasForeignKey(x => x.ReplacesConfigId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ClosedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.EffectiveFrom).IsUnique().HasFilter("lifecycle_status = 'Published'")
            .HasDatabaseName("ux_platform_fee_published_start");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.FeeRate)
            .HasColumnName("fee_rate")
            .HasPrecision(7, 6)
            .IsRequired();

        builder.Property(x => x.EffectiveFrom)
            .HasColumnName("effective_from")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.EffectiveTo)
            .HasColumnName("effective_to")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.CreatedByUser)
            .WithMany(x => x.CreatedPlatformFeeConfigs)
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
