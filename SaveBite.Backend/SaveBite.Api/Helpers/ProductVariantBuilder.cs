using SaveBite.Backend.Constants;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Helpers;

public static class ProductVariantBuilder
{
    public static string CombinationKey(IEnumerable<Guid> ids)
        => string.Join(":", ids.OrderBy(x => x).Select(x => x.ToString("N")));

    public static IReadOnlyList<Guid[]> Combinations(IEnumerable<ProductAttribute> attributes)
    {
        var groups = attributes.OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => x.Values.Select(v => v.Id).ToArray()).ToArray();
        if (groups.Length is < 1 or > ProductLimits.Attributes ||
            groups.Any(x => x.Length is < 1 or > ProductLimits.ValuesPerAttribute))
            throw AppException.BadRequest("A product requires 1–3 attributes with 1–20 values each.");

        var count = groups.Aggregate(1, (total, values) => total * values.Length);
        if (count > ProductLimits.Variants)
            throw AppException.BadRequest("The product cannot exceed 100 variant combinations.");

        IEnumerable<Guid[]> combinations = new[] { Array.Empty<Guid>() };
        foreach (var values in groups)
            combinations = combinations.SelectMany(previous => values,
                (previous, value) => previous.Append(value).ToArray()).ToArray();
        return combinations.ToArray();
    }

    public static IReadOnlyList<ProductVariant> Create(
        Guid productId, IReadOnlyList<Guid[]> combinations,
        IReadOnlyList<ProductVariantPriceRequest> prices, DateTime now)
    {
        var expected = combinations.Select(CombinationKey).ToHashSet(StringComparer.Ordinal);
        var supplied = prices.Select(x => CombinationKey(x.ValueIds)).ToArray();
        if (expected.Count != combinations.Count || prices.Count != expected.Count ||
            supplied.Distinct(StringComparer.Ordinal).Count() != prices.Count ||
            !expected.SetEquals(supplied))
            throw AppException.BadRequest("Provide exactly one price entry for every new variant combination.");

        return prices.Select(price =>
        {
            var id = Guid.NewGuid();
            return new ProductVariant
            {
                Id = id, ProductId = productId, Sku = $"SB-{id:N}",
                OriginalPrice = price.OriginalPrice, DealPrice = price.DealPrice,
                IsActive = true, CreatedAt = now,
                VariantValues = price.ValueIds.Select(valueId => new ProductVariantValue
                {
                    VariantId = id, AttributeValueId = valueId
                }).ToList()
            };
        }).ToArray();
    }
}
