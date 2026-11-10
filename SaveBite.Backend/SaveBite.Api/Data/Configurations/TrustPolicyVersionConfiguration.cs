using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class TrustPolicyVersionConfiguration : IEntityTypeConfiguration<TrustPolicyVersion>
{
    public void Configure(EntityTypeBuilder<TrustPolicyVersion> builder)
    {
        builder.ToTable("trust_policy_version");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.CurrentVersion)
            .HasColumnName("current_version")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Seed initial records
        builder.HasData(new TrustPolicyVersion
        {
            Id = 1,
            CurrentVersion = 1,
            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}