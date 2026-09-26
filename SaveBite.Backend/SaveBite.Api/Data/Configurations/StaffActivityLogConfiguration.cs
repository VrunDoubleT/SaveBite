using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class StaffActivityLogConfiguration
    : IEntityTypeConfiguration<StaffActivityLog>
{
    public void Configure(EntityTypeBuilder<StaffActivityLog> builder)
    {
        builder.ToTable("staff_activity_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.ShopId)
            .HasColumnName("shop_id")
            .IsRequired();

        builder.Property(x => x.ActionBy)
            .HasColumnName("action_by")
            .IsRequired();

        builder.Property(x => x.OrderId)
            .HasColumnName("order_id");

        builder.Property(x => x.Action)
            .HasColumnName("action")
            .HasConversion<string>()
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(x => x.Note)
            .HasColumnName("note")
            .HasColumnType("text");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.Shop)
            .WithMany(x => x.StaffActivityLogs)
            .HasForeignKey(x => x.ShopId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.ActionByUser)
            .WithMany(x => x.StaffActivityLogs)
            .HasForeignKey(x => x.ActionBy)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Order)
            .WithMany(x => x.StaffActivityLogs)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
