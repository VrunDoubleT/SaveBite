using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.IntegrationTests.Features.TrustScore;

public class TrustScoreIntegrationTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _output;

    public TrustScoreIntegrationTests(
        CustomWebApplicationFactory factory,
        ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    private async Task EnsureTrustConfigurationAsync(User? adminUser = null)
    {
        var now = DateTime.UtcNow;

        if (!await DbContext.TrustLevels.AnyAsync())
        {
            var names = new[] { "Bronze", "Silver", "Gold", "Platinum", "Diamond" };
            var minScores = new[] { 0, 80, 90, 95, 100 };
            var minOrders = new[] { 0, 3, 10, 25, 50 };
            var minValues = new[] { 0m, 100000m, 500000m, 1500000m, 3000000m };
            var freeAmounts = new[] { 0m, 10000m, 20000m, 50000m, 100000m };
            var maxAmounts = new[] { 0m, 100000m, 200000m, 500000m, 1000000m };

            for (var rank = 1; rank <= 5; rank++)
            {
                var i = rank - 1;
                var level = new TrustLevel
                {
                    Id = Guid.NewGuid(),
                    Name = names[i],
                    Rank = rank,
                    IsActive = true,
                    Description = rank == 1 ? "Cap khoi dau." : $"Cap do {rank}.",
                    CreatedAt = now.AddMonths(-6),
                    UpdatedAt = now
                };
                DbContext.TrustLevels.Add(level);

                var requirement = new TrustLevelRequirement
                {
                    Id = Guid.NewGuid(),
                    TrustLevelId = level.Id,
                    MinScore = minScores[i],
                    MinCompletedOrders = minOrders[i],
                    MinTotalOrderValue = minValues[i]
                };
                DbContext.TrustLevelRequirements.Add(requirement);

                var policy = new TrustLevelDepositPolicy
                {
                    Id = Guid.NewGuid(),
                    TrustLevelId = level.Id,
                    RequireFullOnlinePayment = rank == 1,
                    DepositFreeOrderAmount = rank == 1 ? null : freeAmounts[i],
                    MaxDepositOrderAmount = rank == 1 ? null : maxAmounts[i],
                    CreatedAt = now.AddMonths(-6),
                    UpdatedAt = now
                };
                DbContext.TrustLevelDepositPolicies.Add(policy);

                if (rank > 1)
                {
                    var bounds = new[] { freeAmounts[i], maxAmounts[i] * 0.4m, maxAmounts[i] * 0.8m, maxAmounts[i] };
                    var deposits = new[] { maxAmounts[i] * 0.05m, maxAmounts[i] * 0.2m, maxAmounts[i] * 0.3m };
                    for (var j = 0; j < 3; j++)
                    {
                        var range = new TrustLevelDepositRange
                        {
                            Id = Guid.NewGuid(),
                            DepositPolicyId = policy.Id,
                            FromOrderAmount = bounds[j],
                            ToOrderAmount = bounds[j + 1],
                            DepositAmount = deposits[j]
                        };
                        DbContext.TrustLevelDepositRanges.Add(range);
                    }
                }
            }
            await DbContext.SaveChangesAsync();
        }

        if (!await DbContext.TrustScoreRules.AnyAsync())
        {
            var adminId = adminUser?.Id ?? (await DbContext.Users.Where(u => u.Role == UserRole.Admin).Select(u => (Guid?)u.Id).FirstOrDefaultAsync());
            if (!adminId.HasValue)
            {
                var fallbackAdmin = await AuthenticateAsAdminAsync(email: $"admin_seed_{Guid.NewGuid():N}@test.com");
                adminId = fallbackAdmin.Id;
            }

            var rule1 = new TrustScoreRule
            {
                Id = Guid.NewGuid(),
                RuleType = TrustScoreRuleType.OnlineOrderCompleted,
                ScoreDelta = 5,
                EffectiveFrom = now.AddMonths(-6),
                CreatedBy = adminId.Value,
                CreatedAt = now.AddMonths(-6)
            };
            var rule2 = new TrustScoreRule
            {
                Id = Guid.NewGuid(),
                RuleType = TrustScoreRuleType.PayAtShopOrderCompleted,
                ScoreDelta = 3,
                EffectiveFrom = now.AddMonths(-6),
                CreatedBy = adminId.Value,
                CreatedAt = now.AddMonths(-6)
            };
            var rule3 = new TrustScoreRule
            {
                Id = Guid.NewGuid(),
                RuleType = TrustScoreRuleType.NoShow,
                ScoreDelta = -20,
                EffectiveFrom = now.AddMonths(-6),
                CreatedBy = adminId.Value,
                CreatedAt = now.AddMonths(-6)
            };

            DbContext.TrustScoreRules.AddRange(rule1, rule2, rule3);
            await DbContext.SaveChangesAsync();
        }

        if (!await DbContext.RecoveryRequirementRules.AnyAsync())
        {
            var recoveryRule = new RecoveryRequirementRule
            {
                Id = Guid.NewGuid(),
                MinimumOccurrence = 1,
                ScoreDeduction = 20,
                RequiredOnlineOrders = 1,
                RequiredOnlineOrderValue = 30000m,
                PermanentOfflineLock = false,
                IsActive = true,
                EffectiveFrom = now.AddMonths(-6),
                CreatedAt = now.AddMonths(-6)
            };
            DbContext.RecoveryRequirementRules.Add(recoveryRule);
            await DbContext.SaveChangesAsync();
        }
    }

    private async Task<(User customer, UserTrustScore score, UserLevelProgress progress, List<TrustLevel> levels)>
        SetupCustomerTrustScenarioAsync(
            int currentScore = 85,
            int rank = 2,
            int completedOrders = 3,
            decimal accumulatedValue = 150000m,
            bool isOfflineLocked = false)
    {
        await EnsureTrustConfigurationAsync();

        var customer = await AuthenticateAsCustomerAsync(
            email: $"trust_customer_{Guid.NewGuid():N}@test.com",
            fullName: "Trust Customer");

        var levels = await DbContext.TrustLevels
            .Include(l => l.Requirement)
            .Include(l => l.DepositPolicy)
                .ThenInclude(p => p!.Ranges)
            .OrderBy(l => l.Rank)
            .ToListAsync();

        var assignedLevel = levels.FirstOrDefault(l => l.Rank == rank) ?? levels[0];
        var now = DateTime.UtcNow;

        var score = new UserTrustScore
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            TrustLevelId = assignedLevel.Id,
            CurrentScore = currentScore,
            IsOfflineLockedPermanently = isOfflineLocked,
            CreatedAt = now.AddDays(-10),
            UpdatedAt = now
        };
        DbContext.UserTrustScores.Add(score);

        var progress = new UserLevelProgress
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            CompletedOrdersCount = completedOrders,
            AccumulatedOrderValue = accumulatedValue,
            CreatedAt = now.AddDays(-10),
            UpdatedAt = now
        };
        DbContext.UserLevelProgresses.Add(progress);

        await DbContext.SaveChangesAsync();

        return (customer, score, progress, levels);
    }

    private async Task<(User customer, NoShowPenalty penalty)> SetupPenaltyScenarioAsync()
    {
        var (customer, _, _, _) = await SetupCustomerTrustScenarioAsync();

        var owner = await AuthenticateAsStoreOwnerAsync(
            email: $"owner_penalty_{Guid.NewGuid():N}@test.com",
            fullName: "Penalty Store Owner");
        var shop = await DbContext.Shops.FirstAsync(s => s.OwnerUserId == owner.Id);

        var now = DateTime.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderCode = $"ORD{Random.Shared.Next(100000, 999999)}",
            CustomerId = customer.Id,
            ShopId = shop.Id,
            PaymentMethod = PaymentMethod.OnlinePayment,
            Status = OrderStatus.Completed,
            TotalAmount = 50000m,
            DepositAmount = 0m,
            RemainingAmount = 0m,
            RequiredOnlineAmount = 50000m,
            CreatedAt = now.AddDays(-2),
            UpdatedAt = now.AddDays(-2)
        };
        DbContext.Orders.Add(order);

        var recoveryRule = await DbContext.RecoveryRequirementRules.FirstOrDefaultAsync();
        if (recoveryRule == null)
        {
            recoveryRule = new RecoveryRequirementRule
            {
                Id = Guid.NewGuid(),
                MinimumOccurrence = 1,
                ScoreDeduction = 20,
                RequiredOnlineOrders = 1,
                RequiredOnlineOrderValue = 30000m,
                PermanentOfflineLock = false,
                IsActive = true,
                EffectiveFrom = DateTime.UtcNow.AddMonths(-1),
                CreatedAt = DateTime.UtcNow.AddMonths(-1)
            };
            DbContext.RecoveryRequirementRules.Add(recoveryRule);
        }

        var penalty = new NoShowPenalty
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            OrderId = order.Id,
            RecoveryRuleId = recoveryRule.Id,
            PenaltyYear = now.Year,
            PenaltyMonth = now.Month,
            OccurrenceNumber = 1,
            ScoreDeducted = 20,
            RequiredOnlineOrders = 1,
            RequiredOnlineOrderValue = 30000m,
            CompletedOnlineOrders = 0,
            CompletedOnlineOrderValue = 0,
            PermanentOfflineLock = false,
            Status = NoShowPenaltyStatus.Recovering,
            CreatedAt = now.AddDays(-2)
        };

        DbContext.NoShowPenalties.Add(penalty);
        await DbContext.SaveChangesAsync();

        return (customer, penalty);
    }

    [Fact]
    public async Task GetMyTrustScore_WhenCustomerHasTrustData_ReturnsOverview_200()
    {
        // Arrange
        var (customer, score, progress, levels) = await SetupCustomerTrustScenarioAsync(
            currentScore: 85,
            rank: 2,
            completedOrders: 3,
            accumulatedValue: 150000m);

        SetBearerToken(customer.Id, customer.Email, customer.Role);

        // Act
        var response = await Client.GetAsync("/api/trust-score/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TrustScoreOverviewResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.CurrentScore.Should().Be(85);
        result.Data.Standing.Should().Be("GoodStanding");
        result.Data.CompletedOrdersCount.Should().Be(3);
        result.Data.AccumulatedOrderValue.Should().Be(150000m);
        result.Data.Level.Name.Should().Be("Silver");
    }

    [Fact]
    public async Task GetMyTrustScore_WhenUserNotInitialized_ReturnsServiceUnavailable_503()
    {
        // Arrange: New customer without UserTrustScore record
        var customer = await AuthenticateAsCustomerAsync(
            email: $"uninit_customer_{Guid.NewGuid():N}@test.com",
            fullName: "Uninitialized Customer");

        SetBearerToken(customer.Id, customer.Email, customer.Role);

        // Act
        var response = await Client.GetAsync("/api/trust-score/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task GetMyLevelProgress_WhenNextLevelExists_ReturnsProgressMetrics_200()
    {
        // Arrange: Bronze level (rank 1) has Silver (rank 2) as next level
        var (customer, _, _, _) = await SetupCustomerTrustScenarioAsync(
            currentScore: 40,
            rank: 1,
            completedOrders: 1,
            accumulatedValue: 50000m);

        SetBearerToken(customer.Id, customer.Email, customer.Role);

        // Act
        var response = await Client.GetAsync("/api/trust-score/me/level-progress");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LevelProgressResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.CurrentLevel.Rank.Should().Be(1);
        result.Data.NextLevel.Should().NotBeNull();
        result.Data.NextLevel!.Rank.Should().Be(2);
        result.Data.IsHighestLevel.Should().BeFalse();
        result.Data.Metrics.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMyLevelProgress_WhenAtMaxLevel_ReturnsMaxLevelTrue_200()
    {
        // Arrange: Highest rank level (Diamond = rank 5)
        var (customer, _, _, _) = await SetupCustomerTrustScenarioAsync(
            currentScore: 100,
            rank: 5,
            completedOrders: 50,
            accumulatedValue: 3000000m);

        SetBearerToken(customer.Id, customer.Email, customer.Role);

        // Act
        var response = await Client.GetAsync("/api/trust-score/me/level-progress");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LevelProgressResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.IsHighestLevel.Should().BeTrue();
        result.Data.NextLevel.Should().BeNull();
        result.Data.OverallPercent.Should().Be(100m);
    }

    [Fact]
    public async Task GetMyHistory_WhenHistoryRecordsExist_ReturnsPagedHistory_200()
    {
        // Arrange
        var (customer, _, _, _) = await SetupCustomerTrustScenarioAsync();

        var now = DateTime.UtcNow;
        var history1 = new TrustScoreHistory
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            EventType = TrustScoreEventType.OrderCompleted,
            ScoreBefore = 80,
            ScoreDelta = 5,
            ScoreAfter = 85,
            Reason = "Completed online order",
            CreatedAt = now.AddDays(-1)
        };
        var history2 = new TrustScoreHistory
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            EventType = TrustScoreEventType.NoShow,
            ScoreBefore = 100,
            ScoreDelta = -20,
            ScoreAfter = 80,
            Reason = "No show penalty",
            CreatedAt = now.AddDays(-5)
        };

        DbContext.TrustScoreHistories.AddRange(history1, history2);
        await DbContext.SaveChangesAsync();

        SetBearerToken(customer.Id, customer.Email, customer.Role);

        // Act
        var response = await Client.GetAsync("/api/trust-score/me/history?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<TrustScoreHistoryItemResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.TotalItems.Should().BeGreaterThanOrEqualTo(2);
        result.Data.Items.Should().Contain(h => h.Id == history1.Id && h.ScoreDelta == 5);
        result.Data.Items.Should().Contain(h => h.Id == history2.Id && h.ScoreDelta == -20);
    }

    [Fact]
    public async Task GetLevels_WhenAuthenticatedCustomer_ReturnsActiveLevelsWithBenefits_200()
    {
        // Arrange
        var (customer, score, _, _) = await SetupCustomerTrustScenarioAsync(rank: 2);

        SetBearerToken(customer.Id, customer.Email, customer.Role);

        // Act
        var response = await Client.GetAsync("/api/trust-score/levels");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TrustLevelInfoResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
        result.Data.Should().Contain(l => l.IsCurrent && l.Id == score.TrustLevelId);
        result.Data.Should().Contain(l => l.Rank == 1 && l.IsUnlocked);
    }

    [Fact]
    public async Task GetMyPenalties_WhenCustomerHasPenalties_ReturnsPagedPenalties_200()
    {
        // Arrange
        var (customer, penalty) = await SetupPenaltyScenarioAsync();

        SetBearerToken(customer.Id, customer.Email, customer.Role);

        // Act
        var response = await Client.GetAsync("/api/trust-score/me/penalties?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<PenaltyItemResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.TotalItems.Should().BeGreaterThanOrEqualTo(1);
        result.Data.Items.Should().Contain(p => p.Id == penalty.Id);
    }

    [Fact]
    public async Task GetMyPenaltyRecovery_WhenOwnedPenalty_ReturnsRecoveryProgress_200()
    {
        // Arrange
        var (customer, penalty) = await SetupPenaltyScenarioAsync();

        SetBearerToken(customer.Id, customer.Email, customer.Role);

        // Act
        var response = await Client.GetAsync($"/api/trust-score/me/penalties/{penalty.Id}/recovery");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PenaltyRecoveryResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Penalty.Id.Should().Be(penalty.Id);
        result.Data.Metrics.Should().NotBeEmpty();
        result.Data.IsRecovered.Should().BeFalse();
    }

    [Fact]
    public async Task GetMyPenaltyRecovery_WhenNotOwned_ReturnsNotFound_404()
    {
        // Arrange
        var (_, penalty) = await SetupPenaltyScenarioAsync();

        var otherCustomer = await AuthenticateAsCustomerAsync(
            email: $"other_trust_cust_{Guid.NewGuid():N}@test.com",
            fullName: "Other Trust Customer");

        SetBearerToken(otherCustomer.Id, otherCustomer.Email, otherCustomer.Role);

        // Act
        var response = await Client.GetAsync($"/api/trust-score/me/penalties/{penalty.Id}/recovery");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Admin_GetLevels_WhenAdminAuthenticated_ReturnsLevelsWithUserCounts_200()
    {
        // Arrange
        var admin = await AuthenticateAsAdminAsync(
            email: $"admin_{Guid.NewGuid():N}@test.com",
            fullName: "Trust Admin");

        await EnsureTrustConfigurationAsync(admin);

        SetBearerToken(admin.Id, admin.Email, admin.Role);

        // Act
        var response = await Client.GetAsync("/api/admin/trust/levels");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<AdminTrustLevelResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
        result.Data.Should().Contain(l => l.Rank == 1 && l.Name == "Bronze");
    }

    [Fact]
    public async Task Admin_UpdateLevelBenefits_WhenAdmin_UpdatesDescriptionAndDepositPolicy_200()
    {
        // Arrange
        var admin = await AuthenticateAsAdminAsync(
            email: $"admin_{Guid.NewGuid():N}@test.com",
            fullName: "Trust Admin Benefits");

        await EnsureTrustConfigurationAsync(admin);

        var level = await DbContext.TrustLevels.FirstAsync(l => l.Rank == 2);
        var updatedDesc = $"Updated Silver Benefits {Guid.NewGuid():N}";

        var request = new UpdateTrustLevelBenefitsRequest
        {
            Description = updatedDesc,
            RequireFullOnlinePayment = true,
            DepositFreeOrderAmount = null,
            MaxDepositOrderAmount = null,
            Ranges = new List<TrustLevelDepositRangeRequest>()
        };

        SetBearerToken(admin.Id, admin.Email, admin.Role);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/admin/trust/levels/{level.Id}/benefits", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AdminTrustLevelResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Description.Should().Be(updatedDesc);
        result.Data.DepositPolicy.RequireFullOnlinePayment.Should().BeTrue();

        // Verify in DB
        var updatedInDb = await DbContext.TrustLevels
            .Include(l => l.DepositPolicy)
            .AsNoTracking()
            .SingleAsync(l => l.Id == level.Id);
        updatedInDb.Description.Should().Be(updatedDesc);
        updatedInDb.DepositPolicy!.RequireFullOnlinePayment.Should().BeTrue();
    }

    [Fact]
    public async Task Admin_UpdateLevelRequirement_WhenAdmin_UpdatesRequirements_200()
    {
        // Arrange
        var admin = await AuthenticateAsAdminAsync(
            email: $"admin_{Guid.NewGuid():N}@test.com",
            fullName: "Trust Admin Req");

        await EnsureTrustConfigurationAsync(admin);

        var level = await DbContext.TrustLevels.FirstAsync(l => l.Rank == 2);

        var request = new UpdateTrustLevelRequirementRequest
        {
            MinScore = 80,
            MinCompletedOrders = 4,
            MinTotalOrderValue = 120000m
        };

        SetBearerToken(admin.Id, admin.Email, admin.Role);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/admin/trust/levels/{level.Id}/requirements", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AdminTrustLevelResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Requirement.MinCompletedOrders.Should().Be(4);
        result.Data.Requirement.MinTotalOrderValue.Should().Be(120000m);
    }

    [Fact]
    public async Task Admin_GetScoreRules_WhenAdmin_ReturnsRulesList_200()
    {
        // Arrange
        var admin = await AuthenticateAsAdminAsync(
            email: $"admin_{Guid.NewGuid():N}@test.com",
            fullName: "Trust Admin Rules");

        await EnsureTrustConfigurationAsync(admin);

        SetBearerToken(admin.Id, admin.Email, admin.Role);

        // Act
        var response = await Client.GetAsync("/api/admin/trust/score-rules");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TrustScoreRuleResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Admin_GetPenaltyRules_WhenAdmin_ReturnsPenaltyRulesList_200()
    {
        // Arrange
        var admin = await AuthenticateAsAdminAsync(
            email: $"admin_{Guid.NewGuid():N}@test.com",
            fullName: "Trust Admin Penalty Rules");

        await EnsureTrustConfigurationAsync(admin);

        SetBearerToken(admin.Id, admin.Email, admin.Role);

        // Act
        var response = await Client.GetAsync("/api/admin/trust/penalty-rules");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<PenaltyRuleResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Admin_UpdatePenaltyRule_WhenAdmin_UpdatesPenaltyRule_200()
    {
        // Arrange
        var admin = await AuthenticateAsAdminAsync(
            email: $"admin_{Guid.NewGuid():N}@test.com",
            fullName: "Trust Admin Update Penalty");

        await EnsureTrustConfigurationAsync(admin);

        var rule = await DbContext.RecoveryRequirementRules.FirstAsync();
        var request = new UpdatePenaltyRuleRequest
        {
            MinimumOccurrence = rule.MinimumOccurrence,
            ScoreDeduction = 25,
            RequiredOnlineOrders = 2,
            RequiredOnlineOrderValue = 50000m,
            PermanentOfflineLock = false,
            IsActive = true
        };

        SetBearerToken(admin.Id, admin.Email, admin.Role);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/admin/trust/penalty-rules/{rule.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PenaltyRuleResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.ScoreDeduction.Should().Be(25);
    }

    [Fact]
    public async Task Admin_GetEarningRules_WhenAdmin_ReturnsEarningRulesList_200()
    {
        // Arrange
        var admin = await AuthenticateAsAdminAsync(
            email: $"admin_{Guid.NewGuid():N}@test.com",
            fullName: "Trust Admin Earning Rules");

        await EnsureTrustConfigurationAsync(admin);

        SetBearerToken(admin.Id, admin.Email, admin.Role);

        // Act
        var response = await Client.GetAsync("/api/admin/trust/earning-rules");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TrustScoreRuleResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Admin_UpdateEarningRule_WhenAdmin_UpdatesScoreDelta_200()
    {
        // Arrange
        var admin = await AuthenticateAsAdminAsync(
            email: $"admin_{Guid.NewGuid():N}@test.com",
            fullName: "Trust Admin Update Earning");

        await EnsureTrustConfigurationAsync(admin);

        var rule = await DbContext.TrustScoreRules.FirstAsync(r => r.ScoreDelta > 0);
        var newScoreDelta = rule.ScoreDelta == 5 ? 6 : 7;
        var request = new UpdateTrustScoreRuleRequest
        {
            ScoreDelta = newScoreDelta
        };

        SetBearerToken(admin.Id, admin.Email, admin.Role);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/admin/trust/earning-rules/{rule.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TrustScoreRuleResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.ScoreDelta.Should().Be(newScoreDelta);
    }

    [Fact]
    public async Task AdminEndpoints_WhenNonAdmin_ReturnsForbidden_403()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync(
            email: $"regular_user_{Guid.NewGuid():N}@test.com",
            fullName: "Regular User");

        SetBearerToken(customer.Id, customer.Email, customer.Role);

        // Act & Assert 1: GET /api/admin/trust/levels
        var getLevelsResponse = await Client.GetAsync("/api/admin/trust/levels");
        getLevelsResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Act & Assert 2: GET /api/admin/trust/score-rules
        var getRulesResponse = await Client.GetAsync("/api/admin/trust/score-rules");
        getRulesResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Act & Assert 3: PUT /api/admin/trust/levels/{id}/benefits
        var putBenefitsResponse = await Client.PutAsJsonAsync($"/api/admin/trust/levels/{Guid.NewGuid()}/benefits", new UpdateTrustLevelBenefitsRequest());
        putBenefitsResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TrustScoreEndpoints_WhenUnauthenticated_ReturnsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act & Assert 1: GET /api/trust-score/me
        var overviewResponse = await Client.GetAsync("/api/trust-score/me");
        overviewResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Act & Assert 2: GET /api/trust-score/levels
        var levelsResponse = await Client.GetAsync("/api/trust-score/levels");
        levelsResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Act & Assert 3: GET /api/admin/trust/levels
        var adminLevelsResponse = await Client.GetAsync("/api/admin/trust/levels");
        adminLevelsResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
