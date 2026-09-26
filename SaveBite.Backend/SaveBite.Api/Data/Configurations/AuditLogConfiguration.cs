using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs", table =>
        {
            table.HasCheckConstraint(
                "audit_logs_actor_check",
                "(\"actor_type\" = 'User' AND " +
                    "\"actor_user_id\" IS NOT NULL) OR " +
                "(\"actor_type\" = 'System' AND " +
                    "\"actor_user_id\" IS NULL)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.ActorUserId)
            .HasColumnName("actor_user_id");

        builder.Property(x => x.ActorType)
            .HasColumnName("actor_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Action)
            .HasColumnName("action")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.TargetType)
            .HasColumnName("target_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.TargetId)
            .HasColumnName("target_id")
            .IsRequired();

        builder.Property(x => x.OldValuesJson)
            .HasColumnName("old_values")
            .HasColumnType("jsonb");

        builder.Property(x => x.NewValuesJson)
            .HasColumnName("new_values")
            .HasColumnType("jsonb");

        builder.Property(x => x.Reason)
            .HasColumnName("reason")
            .HasMaxLength(1000);

        builder.Property(x => x.CorrelationId)
            .HasColumnName("correlation_id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TargetType,
            x.TargetId,
            x.CreatedAt
        });

        builder.HasIndex(x => new
        {
            x.ActorUserId,
            x.CreatedAt
        });

        builder.HasIndex(x => new
        {
            x.Action,
            x.CreatedAt
        });

        builder.HasIndex(x => x.CorrelationId);

        builder.HasOne(x => x.ActorUser)
            .WithMany(x => x.AuditLogs)
            .HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
