using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.IntegrationTests.Infrastructure;

namespace SaveBite.IntegrationTests.Features.TrustScore;

public class TrustScoreAdminManagementIntegrationTests : BaseIntegrationTest
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public TrustScoreAdminManagementIntegrationTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    private async Task<(int MaxMinScore, int MaxCompletedOrders, decimal MaxOrderValue)> EnsureBaseLevelAndGetNextThresholdsAsync()
    {
        var existingLevels = await DbContext.TrustLevels
            .Include(l => l.Requirement)
            .OrderBy(l => l.Rank)
            .ToListAsync();

        if (existingLevels.Count == 0)
        {
            var now = DateTime.UtcNow;
            var baseLevelId = Guid.NewGuid();
            DbContext.TrustLevels.Add(new TrustLevel
            {
                Id = baseLevelId,
                Name = $"Base-{Guid.NewGuid():N}"[..16],
                Description = "Starter level",
                Rank = 1,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                Requirement = new TrustLevelRequirement
                {
                    Id = Guid.NewGuid(),
                    TrustLevelId = baseLevelId,
                    MinScore = 0,
                    MinCompletedOrders = 0,
                    MinTotalOrderValue = 0m
                },
                DepositPolicy = new TrustLevelDepositPolicy
                {
                    Id = Guid.NewGuid(),
                    TrustLevelId = baseLevelId,
                    RequireFullOnlinePayment = true,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            });
            await DbContext.SaveChangesAsync();
            return (10, 1, 100000m);
        }

        var maxScore = existingLevels.Max(l => l.Requirement?.MinScore ?? 0);
        var maxOrders = existingLevels.Max(l => l.Requirement?.MinCompletedOrders ?? 0);
        var maxValue = existingLevels.Max(l => l.Requirement?.MinTotalOrderValue ?? 0m);
        return (maxScore + 10, maxOrders + 2, maxValue + 200000m);
    }

    [Fact]
    public async Task AdminTrustEndpoints_WhenUnauthenticatedOrCustomer_Return401And403()
    {
        Client.DefaultRequestHeaders.Authorization = null;
        var unauth = await Client.PostAsJsonAsync("/api/admin/trust/levels", new CreateTrustLevelRequest());
        unauth.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        await AuthenticateAsCustomerAsync();
        var forbidden = await Client.PostAsJsonAsync("/api/admin/trust/levels", new CreateTrustLevelRequest
        {
            Name = "Unauthorized Tier",
            Requirement = new UpdateTrustLevelRequirementRequest(),
            Benefits = new UpdateTrustLevelBenefitsRequest { RequireFullOnlinePayment = true }
        });
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateLevel_WhenValidAdminRequest_CreatesLevelHistoryAndRejectsDuplicates()
    {
        await AuthenticateAsAdminAsync();
        var (minScore, minOrders, minValue) = await EnsureBaseLevelAndGetNextThresholdsAsync();
        var levelName = $"Tier-{Guid.NewGuid():N}"[..18];

        var createResponse = await Client.PostAsJsonAsync("/api/admin/trust/levels", new CreateTrustLevelRequest
        {
            Name = levelName,
            Requirement = new UpdateTrustLevelRequirementRequest
            {
                MinScore = minScore,
                MinCompletedOrders = minOrders,
                MinTotalOrderValue = minValue
            },
            Benefits = new UpdateTrustLevelBenefitsRequest
            {
                Description = "VIP integration benefits",
                RequireFullOnlinePayment = false,
                DepositFreeOrderAmount = 100000m,
                MaxDepositOrderAmount = 300000m,
                Ranges =
                {
                    new TrustLevelDepositRangeRequest { FromOrderAmount = 100000m, ToOrderAmount = 300000m, DepositAmount = 30000m }
                }
            }
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await createResponse.Content.ReadFromJsonAsync<ApiResponse<AdminTrustLevelResponse>>(JsonOptions);
        payload!.Success.Should().BeTrue();
        payload.Data!.Name.Should().Be(levelName);
        payload.Data.IsActive.Should().BeTrue();
        payload.Data.DepositPolicy!.Ranges.Should().ContainSingle();

        var persisted = await DbContext.TrustLevels.AsNoTracking()
            .Include(l => l.Requirement)
            .Include(l => l.DepositPolicy)!.ThenInclude(p => p!.Ranges)
            .SingleAsync(l => l.Id == payload.Data.Id);
        persisted.Requirement!.MinScore.Should().Be(minScore);
        persisted.DepositPolicy!.Ranges.Should().ContainSingle();

        var history = await DbContext.TrustLevelHistories.AsNoTracking()
            .SingleOrDefaultAsync(h => h.TrustLevelId == persisted.Id && h.ChangeType == TrustLevelChangeType.Created);
        history.Should().NotBeNull();

        var duplicateResponse = await Client.PostAsJsonAsync("/api/admin/trust/levels", new CreateTrustLevelRequest
        {
            Name = levelName,
            Requirement = new UpdateTrustLevelRequirementRequest
            {
                MinScore = minScore + 5,
                MinCompletedOrders = minOrders + 1,
                MinTotalOrderValue = minValue + 100000m
            },
            Benefits = new UpdateTrustLevelBenefitsRequest { RequireFullOnlinePayment = true }
        });
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateLevelInfo_WhenValidRename_UpdatesNameAndRejectsMissingOrDuplicate()
    {
        await AuthenticateAsAdminAsync();
        var (minScore, minOrders, minValue) = await EnsureBaseLevelAndGetNextThresholdsAsync();
        var initialName = $"LvlA-{Guid.NewGuid():N}"[..16];
        var secondName = $"LvlB-{Guid.NewGuid():N}"[..16];
        var renamed = $"LvlNew-{Guid.NewGuid():N}"[..16];

        var firstRes = await Client.PostAsJsonAsync("/api/admin/trust/levels", new CreateTrustLevelRequest
        {
            Name = initialName,
            Requirement = new UpdateTrustLevelRequirementRequest { MinScore = minScore, MinCompletedOrders = minOrders, MinTotalOrderValue = minValue },
            Benefits = new UpdateTrustLevelBenefitsRequest { RequireFullOnlinePayment = true }
        });
        firstRes.EnsureSuccessStatusCode();
        var firstLevel = (await firstRes.Content.ReadFromJsonAsync<ApiResponse<AdminTrustLevelResponse>>(JsonOptions))!.Data!;

        var secondRes = await Client.PostAsJsonAsync("/api/admin/trust/levels", new CreateTrustLevelRequest
        {
            Name = secondName,
            Requirement = new UpdateTrustLevelRequirementRequest { MinScore = minScore + 5, MinCompletedOrders = minOrders + 1, MinTotalOrderValue = minValue + 100000m },
            Benefits = new UpdateTrustLevelBenefitsRequest { RequireFullOnlinePayment = true }
        });
        secondRes.EnsureSuccessStatusCode();

        var updateRes = await Client.PutAsJsonAsync($"/api/admin/trust/levels/{firstLevel.Id}/info", new UpdateTrustLevelInfoRequest
        {
            Name = renamed
        });
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedPayload = await updateRes.Content.ReadFromJsonAsync<ApiResponse<AdminTrustLevelResponse>>(JsonOptions);
        updatedPayload!.Data!.Name.Should().Be(renamed);

        var persisted = await DbContext.TrustLevels.AsNoTracking().SingleAsync(l => l.Id == firstLevel.Id);
        persisted.Name.Should().Be(renamed);

        var duplicateRename = await Client.PutAsJsonAsync($"/api/admin/trust/levels/{firstLevel.Id}/info", new UpdateTrustLevelInfoRequest
        {
            Name = secondName
        });
        duplicateRename.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var notFound = await Client.PutAsJsonAsync($"/api/admin/trust/levels/{Guid.NewGuid()}/info", new UpdateTrustLevelInfoRequest
        {
            Name = "NonExistent"
        });
        notFound.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateLevelStatus_WhenDeactivatingAndReactivatingTopLevel_UpdatesDatabaseAndRejectsLowest()
    {
        await AuthenticateAsAdminAsync();
        var (minScore, minOrders, minValue) = await EnsureBaseLevelAndGetNextThresholdsAsync();
        var topLevelName = $"Top-{Guid.NewGuid():N}"[..16];

        var createRes = await Client.PostAsJsonAsync("/api/admin/trust/levels", new CreateTrustLevelRequest
        {
            Name = topLevelName,
            Requirement = new UpdateTrustLevelRequirementRequest { MinScore = minScore, MinCompletedOrders = minOrders, MinTotalOrderValue = minValue },
            Benefits = new UpdateTrustLevelBenefitsRequest { RequireFullOnlinePayment = true }
        });
        createRes.EnsureSuccessStatusCode();
        var created = (await createRes.Content.ReadFromJsonAsync<ApiResponse<AdminTrustLevelResponse>>(JsonOptions))!.Data!;

        var deactivateRes = await Client.PutAsJsonAsync($"/api/admin/trust/levels/{created.Id}/status", new UpdateTrustLevelStatusRequest
        {
            IsActive = false
        });
        deactivateRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var deactivatedPayload = await deactivateRes.Content.ReadFromJsonAsync<ApiResponse<AdminTrustLevelResponse>>(JsonOptions);
        deactivatedPayload!.Data!.IsActive.Should().BeFalse();

        var reactivateRes = await Client.PutAsJsonAsync($"/api/admin/trust/levels/{created.Id}/status", new UpdateTrustLevelStatusRequest
        {
            IsActive = true
        });
        reactivateRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var reactivatedPayload = await reactivateRes.Content.ReadFromJsonAsync<ApiResponse<AdminTrustLevelResponse>>(JsonOptions);
        reactivatedPayload!.Data!.IsActive.Should().BeTrue();

        var lowestActive = await DbContext.TrustLevels.AsNoTracking()
            .Where(l => l.IsActive)
            .OrderBy(l => l.Rank)
            .FirstAsync();

        var deactivateLowestRes = await Client.PutAsJsonAsync($"/api/admin/trust/levels/{lowestActive.Id}/status", new UpdateTrustLevelStatusRequest
        {
            IsActive = false
        });
        deactivateLowestRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateAndDeletePenaltyRule_WhenValidAndInvalid_BehavesAsExpected()
    {
        await AuthenticateAsAdminAsync();
        var existingMaxOccurrence = await DbContext.RecoveryRequirementRules.AsNoTracking()
            .Select(r => (int?)r.MinimumOccurrence)
            .MaxAsync() ?? 0;
        var occurrence = Math.Min(99, existingMaxOccurrence + 10);
        var from = DateTime.UtcNow.AddDays(5);
        var to = DateTime.UtcNow.AddDays(15);

        var pastRes = await Client.PostAsJsonAsync("/api/admin/trust/penalty-rules", new CreatePenaltyRuleRequest
        {
            MinimumOccurrence = occurrence,
            ScoreDeduction = 15,
            RequiredOnlineOrders = 2,
            RequiredOnlineOrderValue = 100000m,
            EffectiveFrom = DateTime.UtcNow.AddHours(-2)
        });
        pastRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var scheduledRes = await Client.PostAsJsonAsync("/api/admin/trust/penalty-rules", new CreatePenaltyRuleRequest
        {
            MinimumOccurrence = occurrence,
            ScoreDeduction = 20,
            RequiredOnlineOrders = 3,
            RequiredOnlineOrderValue = 150000m,
            PermanentOfflineLock = false,
            IsActive = true,
            EffectiveFrom = from,
            EffectiveTo = to
        });
        scheduledRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var scheduledRule = (await scheduledRes.Content.ReadFromJsonAsync<ApiResponse<PenaltyRuleResponse>>(JsonOptions))!.Data!;
        scheduledRule.IsCurrent.Should().BeFalse();

        var overlapRes = await Client.PostAsJsonAsync("/api/admin/trust/penalty-rules", new CreatePenaltyRuleRequest
        {
            MinimumOccurrence = occurrence,
            ScoreDeduction = 25,
            RequiredOnlineOrders = 3,
            RequiredOnlineOrderValue = 200000m,
            EffectiveFrom = from.AddDays(1),
            EffectiveTo = to.AddDays(1)
        });
        overlapRes.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var immediateRes = await Client.PostAsJsonAsync("/api/admin/trust/penalty-rules", new CreatePenaltyRuleRequest
        {
            MinimumOccurrence = occurrence,
            ScoreDeduction = 10,
            RequiredOnlineOrders = 1,
            RequiredOnlineOrderValue = 50000m,
            EffectiveFrom = null,
            EffectiveTo = from
        });
        immediateRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var immediateRule = (await immediateRes.Content.ReadFromJsonAsync<ApiResponse<PenaltyRuleResponse>>(JsonOptions))!.Data!;
        immediateRule.IsCurrent.Should().BeTrue();

        var deleteImmediateRes = await Client.DeleteAsync($"/api/admin/trust/penalty-rules/{immediateRule.Id}");
        deleteImmediateRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var deleteScheduledRes = await Client.DeleteAsync($"/api/admin/trust/penalty-rules/{scheduledRule.Id}");
        deleteScheduledRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var stillExists = await DbContext.RecoveryRequirementRules.AsNoTracking()
            .AnyAsync(r => r.Id == scheduledRule.Id);
        stillExists.Should().BeFalse();

        var deleteMissingRes = await Client.DeleteAsync($"/api/admin/trust/penalty-rules/{Guid.NewGuid()}");
        deleteMissingRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
