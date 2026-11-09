using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class DealNotificationRecipientConfiguration : IEntityTypeConfiguration<DealNotificationRecipient>
{
    public void Configure(EntityTypeBuilder<DealNotificationRecipient> builder)
    {
        builder.ToTable("deal_notification_recipients");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.PublicationId)
            .HasColumnName("publication_id")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.LocationSource)
            .HasColumnName("location_source")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.LocationVersion)
            .HasColumnName("location_version")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.SentAt)
            .HasColumnName("sent_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ErrorMessage)
            .HasColumnName("error_message")
            .HasColumnType("text");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.PublicationId, x.UserId })
            .IsUnique()
            .HasDatabaseName("ux_deal_notification_recipients_pub_user");

        builder.HasIndex(x => new { x.PublicationId, x.Status })
            .HasDatabaseName("ix_deal_notification_recipients_pub_status");
    }
}
