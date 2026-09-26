using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class TrustLevelRequirementConfiguration
    : IEntityTypeConfiguration<TrustLevelRequirement>
{
    public void Configure(
        EntityTypeBuilder<TrustLevelRequirement> builder)
    {
        builder.ToTable("trust_level_requirements");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.TrustLevelId)
            .HasColumnName("trust_level_id")
            .IsRequired();

        builder.Property(x => x.MinScore)
            .HasColumnName("min_score")
            .IsRequired();

        builder.Property(x => x.MinCompletedOrders)
            .HasColumnName("min_completed_orders")
            .IsRequired();

        builder.Property(x => x.MinTotalOrderValue)
            .HasColumnName("min_total_order_value")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.HasOne(x => x.TrustLevel)
            .WithOne(x => x.Requirement)
            .HasForeignKey<TrustLevelRequirement>(x => x.TrustLevelId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.TrustLevelId)
            .IsUnique();
    }
}
