using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;
namespace SaveBite.Backend.Data.Configurations;
public sealed class ProductWarningConfiguration : IEntityTypeConfiguration<ProductWarning>
{
    public void Configure(EntityTypeBuilder<ProductWarning> b)
    {
        b.ToTable("product_warnings");
        b.HasKey(x => x.Id);
        b.Property(x => x.Reason).HasMaxLength(2000);
        b.Property(x => x.RequiredCorrection).HasMaxLength(2000);
        b.Property(x => x.ProductName).HasMaxLength(200);
        b.HasOne<Product>().WithMany(p => p.Warnings).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Shop>().WithMany().HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.IssuedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.BaselineRevision).WithMany().HasForeignKey(x => x.BaselineRevisionId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.ProductId).IsUnique().HasFilter("\"ResolvedAt\" IS NULL");
        b.HasIndex(x => new { x.ResolvedAt, x.DueAt });
    }
}
