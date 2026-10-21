using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

// Configure fixed deposits for contiguous order-value intervals.
public class TrustLevelDepositRangeConfiguration : IEntityTypeConfiguration<TrustLevelDepositRange>
{
    // Map deposit intervals and enforce valid bounds and fixed deposit amounts.
    public void Configure(EntityTypeBuilder<TrustLevelDepositRange> builder)
    {
        builder.ToTable("trust_level_deposit_ranges", table =>
        {
            table.HasCheckConstraint("ck_deposit_range_bounds", "from_order_amount >= 0 AND to_order_amount > from_order_amount");
            table.HasCheckConstraint("ck_deposit_range_amount", "deposit_amount > 0 AND deposit_amount <= from_order_amount");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.DepositPolicyId).HasColumnName("deposit_policy_id");
        builder.Property(x => x.FromOrderAmount).HasColumnName("from_order_amount").HasPrecision(12, 2);
        builder.Property(x => x.ToOrderAmount).HasColumnName("to_order_amount").HasPrecision(12, 2);
        builder.Property(x => x.DepositAmount).HasColumnName("deposit_amount").HasPrecision(12, 2);
        builder.HasOne(x => x.DepositPolicy).WithMany(x => x.Ranges)
            .HasForeignKey(x => x.DepositPolicyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.DepositPolicyId, x.FromOrderAmount }).IsUnique();
    }
}
