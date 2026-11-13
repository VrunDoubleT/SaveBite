using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Helpers;

// Persist purchase scalars and item/history snapshots, never live entity graphs or credentials.
public static class CheckoutSessionSnapshot
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(info =>
        {
            if (info.Type != typeof(Order) && info.Type != typeof(OrderItem) && info.Type != typeof(OrderStatusHistory)) return;
            foreach (var property in info.Properties.ToArray())
            {
                var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                if (type.IsValueType || type == typeof(string)) continue;
                if (info.Type == typeof(Order) && property.Name is nameof(Order.Items) or nameof(Order.StatusHistories)) continue;
                info.Properties.Remove(property);
            }
        });
        return new JsonSerializerOptions { TypeInfoResolver = resolver };
    }

    public static string Serialize(Order order) => JsonSerializer.Serialize(order, Options);
    public static Order Read(string json) => JsonSerializer.Deserialize<Order>(json, Options)
        ?? throw new InvalidOperationException("Invalid checkout session snapshot.");
}
