namespace SaveBite.Backend.Models.Responses;

public sealed record AdminProductSummaryResponse(int Total, int Active, int Hidden, int WithDeals, int NeedsCorrection);
