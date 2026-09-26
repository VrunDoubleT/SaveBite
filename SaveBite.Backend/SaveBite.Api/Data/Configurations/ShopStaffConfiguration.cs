using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class ShopStaffConfiguration
    : IEntityTypeConfiguration<ShopStaff>
{
    public void Configure(EntityTypeBuilder<ShopStaff> builder)
    {
        builder.ToTable("shop_staff");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.ShopId)
            .HasColumnName("shop_id")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Nickname)
            .HasColumnName("nickname")
            .HasMaxLength(100);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.JoinedAt)
            .HasColumnName("joined_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => new { x.ShopId, x.UserId })
            .IsUnique()
            .HasDatabaseName("shop_staff_unique_0");

        builder.HasOne(x => x.Shop)
            .WithMany(x => x.StaffMembers)
            .HasForeignKey(x => x.ShopId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.User)
            .WithMany(x => x.ShopStaffMemberships)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
