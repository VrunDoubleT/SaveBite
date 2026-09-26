using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class UserRoleChangeLogConfiguration
    : IEntityTypeConfiguration<UserRoleChangeLog>
{
    public void Configure(
        EntityTypeBuilder<UserRoleChangeLog> builder)
    {
        builder.ToTable("user_role_change_logs", table =>
        {
            table.HasCheckConstraint(
                "user_role_change_logs_roles_different_check",
                "\"previous_role\" <> \"new_role\"");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.PreviousRole)
            .HasColumnName("previous_role")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.NewRole)
            .HasColumnName("new_role")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasColumnName("reason")
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.UserId,
            x.CreatedAt
        });

        builder.HasOne(x => x.User)
            .WithMany(x => x.RoleChangeLogs)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
