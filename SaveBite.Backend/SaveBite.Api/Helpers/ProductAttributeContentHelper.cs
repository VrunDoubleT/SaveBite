using System.Text.Json;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Helpers;

public static class ProductAttributeContentHelper
{
    public static AttributeValueContent[] Capture(Product product)
    {
        return product.Attributes.OrderBy(attribute => attribute.Id)
            .SelectMany(attribute => attribute.Values.OrderBy(value => value.Id)
                .Select(value => new AttributeValueContent(
                    attribute.Id, attribute.Name, value.Id, value.Value, value.ImageUrl)))
            .ToArray();
    }

    public static AttributeValueContent[]? Read(string? json)
    {
        return json == null ? null : JsonSerializer.Deserialize<AttributeValueContent[]>(json);
    }

    public static bool HasChanges(IEnumerable<AttributeValueContent> before, Product product)
    {
        return !before.Select(value => (value.AttributeId, value.AttributeName, value.ValueId, value.Value, value.ImageUrl))
            .SequenceEqual(Capture(product).Select(value =>
                (value.AttributeId, value.AttributeName, value.ValueId, value.Value, value.ImageUrl)));
    }
}
