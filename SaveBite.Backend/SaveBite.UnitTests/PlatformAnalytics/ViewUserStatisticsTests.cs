using FluentAssertions;
using Moq;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.PlatformAnalytics;

/// <summary>
/// Unit Tests for Feature: View User Statistics
/// Service: PlatformAnalyticsService.GetUserStatisticsAsync
/// Function Code: UT-130
/// Sheet Name: UT_ViewUserStatistics
/// </summary>
public class ViewUserStatisticsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IPlatformAnalyticsRepository> _repositoryMock = new();
    private readonly PlatformAnalyticsService _service;

    public ViewUserStatisticsTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new PlatformAnalyticsService(_repositoryMock.Object, TimeProvider.System);
    }

    // =========================================================================
    // TCID01: Valid Request -> Returns User Growth and Role Breakdown (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetUserStatistics_ValidRequest_ReturnsUserMetricsAndRoles_Normal()
    {
        // Arrange
        var request = new AnalyticsPeriodRequest { Preset = "today" };
        var now = DateTime.UtcNow;

        var userCounts = new UserCountsRow(
            Total: 100,
            New: 10,
            Verified: 85,
            ActiveAccounts: 90,
            SuspendedAccounts: 10,
            ActiveCustomerProfiles: 80,
            SuspendedCustomerProfiles: 5,
            ActiveBuyers: 35,
            Roles: new List<KeyValuePair<UserRole, int>>
            {
                new(UserRole.User, 70),
                new(UserRole.StoreOwner, 20),
                new(UserRole.Staff, 8),
                new(UserRole.Admin, 2)
            },
            TrustLevels: new List<KeyValuePair<string, int>>
            {
                new("Gold", 20),
                new("Silver", 30),
                new("Bronze", 50)
            },
            UsersWithTrustLevel: 100,
            NewUserCreatedAtUtc: new List<DateTime> { now.AddHours(-2), now.AddHours(-1) }
        );

        _repositoryMock
            .Setup(r => r.GetUserCountsAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userCounts);

        // Act
        var result = await _service.GetUserStatisticsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalUsers.Should().Be(100);
        result.NewUsersInPeriod.Should().Be(10);
        result.VerifiedUsers.Should().Be(85);
        result.ActiveAccounts.Should().Be(90);
        result.SuspendedAccounts.Should().Be(10);
        result.ActiveBuyersInPeriod.Should().Be(35);
        result.RoleBreakdown.Should().NotBeEmpty();
        result.TrustLevelBreakdown.Should().HaveCount(3);

        var customerRole = result.RoleBreakdown.First(r => r.Label == UserRole.User.ToString());
        customerRole.Count.Should().Be(70);
        customerRole.Percentage.Should().Be(70.0m);

        _output.WriteLine($"[TCID01 PASSED] 200 OK - 100 total users, 10 new, 70% customers.");
    }
}
