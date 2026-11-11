namespace SaveBite.Backend.Messaging.Events;
public sealed record InventoryChangedEvent(Guid[] FlashDealIds);
