using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class ShopConfiguration : IEntityTypeConfiguration<Shop>
{
    public void Configure(EntityTypeBuilder<Shop> builder)
    {
        builder.ToTable("shops");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.OwnerUserId)
            .HasColumnName("owner_user_id")
            .IsRequired();
        
        builder.Property(x => x.ApplicationId)
            .HasColumnName("application_id")
            .IsRequired();

        builder.HasIndex(x => x.ApplicationId)
            .IsUnique();

        builder.HasOne(x => x.Application)
            .WithOne(x => x.Shop)
            .HasForeignKey<Shop>(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text");

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

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.Owner)
            .WithMany(x => x.OwnedShops)
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
