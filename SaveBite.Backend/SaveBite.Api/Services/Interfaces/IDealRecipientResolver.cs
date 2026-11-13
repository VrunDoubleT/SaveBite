namespace SaveBite.Backend.Services.Interfaces;

public interface IDealRecipientResolver
{
    Task<int> ResolveAndMaterializeRecipientsAsync(
        Guid flashDealId,
        Guid publicationId,
        Guid shopId,
        double shopLatitude,
        double shopLongitude,
        double radiusInKm,
        CancellationToken ct = default);
}
