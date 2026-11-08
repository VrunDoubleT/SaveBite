using System.Text.RegularExpressions;

namespace SaveBite.Backend.Constants;

/// <summary>
/// Centralized constants for all audit log action types recorded across the platform.
/// Standardized in UPPER_SNAKE_CASE convention.
/// </summary>
public static class AuditActionTypes
{
    // ==========================================
    // 1. Generic Entity Operations (EF Core Interceptor & Repositories)
    // Pattern: $"{TARGET_TYPE}_{OPERATION}" (e.g., "PRODUCT_UPDATE", "SHOP_CREATE")
    // ==========================================
    public const string Create = "CREATE";
    public const string Update = "UPDATE";
    public const string Delete = "DELETE";

    /// <summary>
    /// Composes a standardized action string from target type name and operation name.
    /// Example: Format("FlashDeal", "UPDATE") => "FLASH_DEAL_UPDATE"
    /// </summary>
    public static string Format(string targetType, string operation)
    {
        var opSnake = ToUpperSnakeCase(operation);
        if (string.IsNullOrWhiteSpace(targetType)) return opSnake;

        var typeSnake = ToUpperSnakeCase(targetType);
        if (opSnake.StartsWith(typeSnake + "_", StringComparison.OrdinalIgnoreCase))
        {
            return opSnake;
        }

        return $"{typeSnake}_{opSnake}";
    }

    // Pre-composed common entity actions
    public const string ShopCreate = "SHOP_CREATE";
    public const string ShopUpdate = "SHOP_UPDATE";
    public const string ShopDelete = "SHOP_DELETE";

    public const string ProductCreate = "PRODUCT_CREATE";
    public const string ProductUpdate = "PRODUCT_UPDATE";
    public const string ProductDelete = "PRODUCT_DELETE";

    public const string FlashDealCreate = "FLASH_DEAL_CREATE";
    public const string FlashDealUpdate = "FLASH_DEAL_UPDATE";
    public const string FlashDealDelete = "FLASH_DEAL_DELETE";

    public const string FlashDealStatusHistoryCreate = "FLASH_DEAL_STATUS_HISTORY_CREATE";

    public const string CartItemCreate = "CART_ITEM_CREATE";
    public const string CartItemDelete = "CART_ITEM_DELETE";

    public const string RefreshTokenCreate = "REFRESH_TOKEN_CREATE";
    public const string RefreshTokenUpdate = "REFRESH_TOKEN_UPDATE";

    public const string ProductFeedbackCreate = "PRODUCT_FEEDBACK_CREATE";
    public const string ProductFeedbackUpdate = "PRODUCT_FEEDBACK_UPDATE";

    public const string UserRevokeRefreshTokens = "USER_REVOKE_REFRESH_TOKENS";
    public const string RevokeRefreshTokens = "REVOKE_REFRESH_TOKENS";
    public const string StaffInvitationUpdate = "STAFF_INVITATION_UPDATE";

    // ==========================================
    // 2. Category Operations
    // ==========================================
    public const string CategoryCreated = "CATEGORY_CREATED";
    public const string CategoryUpdated = "CATEGORY_UPDATED";
    public const string CategoryActivated = "CATEGORY_ACTIVATED";
    public const string CategoryDeactivated = "CATEGORY_DEACTIVATED";
    public const string CategoryDeleted = "CATEGORY_DELETED";

    // ==========================================
    // 3. Shop & Shop Application Operations
    // ==========================================
    public const string ShopStatusUpdated = "SHOP_STATUS_UPDATED";
    public const string ShopApplicationReviewed = "SHOP_APPLICATION_REVIEWED";

    // ==========================================
    // 4. Product, Content Moderation & Warnings
    // ==========================================
    public const string ProductContentApproved = "PRODUCT_CONTENT_APPROVED";
    public const string ProductRemovedViolation = "PRODUCT_REMOVED_VIOLATION";
    public const string ProductWarningIssued = "PRODUCT_WARNING_ISSUED";
    public const string ShopWarningIssued = "SHOP_WARNING_ISSUED";

    // ==========================================
    // 5. Feedback Moderation
    // ==========================================
    public const string FeedbackHidden = "FEEDBACK_HIDDEN";
    public const string FeedbackRestored = "FEEDBACK_RESTORED";

    // ==========================================
    // 6. User & Customer Access Management
    // ==========================================
    public const string SuspendCustomerFeatures = "SUSPEND_CUSTOMER_FEATURES";
    public const string ReactivateCustomerFeatures = "REACTIVATE_CUSTOMER_FEATURES";
    public const string SuspendAccount = "SUSPEND_ACCOUNT";
    public const string ReactivateAccount = "REACTIVATE_ACCOUNT";
    public const string SuspendCustomerAccess = "SUSPEND_CUSTOMER_ACCESS";

    // ==========================================
    // 7. Platform Fee Configuration & Billing Runs
    // ==========================================
    public const string UpdateScheduledFeeConfiguration = "UPDATE_SCHEDULED_FEE_CONFIGURATION";
    public const string CreateScheduledFeeConfiguration = "CREATE_SCHEDULED_FEE_CONFIGURATION";
    public const string UpdateFeeConfiguration = "UPDATE_FEE_CONFIGURATION";
    public const string CancelScheduledFeeConfiguration = "CANCEL_SCHEDULED_FEE_CONFIGURATION";
    public const string RecordExternalPlatformFeePayment = "RECORD_EXTERNAL_PLATFORM_FEE_PAYMENT";
    public const string ProcessOverduePlatformFees = "PROCESS_OVERDUE_PLATFORM_FEES";
    public const string GenerateMonthlyPlatformFeeStatements = "GENERATE_MONTHLY_PLATFORM_FEE_STATEMENTS";

    // ==========================================
    // 8. Platform PayOS Configuration
    // ==========================================
    public const string PlatformPayOsConfigUpdated = "PLATFORM_PAYOS_CONFIG_UPDATED";

    // ==========================================
    // 9. Trust Level & Trust Score Configuration
    // ==========================================
    public const string CreateTrustLevel = "CREATE_TRUST_LEVEL";
    public const string UpdateTrustLevelInfo = "UPDATE_TRUST_LEVEL_INFO";
    public const string DeactivateTrustLevel = "DEACTIVATE_TRUST_LEVEL";
    public const string ReactivateTrustLevel = "REACTIVATE_TRUST_LEVEL";
    public const string UpdateTrustLevelBenefits = "UPDATE_TRUST_LEVEL_BENEFITS";
    public const string UpdateTrustLevelRequirement = "UPDATE_TRUST_LEVEL_REQUIREMENT";
    public const string CreatePenaltyRule = "CREATE_PENALTY_RULE";
    public const string UpdatePenaltyRule = "UPDATE_PENALTY_RULE";
    public const string DeletePenaltyRule = "DELETE_PENALTY_RULE";
    public const string UpdateTrustScoreRule = "UPDATE_TRUST_SCORE_RULE";
    public const string UpdateTrustEarningRule = "UPDATE_TRUST_EARNING_RULE";

    // ==========================================
    // 10. Technical, Email & Seeder Milestones
    // ==========================================
    public const string ResolveEmailDelivery = "RESOLVE_EMAIL_DELIVERY";
    public const string RefreshTokenRotated = "REFRESH_TOKEN_ROTATED";
    public const string SeedV2Completed = "SEED_V2_COMPLETED";

    /// <summary>
    /// Converts a PascalCase or dotted string into UPPER_SNAKE_CASE.
    /// Examples: "FlashDeal" => "FLASH_DEAL", "Product.Update" => "PRODUCT_UPDATE"
    /// </summary>
    private static string ToUpperSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var sanitized = input.Replace('.', '_').Replace('-', '_').Replace(' ', '_');
        var snake = Regex.Replace(sanitized, "([a-z0-9])([A-Z])", "$1_$2");
        return Regex.Replace(snake, "_+", "_").Trim('_').ToUpperInvariant();
    }
}
