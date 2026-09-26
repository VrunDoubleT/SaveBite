using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    
    // Users
    public DbSet<User> Users => Set<User>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<AccountStatusLog> AccountStatusLogs => Set<AccountStatusLog>();
    public DbSet<UserRoleChangeLog>  UserRoleChangeLogs => Set<UserRoleChangeLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    // Products
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
    public DbSet<ProductAttributeValue> ProductAttributeValues => Set<ProductAttributeValue>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductVariantValue> ProductVariantValues => Set<ProductVariantValue>();
    // Flash deals
    public DbSet<FlashDeal> FlashDeals => Set<FlashDeal>();
    public DbSet<FlashDealVariant> FlashDealVariants => Set<FlashDealVariant>();
    // Cart & Orders
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    // Trust score & levels
    public DbSet<NoShowPenalty> NoShowPenalties => Set<NoShowPenalty>();
    public DbSet<NoShowPenaltyOrderHistory> NoShowPenaltyOrderHistories => Set<NoShowPenaltyOrderHistory>();
    public DbSet<RecoveryRequirementRule> RecoveryRequirementRules => Set<RecoveryRequirementRule>();
    public DbSet<TrustLevel> TrustLevels => Set<TrustLevel>();
    public DbSet<TrustLevelRequirement> TrustLevelRequirements => Set<TrustLevelRequirement>();
    public DbSet<TrustScoreHistory> TrustScoreHistories => Set<TrustScoreHistory>();
    public DbSet<TrustScoreRule> TrustScoreRules => Set<TrustScoreRule>();
    public DbSet<UserLevelProgress> UserLevelProgresses => Set<UserLevelProgress>();
    public DbSet<UserTrustLevelHistory> UserTrustLevelHistories => Set<UserTrustLevelHistory>();
    public DbSet<UserTrustScore> UserTrustScores => Set<UserTrustScore>();
    // Shops
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<ShopApplication> ShopApplications => Set<ShopApplication>();
    public DbSet<ShopApplicationDocument> ShopApplicationDocuments => Set<ShopApplicationDocument>();
    public DbSet<ShopApplicationReviewLog> ShopApplicationReviewLogs => Set<ShopApplicationReviewLog>();
    // Staff
    public DbSet<StaffInvitation> StaffInvitations => Set<StaffInvitation>();
    public DbSet<ShopStaff> ShopStaffMembers => Set<ShopStaff>();
    public DbSet<StaffActivityLog> StaffActivityLogs => Set<StaffActivityLog>();
    // Refunds
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<RefundEvidenceImage> RefundEvidenceImages => Set<RefundEvidenceImage>();
    // Platform Fee
    public DbSet<PlatformFeeConfig> PlatformFeeConfigs => Set<PlatformFeeConfig>();
    public DbSet<PlatformFeeStatement> PlatformFeeStatements => Set<PlatformFeeStatement>();
    public DbSet<OrderPlatformFee> OrderPlatformFees => Set<OrderPlatformFee>();
    public DbSet<PlatformFeeAdjustment> PlatformFeeAdjustments => Set<PlatformFeeAdjustment>();
    public DbSet<PlatformFeePayment> PlatformFeePayments => Set<PlatformFeePayment>();
    // Notifications
    public DbSet<Notification> Notifications => Set<Notification>();
    // Feedbacks
    public DbSet<ProductFeedback> ProductFeedbacks => Set<ProductFeedback>();
    public DbSet<ProductFeedbackReply> ProductFeedbackReplies => Set<ProductFeedbackReply>();
    // Content Moderation
    public DbSet<ProductContentRevision> ProductContentRevisions => Set<ProductContentRevision>();
    // Feedback Moderation
    public DbSet<ProductFeedbackModerationLog> ProductFeedbackModerationLogs => Set<ProductFeedbackModerationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);
    }
}
