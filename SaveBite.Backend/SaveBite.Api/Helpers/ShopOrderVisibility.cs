using System.Linq.Expressions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Helpers;

// Keep unsubmitted orders outside the shop order workspace.
public static class ShopOrderVisibility
{
    // Checkout orders become visible only after reaching Pending; failed unpaid checkouts stay customer-only.
    private static readonly Expression<Func<Order, bool>> Predicate = order =>
        order.Status != OrderStatus.AwaitingPayment && (order.CheckoutQuoteId == null
            || order.StatusHistories.Any(history => history.ToStatus == OrderStatus.Pending));
    private static readonly Func<Order, bool> Matches = Predicate.Compile();

    // Filter out orders that have not yet been submitted to the shop.
    public static IQueryable<Order> Apply(IQueryable<Order> orders) => orders.Where(Predicate);
    // Check whether the shop may view this order's current submission state.
    public static bool IsVisible(Order order) => Matches(order);
}
