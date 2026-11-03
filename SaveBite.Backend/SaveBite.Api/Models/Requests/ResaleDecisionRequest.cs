namespace SaveBite.Backend.Models.Requests;
public sealed record ResaleItemDecision(Guid OrderItemId, int ResaleQuantity);
public sealed record ResaleDecisionRequest(IReadOnlyList<ResaleItemDecision> Items);
