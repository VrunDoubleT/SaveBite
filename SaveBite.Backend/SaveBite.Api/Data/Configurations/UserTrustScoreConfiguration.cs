using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class UserTrustScoreConfiguration
    : IEntityTypeConfiguration<UserTrustScore>
{
    public void Configure(EntityTypeBuilder<UserTrustScore> builder)
    {
        builder.ToTable("user_trust_scores");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.TrustLevelId)
            .HasColumnName("trust_level_id")
            .IsRequired();

        builder.Property(x => x.CurrentScore)
            .HasColumnName("current_score")
            .IsRequired();

        builder.Property(x => x.IsOfflineLockedPermanently)
            .HasColumnName("is_offline_locked_permanently")
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
            .WithOne(x => x.TrustScore)
            .HasForeignKey<UserTrustScore>(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.TrustLevel)
            .WithMany(x => x.UserTrustScores)
            .HasForeignKey(x => x.TrustLevelId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.UserId)
            .IsUnique();
    }
}
