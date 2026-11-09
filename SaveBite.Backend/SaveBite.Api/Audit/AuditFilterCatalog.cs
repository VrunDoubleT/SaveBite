using SaveBite.Backend.Constants;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Audit;

/// <summary>Business filter choices remain available before events occur and after archival.</summary>
public static class AuditFilterCatalog
{
    private static readonly (string TargetType, string Action)[] Known = BuildKnown();

    public static (IReadOnlyList<string> Actions, IReadOnlyList<string> TargetTypes) Build(
        IEnumerable<(string TargetType, string Action)> stored, string? targetType)
    {
        var selectedTarget = targetType?.Trim();
        var entries = Known.Concat(stored).Where(x => IsBusiness(x.TargetType, x.Action)).Distinct().ToArray();
        var actions = entries.Where(x => string.IsNullOrEmpty(selectedTarget) || x.TargetType == selectedTarget)
            .Select(x => x.Action).Distinct().Order(StringComparer.Ordinal).ToArray();
        var targets = entries.Select(x => x.TargetType).Distinct().Order(StringComparer.Ordinal).ToArray();
        return (actions, targets);
    }

    private static bool IsBusiness(string target, string action) =>
        target is not ("Database" or "RefreshToken" or "CartItem") &&
        action is not (AuditActionTypes.SeedV2Completed or AuditActionTypes.RefreshTokenRotated
            or AuditActionTypes.UserRevokeRefreshTokens or AuditActionTypes.RevokeRefreshTokens);

    private static (string TargetType, string Action)[] BuildKnown()
    {
        var entries = new List<(string TargetType, string Action)>();
        void Raw(string target, params string[] actions) => entries.AddRange(actions.Select(action => (target, action)));
        void Operations(string target, params string[] operations) =>
            Raw(target, operations.Select(operation => AuditActionTypes.Format(target, operation)).ToArray());
        void Statuses<T>(string target) where T : struct, Enum =>
            Operations(target, Enum.GetNames<T>().Select(status => "STATUS_" + status).ToArray());

        // Keep automatic roots in sync with the interceptor policy, including enum status transitions.
        foreach (var type in AuditPolicy.RootTypes)
        {
            Operations(type.Name, AuditActionTypes.Create, AuditActionTypes.Update, AuditActionTypes.Delete);
            var status = type.GetProperty("Status")?.PropertyType;
            if (status?.IsEnum == true)
                Operations(type.Name, Enum.GetNames(status).Select(value => "STATUS_" + value).ToArray());
        }

        Operations(nameof(Order), "CREATED");
        Statuses<OrderStatus>(nameof(Order));
        Operations(nameof(FlashDeal), "CREATED");
        Statuses<FlashDealStatus>(nameof(FlashDeal));
        Operations(nameof(ShopApplication), Enum.GetNames<ShopApplicationStatus>());
        Operations(nameof(Refund), "Requested", "DestinationProvided", "DestinationConfirmed", "Withdrawn",
            "Approved", "Rejected", "Transferred", "DeadlineExpired");
        Operations(nameof(Payment), "VERIFIED", "VERIFIED_REVIEW_REQUIRED", "Pending", "Failed", "Cancelled", "Expired");
        Operations(nameof(User), "ROLE_CHANGED");
        Operations("UserTrustScore", Enum.GetNames<TrustScoreEventType>());
        Operations("UserTrustLevel", "LEVEL_CHANGED");
        Operations("UserAddressBook", "DEFAULT_ADDRESS_CHANGED");
        Operations("ProductWarning", "DEADLINE_BLOCKED");
        Operations("AuditLogArchive", "CREATED", "TEST_CREATED", "TEST_DELETED", "DOWNLOADED");
        Raw("EmailDelivery", AuditActionTypes.ResolveEmailDelivery);
        Raw("Category", AuditActionTypes.CategoryCreated, AuditActionTypes.CategoryUpdated,
            AuditActionTypes.CategoryActivated, AuditActionTypes.CategoryDeactivated, AuditActionTypes.CategoryDeleted);
        Raw("Shop", AuditActionTypes.ShopStatusUpdated);
        Raw("ShopApplication", AuditActionTypes.ShopApplicationReviewed);
        Raw("ShopPaymentConfig", "SHOP_PAYMENT_CONFIG_UPDATED");
        Raw("Product", AuditActionTypes.ProductRemovedViolation, AuditActionTypes.ProductWarningIssued);
        Raw("ProductRevision", AuditActionTypes.ProductContentApproved);
        Raw("ProductFeedback", AuditActionTypes.FeedbackHidden, AuditActionTypes.FeedbackRestored);
        Raw("User", AuditActionTypes.SuspendAccount, AuditActionTypes.ReactivateAccount,
            AuditActionTypes.SuspendCustomerFeatures, AuditActionTypes.ReactivateCustomerFeatures);
        Raw("PlatformPayOsConfig", AuditActionTypes.PlatformPayOsConfigUpdated);
        Raw("PlatformFeeConfig", AuditActionTypes.CreateScheduledFeeConfiguration, AuditActionTypes.UpdateScheduledFeeConfiguration,
            AuditActionTypes.UpdateFeeConfiguration, AuditActionTypes.CancelScheduledFeeConfiguration);
        Raw("PlatformFeeStatement", AuditActionTypes.RecordExternalPlatformFeePayment);
        Raw("PlatformFeeOverdueProcessingRun", AuditActionTypes.ProcessOverduePlatformFees);
        Raw("PlatformFeeStatementGenerationRun", AuditActionTypes.GenerateMonthlyPlatformFeeStatements);
        Raw("TrustLevel", AuditActionTypes.CreateTrustLevel, AuditActionTypes.UpdateTrustLevelInfo,
            AuditActionTypes.DeactivateTrustLevel, AuditActionTypes.ReactivateTrustLevel,
            AuditActionTypes.UpdateTrustLevelBenefits, AuditActionTypes.UpdateTrustLevelRequirement);
        Raw("RecoveryRequirementRule", AuditActionTypes.CreatePenaltyRule, AuditActionTypes.UpdatePenaltyRule, AuditActionTypes.DeletePenaltyRule);
        Raw("TrustScoreRule", AuditActionTypes.UpdateTrustScoreRule, AuditActionTypes.UpdateTrustEarningRule);
        return entries.ToArray();
    }
}
