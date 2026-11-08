using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Uploads;

namespace SaveBite.Backend.Data;

public class AppDbContext : DbContext
{
    private readonly SaveBite.Backend.Services.Implementations.NotificationOutboxDelivery? _notificationDelivery;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AppDbContext(DbContextOptions<AppDbContext> options,
        SaveBite.Backend.Services.Implementations.NotificationOutboxDelivery? notificationDelivery = null,
        IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        _notificationDelivery = notificationDelivery;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new NotificationOutboxCommitInterceptor());
        base.OnConfiguring(optionsBuilder);
    }

    public void TrackNotificationOutbox(Guid id) => _notificationDelivery?.Track(id);
    public void ClearNotificationOutbox() => _notificationDelivery?.Clear();
    public Task FlushNotificationOutboxAsync() => _notificationDelivery?.FlushAsync() ?? Task.CompletedTask;

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var actorValue = _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? _httpContextAccessor?.HttpContext?.User.FindFirstValue("sub");
        if (Guid.TryParse(actorValue, out var actorId))
            await UploadAttachmentTracker.StageAsync(this, actorId, cancellationToken);

        foreach (var entry in ChangeTracker.Entries<FlashDeal>().Where(e => e.State is EntityState.Added or EntityState.Modified).ToArray())
            SaveBite.Backend.Services.Implementations.FlashDealRealtimeOutbox.Stage(this, entry.Entity.Id, entry.Entity.ShopId);
        foreach (var entry in ChangeTracker.Entries<OrderEventOutbox>().Where(e => e.State == EntityState.Added))
            TrackNotificationOutbox(entry.Entity.Id);
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        // Single-command SaveChanges may not create an explicit/implicit transaction.
        if (Database.CurrentTransaction == null) await FlushNotificationOutboxAsync();
        return result;
    }

    // Users.
    public DbSet<User> Users => Set<User>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<AccountStatusLog> AccountStatusLogs => Set<AccountStatusLog>();
    public DbSet<UserRoleChangeLog> UserRoleChangeLogs => Set<UserRoleChangeLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    // Products.
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
    public DbSet<ProductAttributeValue> ProductAttributeValues => Set<ProductAttributeValue>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductVariantValue> ProductVariantValues => Set<ProductVariantValue>();
    // Flash deals.
    public DbSet<FlashDeal> FlashDeals => Set<FlashDeal>();
    public DbSet<FlashDealVariant> FlashDealVariants => Set<FlashDealVariant>();
    public DbSet<FlashDealStatusHistory> FlashDealStatusHistories => Set<FlashDealStatusHistory>();
    // Cart & Orders.
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    // Trust score & levels.
    public DbSet<NoShowPenalty> NoShowPenalties => Set<NoShowPenalty>();
    public DbSet<NoShowPenaltyOrderHistory> NoShowPenaltyOrderHistories => Set<NoShowPenaltyOrderHistory>();
    public DbSet<RecoveryRequirementRule> RecoveryRequirementRules => Set<RecoveryRequirementRule>();
    public DbSet<TrustLevel> TrustLevels => Set<TrustLevel>();
    public DbSet<TrustLevelDepositPolicy> TrustLevelDepositPolicies => Set<TrustLevelDepositPolicy>();
    public DbSet<TrustLevelDepositRange> TrustLevelDepositRanges => Set<TrustLevelDepositRange>();
    public DbSet<TrustLevelRequirement> TrustLevelRequirements => Set<TrustLevelRequirement>();
    public DbSet<TrustProgressHistory> TrustProgressHistories => Set<TrustProgressHistory>();
    public DbSet<TrustScoreRule> TrustScoreRules => Set<TrustScoreRule>();
    public DbSet<UserLevelProgress> UserLevelProgresses => Set<UserLevelProgress>();
    public DbSet<UserTrustLevelHistory> UserTrustLevelHistories => Set<UserTrustLevelHistory>();
    public DbSet<UserTrustScore> UserTrustScores => Set<UserTrustScore>();
    public DbSet<TrustLevelHistory> TrustLevelHistories => Set<TrustLevelHistory>();
    // Shops.
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<ShopFollow> ShopFollows => Set<ShopFollow>();
    public DbSet<ShopApplication> ShopApplications => Set<ShopApplication>();
    public DbSet<ShopApplicationDocument> ShopApplicationDocuments => Set<ShopApplicationDocument>();
    public DbSet<ShopApplicationReviewLog> ShopApplicationReviewLogs => Set<ShopApplicationReviewLog>();
    // Staff.
    public DbSet<StaffInvitation> StaffInvitations => Set<StaffInvitation>();
    public DbSet<ShopStaff> ShopStaffMembers => Set<ShopStaff>();
    public DbSet<StaffActivityLog> StaffActivityLogs => Set<StaffActivityLog>();
    // Refunds.
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<RefundEvidenceImage> RefundEvidenceImages => Set<RefundEvidenceImage>();
    // Platform Fee.
    public DbSet<PlatformFeeConfig> PlatformFeeConfigs => Set<PlatformFeeConfig>();
    public DbSet<PlatformFeeStatement> PlatformFeeStatements => Set<PlatformFeeStatement>();
    public DbSet<OrderPlatformFee> OrderPlatformFees => Set<OrderPlatformFee>();
    public DbSet<PlatformFeeAdjustment> PlatformFeeAdjustments => Set<PlatformFeeAdjustment>();
    public DbSet<PlatformFeePayment> PlatformFeePayments => Set<PlatformFeePayment>();
    public DbSet<PlatformFeeStatementHistory> PlatformFeeStatementHistories => Set<PlatformFeeStatementHistory>();
    public DbSet<PlatformPayOsConfig> PlatformPayOsConfigs => Set<PlatformPayOsConfig>();
    public DbSet<PlatformPayOsConfigHistory> PlatformPayOsConfigHistories => Set<PlatformPayOsConfigHistory>();
    // Notifications.
    public DbSet<Notification> Notifications => Set<Notification>();
    // Feedbacks.
    public DbSet<ProductFeedback> ProductFeedbacks => Set<ProductFeedback>();
    public DbSet<ProductFeedbackImage> ProductFeedbackImages => Set<ProductFeedbackImage>();
    public DbSet<ProductFeedbackReply> ProductFeedbackReplies => Set<ProductFeedbackReply>();
    public DbSet<ProductWarning> ProductWarnings => Set<ProductWarning>();
    // Content Moderation.
    public DbSet<ProductContentRevision> ProductContentRevisions => Set<ProductContentRevision>();
    // Feedback Moderation.
    public DbSet<ProductFeedbackModerationLog> ProductFeedbackModerationLogs => Set<ProductFeedbackModerationLog>();
    // Audit Logs.
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UploadSession> UploadSessions => Set<UploadSession>();
    public DbSet<UploadAsset> UploadAssets => Set<UploadAsset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);
    }
}
