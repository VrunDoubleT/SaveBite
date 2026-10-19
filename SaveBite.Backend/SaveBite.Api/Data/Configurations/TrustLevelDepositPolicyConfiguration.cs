using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

// Configure one deposit policy per trust level and validate its payment mode.
public class TrustLevelDepositPolicyConfiguration : IEntityTypeConfiguration<TrustLevelDepositPolicy>
{
    // Map each trust level's payment mode and deposit eligibility thresholds.
    public void Configure(EntityTypeBuilder<TrustLevelDepositPolicy> builder)
    {
        builder.ToTable("trust_level_deposit_policies", table =>
            table.HasCheckConstraint("ck_deposit_policy_payment_mode",
                "(require_full_online_payment AND deposit_free_order_amount IS NULL AND max_deposit_order_amount IS NULL) OR " +
                "(NOT require_full_online_payment AND deposit_free_order_amount IS NOT NULL AND max_deposit_order_amount IS NOT NULL " +
                "AND deposit_free_order_amount >= 0 AND max_deposit_order_amount > deposit_free_order_amount)"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.TrustLevelId).HasColumnName("trust_level_id");
        builder.Property(x => x.RequireFullOnlinePayment).HasColumnName("require_full_online_payment");
        builder.Property(x => x.DepositFreeOrderAmount).HasColumnName("deposit_free_order_amount").HasPrecision(12, 2);
        builder.Property(x => x.MaxDepositOrderAmount).HasColumnName("max_deposit_order_amount").HasPrecision(12, 2);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
        builder.HasOne(x => x.TrustLevel).WithOne(x => x.DepositPolicy)
            .HasForeignKey<TrustLevelDepositPolicy>(x => x.TrustLevelId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(x => x.TrustLevelId).IsUnique();
    }
}
