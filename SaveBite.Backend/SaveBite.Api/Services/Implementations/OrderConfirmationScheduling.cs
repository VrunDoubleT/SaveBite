using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
namespace SaveBite.Backend.Services.Implementations;

public sealed class OrderConfirmationScheduling(IShopOrderConfirmationScheduler scheduler)
{
    public void Register(Order order)
    {
        var payment = CheckoutOrderMapper.RequiredPayment(order);
        if (order.Status != OrderStatus.Pending || order.PaymentReviewRequired || payment?.Status != PaymentStatus.Succeeded) return;
        if (order.ConfirmationExpirationJobId == null)
            order.ConfirmationExpirationJobId = scheduler.Schedule(order.Id, (payment.PaidAt ?? order.CreatedAt).AddMinutes(5));
    }
}
