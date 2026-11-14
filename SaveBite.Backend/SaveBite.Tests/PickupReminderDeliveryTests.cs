using SaveBite.Backend.Helpers;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using Xunit;

namespace SaveBite.Tests;

public class PickupReminderDeliveryTests
{
    private static readonly DateTime Now = new(2026, 11, 14, 10, 0, 0, DateTimeKind.Utc);
    private static Order Order() => new() { Status = OrderStatus.ReadyForPickup, HardDeadlineAt = Now.AddMinutes(5),
        Customer = new User { Email = "customer@example.com", Status = UserStatus.Active, CustomerStatus = CustomerStatus.Active } };

    [Theory]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.NoShow)]
    [InlineData(OrderStatus.Expired)]
    [InlineData(OrderStatus.Rejected)]
    public void QueuedReminderIsSuppressedAfterTerminalTransition(OrderStatus status)
    {
        var order = Order(); order.Status = status;
        Assert.False(PickupReminderPolicy.CanDeliver(order, order.HardDeadlineAt!.Value, Now, order.Customer.Email));
    }

    [Fact]
    public void EligibleReminderStopsForChangedDeadlineRecipientOrCompletedAt()
    {
        var order = Order(); var deadline = order.HardDeadlineAt!.Value;
        Assert.True(PickupReminderPolicy.CanDeliver(order, deadline, Now, order.Customer.Email));
        Assert.False(PickupReminderPolicy.CanDeliver(order, deadline, deadline, order.Customer.Email));
        Assert.False(PickupReminderPolicy.CanDeliver(order, deadline.AddMinutes(-1), Now, order.Customer.Email));
        Assert.False(PickupReminderPolicy.CanDeliver(order, deadline, Now, "other@example.com"));
        order.CompletedAt = Now;
        Assert.False(PickupReminderPolicy.CanDeliver(order, deadline, Now, order.Customer.Email));
    }

    [Fact]
    public void PreviouslySentOrSuspendedCustomerDoesNotReceiveReminder()
    {
        var order = Order(); var deadline = order.HardDeadlineAt!.Value;
        order.PickupReminderEmailSentAt = Now;
        Assert.False(PickupReminderPolicy.CanDeliver(order, deadline, Now, order.Customer.Email));
        order.PickupReminderEmailSentAt = null; order.Customer.CustomerStatus = CustomerStatus.Suspended;
        Assert.False(PickupReminderPolicy.CanDeliver(order, deadline, Now, order.Customer.Email));
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void NewAndLegacyQueuedMessagesIdentifySameOrderAndDeadline(bool legacy)
    {
        var id = Guid.NewGuid(); var deadline = Now.AddMinutes(5);
        var email = legacy ? new EmailNotificationEvent { DeliveryKey = $"pickup-email:{id}:{deadline.Ticks / 10}" }
            : new EmailNotificationEvent { BusinessKind = "pickup-reminder", BusinessSubjectId = id, ExpectedDeadline = deadline };
        Assert.True(PickupReminderPolicy.TryIdentifyEmail(email, out var actualId, out var actualDeadline));
        Assert.Equal(id, actualId); Assert.Equal(deadline, actualDeadline);
    }

    [Theory]
    [InlineData("pickup-email:bad:123")]
    [InlineData("pickup-email:11111111-1111-4111-8111-111111111111:9223372036854775807")]
    [InlineData("pickup-email:11111111-1111-4111-8111-111111111111:-1")]
    public void InvalidLegacyMessagesAreRejected(string key) => Assert.False(
        PickupReminderPolicy.TryIdentifyEmail(new EmailNotificationEvent { DeliveryKey = key }, out _, out _));
}
