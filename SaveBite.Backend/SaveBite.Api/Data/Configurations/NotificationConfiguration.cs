using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class NotificationConfiguration
    : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Body)
            .HasColumnName("body")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.ActionUrl)
            .HasColumnName("action_url")
            .HasMaxLength(500);

        builder.Property(x => x.DeduplicationKey)
            .HasColumnName("deduplication_key")
            .HasMaxLength(200);

        builder.Property(x => x.ReadAt)
            .HasColumnName("read_at");

        builder.Property(x => x.DismissedAt)
            .HasColumnName("dismissed_at");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.UserId,
            x.CreatedAt
        });

        builder.HasIndex(x => x.UserId)
            .HasFilter(
                "\"read_at\" IS NULL AND \"dismissed_at\" IS NULL")
            .HasDatabaseName("ix_notifications_unread_user");

        builder.HasIndex(x => new
        {
            x.UserId,
            x.DeduplicationKey
        })
        .IsUnique()
        .HasFilter("\"deduplication_key\" IS NOT NULL");

        builder.HasOne(x => x.User)
            .WithMany(x => x.Notifications)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
