using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.DTOs;

public enum NotificationRecipientScope
{
    Users,
    OrderCustomer,
    ShopOwner,
    ShopStaff,
    ShopOperators,
    Admins,
    Customers
}

public sealed record NotificationTarget(
    NotificationRecipientScope Scope,
    IReadOnlyCollection<Guid>? UserIds = null,
    Guid? EntityId = null)
{
    // 1. Send to a specific account (Customer, Staff, StoreOwner, or Admin)
    public static NotificationTarget User(Guid userId) =>
        new(NotificationRecipientScope.Users, new[] { userId });

    // 2. Send to a list of multiple accounts
    public static NotificationTarget Users(IEnumerable<Guid> userIds) =>
        new(NotificationRecipientScope.Users, userIds.Distinct().ToArray());

    // 3. Automatically find the customer who owns the order
    public static NotificationTarget OrderCustomer(Guid orderId) =>
        new(NotificationRecipientScope.OrderCustomer, EntityId: orderId);

    // 4. Automatically find the active store owner of the shop
    public static NotificationTarget ShopOwner(Guid shopId) =>
        new(NotificationRecipientScope.ShopOwner, EntityId: shopId);

    // 5. Automatically find all active staff members of the shop
    public static NotificationTarget ShopStaff(Guid shopId) =>
        new(NotificationRecipientScope.ShopStaff, EntityId: shopId);

    // 6. Automatically find both the active store owner and active staff members of the shop
    public static NotificationTarget ShopOperators(Guid shopId) =>
        new(NotificationRecipientScope.ShopOperators, EntityId: shopId);

    // 7. Automatically find all active administrators in the system
    public static NotificationTarget Admins() =>
        new(NotificationRecipientScope.Admins);

    // 8. Automatically find all active customers in the system
    public static NotificationTarget Customers() =>
        new(NotificationRecipientScope.Customers);
}

public sealed record CreateNotificationCommand(
    NotificationType Type,
    string Title,
    string Body,
    string? ActionUrl = null,
    string? DeduplicationKey = null);
