using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class ShopPaymentConfigConfiguration
    : IEntityTypeConfiguration<ShopPaymentConfig>
{
    public void Configure(EntityTypeBuilder<ShopPaymentConfig> builder)
    {
        builder.ToTable("shop_payment_configs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(x => x.ShopId)
            .HasColumnName("shop_id")
            .IsRequired();

        builder.HasIndex(x => x.ShopId).IsUnique();

        builder.Property(x => x.BankName)
            .HasColumnName("bank_name")
            .HasMaxLength(100);

        builder.Property(x => x.BankAccountNumber)
            .HasColumnName("bank_account_number")
            .HasMaxLength(50);

        builder.Property(x => x.BankAccountHolder)
            .HasColumnName("bank_account_holder")
            .HasMaxLength(150);

        builder.Property(x => x.PayosClientId)
            .HasColumnName("payos_client_id")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.PayosApiKey)
            .HasColumnName("payos_api_key")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.PayosChecksumKey)
            .HasColumnName("payos_checksum_key")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasOne(x => x.Shop)
            .WithOne(x => x.PaymentConfig)
            .HasForeignKey<ShopPaymentConfig>(x => x.ShopId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
