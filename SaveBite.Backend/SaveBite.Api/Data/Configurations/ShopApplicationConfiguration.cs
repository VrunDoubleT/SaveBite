using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class ShopApplicationConfiguration
    : IEntityTypeConfiguration<ShopApplication>
{
    public void Configure(EntityTypeBuilder<ShopApplication> builder)
    {
        builder.ToTable("shop_applications", table =>
        {
            table.HasCheckConstraint(
                "ck_shop_application_revision",
                "\"revision_number\" > 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.ApplicantUserId)
            .HasColumnName("applicant_user_id")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(x => x.BusinessLicenseNo)
            .HasColumnName("business_license_no")
            .HasMaxLength(100);

        builder.Property(x => x.AddressLine)
            .HasColumnName("address_line")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.Ward)
            .HasColumnName("ward")
            .HasMaxLength(100);

        builder.Property(x => x.District)
            .HasColumnName("district")
            .HasMaxLength(100);

        builder.Property(x => x.City)
            .HasColumnName("city")
            .HasMaxLength(100);

        builder.Property(x => x.Latitude)
            .HasColumnName("latitude")
            .HasColumnType("double precision")
            .IsRequired();

        builder.Property(x => x.Longitude)
            .HasColumnName("longitude")
            .HasColumnType("double precision")
            .IsRequired();

        builder.Property(x => x.LogoUrl)
            .HasColumnName("logo_url")
            .HasColumnType("text");

        builder.Property(x => x.CoverImageUrl)
            .HasColumnName("cover_image_url")
            .HasColumnType("text");

        builder.Property(x => x.OpeningTime)
            .HasColumnName("opening_time")
            .HasColumnType("time without time zone");

        builder.Property(x => x.ClosingTime)
            .HasColumnName("closing_time")
            .HasColumnType("time without time zone");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        
        builder.Property(x => x.BankName)
            .HasColumnName("bank_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.BankAccountNumber)
            .HasColumnName("bank_account_number")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.BankAccountHolder)
            .HasColumnName("bank_account_holder")
            .HasMaxLength(150)
            .IsRequired();

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

        builder.Property(x => x.RevisionNumber)
            .HasColumnName("revision_number")
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.ApplicantUser)
            .WithMany(x => x.ShopApplications)
            .HasForeignKey(x => x.ApplicantUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
