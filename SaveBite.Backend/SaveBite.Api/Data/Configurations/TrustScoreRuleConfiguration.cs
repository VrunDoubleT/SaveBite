using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class TrustScoreRuleConfiguration
    : IEntityTypeConfiguration<TrustScoreRule>
{
    public void Configure(EntityTypeBuilder<TrustScoreRule> builder)
    {
        builder.ToTable("trust_score_rules");

        builder.Property(x => x.RuleType).HasColumnName("rule_type").HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.RuleType).IsUnique();

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.Property(x => x.ScoreDelta)
            .HasColumnName("score_delta")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.CreatedByUser)
            .WithMany(x => x.CreatedTrustScoreRules)
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
