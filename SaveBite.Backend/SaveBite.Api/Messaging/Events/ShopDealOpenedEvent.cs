namespace SaveBite.Backend.Messaging.Events;

public sealed record ShopDealOpenedEvent(Guid FlashDealId, Guid ShopId, DateTime OpenedAt);
