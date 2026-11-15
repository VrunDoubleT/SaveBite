using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Entities;
using Xunit;

namespace SaveBite.Tests;

public class ProductImageConsistencyTests
{
    [Fact]
    public void VariantImageUsesAttributeOrderRatherThanCollectionOrder()
    {
        var first = new ProductAttribute { Id = Guid.NewGuid(), Name = "Size", SortOrder = 0 };
        var second = new ProductAttribute { Id = Guid.NewGuid(), Name = "Flavor", SortOrder = 1 };
        var variant = new ProductVariant { VariantValues = [
            new() { AttributeValue = new() { Attribute = second, AttributeId = second.Id, ImageUrl = "flavor.jpg" } },
            new() { AttributeValue = new() { Attribute = first, AttributeId = first.Id, ImageUrl = "size.jpg" } }] };
        Assert.Equal("size.jpg", ProductImages.ForVariant(variant));
        variant.VariantValues = variant.VariantValues.Reverse().ToList();
        Assert.Equal("size.jpg", ProductImages.ForVariant(variant));
    }

    [Fact]
    public void ProductFallbackUsesSortOrderAndSnapshotNeverChanges()
    {
        var product = new Product { Images = [new() { Id = Guid.NewGuid(), SortOrder = 2, ImageUrl = "other.jpg" },
            new() { Id = Guid.NewGuid(), SortOrder = 0, ImageUrl = "primary.jpg" }] };
        Assert.Equal("primary.jpg", ProductImages.ForVariant(new ProductVariant(), product));
        var item = new OrderItem { SnapshotCapturedAt = DateTime.UtcNow, ProductImageUrlSnapshot = "purchased-variant.jpg" };
        Assert.Equal("purchased-variant.jpg", PurchaseSnapshots.ProductImage(item, product));
        item.ProductImageUrlSnapshot = null;
        Assert.Null(PurchaseSnapshots.ProductImage(item, product));
    }
}
