using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Requests;

public sealed class IssueViolationWarningRequest
{
    public string Reason { get; set; } = string.Empty;
    public string RequiredCorrection { get; set; } = string.Empty;
}

public sealed class RemoveViolatingProductRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class UpdateShopModerationStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public sealed class HideViolatingFeedbackRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class RestoreFeedbackRequest
{
    public string? Reason { get; set; }
}