using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public CustomerStatus CustomerStatus { get; set; } = CustomerStatus.Active;
    public ShopAccessStatus ShopStatus { get; set; } = ShopAccessStatus.Active;
    public UserRole Role { get; set; } = UserRole.User;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public UserLevelProgress? LevelProgress { get; set; }
    public UserTrustScore? TrustScore { get; set; }

    public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
    public ICollection<AccountStatusLog> AccountStatusLogs { get; set; } = new List<AccountStatusLog>();
    public ICollection<AccountStatusLog> AdminAccountStatusLogs { get; set; } = new List<AccountStatusLog>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Order> CancelledOrders { get; set; } = new List<Order>();
    public ICollection<OrderStatusHistory> OrderStatusChanges { get; set; } = new List<OrderStatusHistory>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<NoShowPenalty> NoShowPenalties { get; set; } = new List<NoShowPenalty>();
    public ICollection<TrustScoreHistory> TrustScoreHistories { get; set; } = new List<TrustScoreHistory>();
    public ICollection<UserTrustLevelHistory> TrustLevelHistories { get; set; } = new List<UserTrustLevelHistory>();
    public ICollection<UserRoleChangeLog> RoleChangeLogs { get; set; } = new List<UserRoleChangeLog>();
    public ICollection<Shop> OwnedShops { get; set; } = new List<Shop>();
    public ICollection<ShopApplication> ShopApplications { get; set; } = new List<ShopApplication>();
    public ICollection<ShopApplicationDocument> UploadedShopApplicationDocuments { get; set; } = new List<ShopApplicationDocument>();
    public ICollection<ShopApplicationReviewLog> ReviewedShopApplications { get; set; } = new List<ShopApplicationReviewLog>();
    public ICollection<ShopStaff> ShopStaffMemberships { get; set; } = new List<ShopStaff>();
    public ICollection<StaffInvitation> ReceivedStaffInvitations { get; set; } = new List<StaffInvitation>();
    public ICollection<StaffInvitation> SentStaffInvitations { get; set; } = new List<StaffInvitation>();
    public ICollection<StaffActivityLog> StaffActivityLogs { get; set; } = new List<StaffActivityLog>();
    public ICollection<Refund> InitiatedRefunds { get; set; } = new List<Refund>();
    public ICollection<Refund> ReviewedRefunds { get; set; } = new List<Refund>();
    public ICollection<RefundEvidenceImage> UploadedRefundEvidenceImages { get; set; } = new List<RefundEvidenceImage>();
    public ICollection<PlatformFeeConfig> CreatedPlatformFeeConfigs { get; set; } = new List<PlatformFeeConfig>();
    public ICollection<TrustScoreRule> CreatedTrustScoreRules { get; set; } = new List<TrustScoreRule>();
    public ICollection<RecoveryRequirementRule> CreatedRecoveryRequirementRules { get; set; } = new List<RecoveryRequirementRule>();
    public ICollection<ProductContentRevision> SubmittedProductContentRevisions { get; set; } = new List<ProductContentRevision>();
    public ICollection<ProductContentRevision> ReviewedProductContentRevisions { get; set; } = new List<ProductContentRevision>();
    public ICollection<ProductFeedback> ProductFeedbacks { get; set; } = new List<ProductFeedback>();
    public ICollection<ProductFeedbackReply> ProductFeedbackReplies { get; set; } = new List<ProductFeedbackReply>();
    public ICollection<ProductFeedbackModerationLog> ModeratedProductFeedbacks { get; set; } = new List<ProductFeedbackModerationLog>();
}
