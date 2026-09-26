using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class StaffInvitationConfiguration
    : IEntityTypeConfiguration<StaffInvitation>
{
    public void Configure(EntityTypeBuilder<StaffInvitation> builder)
    {
        builder.ToTable("staff_invitations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.ShopId)
            .HasColumnName("shop_id")
            .IsRequired();

        builder.Property(x => x.InvitedUserId)
            .HasColumnName("invited_user_id")
            .IsRequired();

        builder.Property(x => x.InvitedBy)
            .HasColumnName("invited_by")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.InvitedAt)
            .HasColumnName("invited_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.RespondedAt)
            .HasColumnName("responded_at")
            .HasColumnType("timestamp with time zone");

        builder.HasOne(x => x.Shop)
            .WithMany(x => x.StaffInvitations)
            .HasForeignKey(x => x.ShopId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.InvitedUser)
            .WithMany(x => x.ReceivedStaffInvitations)
            .HasForeignKey(x => x.InvitedUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.InvitedByUser)
            .WithMany(x => x.SentStaffInvitations)
            .HasForeignKey(x => x.InvitedBy)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
