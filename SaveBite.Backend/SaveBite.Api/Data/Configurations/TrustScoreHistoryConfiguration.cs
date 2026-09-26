using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class TrustScoreHistoryConfiguration
    : IEntityTypeConfiguration<TrustScoreHistory>
{
    public void Configure(EntityTypeBuilder<TrustScoreHistory> builder)
    {
        builder.ToTable("trust_score_history");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.OrderId)
            .HasColumnName("order_id");

        builder.Property(x => x.RuleId)
            .HasColumnName("rule_id");

        builder.Property(x => x.NoShowPenaltyId)
            .HasColumnName("no_show_penalty_id");

        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.ScoreBefore)
            .HasColumnName("score_before")
            .IsRequired();

        builder.Property(x => x.ScoreDelta)
            .HasColumnName("score_delta")
            .IsRequired();

        builder.Property(x => x.ScoreAfter)
            .HasColumnName("score_after")
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasColumnName("reason")
            .HasColumnType("text");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.TrustScoreHistories)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Order)
            .WithMany(x => x.TrustScoreHistories)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Rule)
            .WithMany(x => x.Histories)
            .HasForeignKey(x => x.RuleId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.NoShowPenalty)
            .WithMany(x => x.TrustScoreHistories)
            .HasForeignKey(x => x.NoShowPenaltyId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
