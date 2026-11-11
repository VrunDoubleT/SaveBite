namespace SaveBite.Backend.Messaging.Events;
public sealed record RefundTransferredEvent(Guid RefundId, Guid OrderId, decimal TransferredAmount);
