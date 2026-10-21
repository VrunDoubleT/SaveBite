using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.TrustScore;

/// <summary>
/// Unit Tests for Feature: Track Penalty Recovery Progress (UC-49)
/// Service: TrustScoreService.GetPenaltyRecoveryAsync
/// Function Code: UT_TrackPenaltyRecoveryProgress
/// Sheet Name: UT_TrackPenaltyRecoveryProgress
/// Business Rules: BR-054, BR-055
/// </summary>
public class TrackPenaltyRecoveryProgressTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ITrustScoreRepository> _repositoryMock = new();
    private readonly Mock<ILogger<TrustScoreService>> _loggerMock = new();
    private readonly TrustScoreService _service;

    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _penaltyId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _nonExistentPenaltyId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public TrackPenaltyRecoveryProgressTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new TrustScoreService(
            _repositoryMock.Object,
            TimeProvider.System,
            _loggerMock.Object
        );
    }

    // =========================================================================
    // UTCID01: Theo dõi tiến độ vi phạm đang trong quá trình khắc phục (Normal - N)
    // Business Rules: BR-054, BR-055
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetPenaltyRecovery_RecoveringWithPartialOrders_ReturnsProgressMetrics_Normal()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var recoveryOrderId = Guid.NewGuid();

        var penalty = new NoShowPenalty
        {
            Id = _penaltyId,
            UserId = _userId,
            OrderId = orderId,
            Order = new Order { Id = orderId, OrderCode = "ORD-PEN-01" },
            OccurrenceNumber = 1,
            ScoreDeducted = 10,
            PermanentOfflineLock = false,
            Status = NoShowPenaltyStatus.Recovering,
            RequiredOnlineOrders = 2,
            RequiredOnlineOrderValue = 100000m,
            CompletedOnlineOrders = 1,
            CompletedOnlineOrderValue = 50000m,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            RecoveryOrders = new List<NoShowPenaltyOrderHistory>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    NoShowPenaltyId = _penaltyId,
                    OrderId = recoveryOrderId,
                    Order = new Order { Id = recoveryOrderId, OrderCode = "ORD-REC-01" },
                    OrderValue = 50000m,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            }
        };

        _repositoryMock
            .Setup(r => r.GetPenaltyWithRecoveryOrdersAsync(_userId, _penaltyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(penalty);

        // Act
        var result = await _service.GetPenaltyRecoveryAsync(_userId, _penaltyId);

        // Assert & Log
        PrintSuccess("UTCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.IsRecovered.Should().BeFalse();
        result.OverallPercent.Should().Be(50m);
        result.Metrics.Should().HaveCount(2);

        var orderMetric = result.Metrics.First(m => m.Key == "onlineOrders");
        orderMetric.Current.Should().Be(1);
        orderMetric.Required.Should().Be(2);
        orderMetric.Remaining.Should().Be(1);
        orderMetric.Percent.Should().Be(50m);

        var valueMetric = result.Metrics.First(m => m.Key == "onlineOrderValue");
        valueMetric.Current.Should().Be(50000m);
        valueMetric.Required.Should().Be(100000m);
        valueMetric.Remaining.Should().Be(50000m);
        valueMetric.Percent.Should().Be(50m);

        result.RecoveryOrders.Should().HaveCount(1);
        result.RecoveryOrders[0].OrderCode.Should().Be("ORD-REC-01");
    }

    // =========================================================================
    // UTCID02: Theo dõi tiến độ vi phạm đã hoàn thành khắc phục (Normal - N)
    // Business Rules: BR-054, BR-055
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetPenaltyRecovery_RecoveredStatus_Returns100PercentAndIsRecoveredTrue_Normal()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var penalty = new NoShowPenalty
        {
            Id = _penaltyId,
            UserId = _userId,
            OrderId = orderId,
            Order = new Order { Id = orderId, OrderCode = "ORD-PEN-02" },
            OccurrenceNumber = 1,
            ScoreDeducted = 10,
            PermanentOfflineLock = false,
            Status = NoShowPenaltyStatus.Recovered,
            RequiredOnlineOrders = 2,
            RequiredOnlineOrderValue = 100000m,
            CompletedOnlineOrders = 2,
            CompletedOnlineOrderValue = 120000m,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            RecoveredAt = DateTime.UtcNow.AddDays(-1),
            RecoveryOrders = new List<NoShowPenaltyOrderHistory>()
        };

        _repositoryMock
            .Setup(r => r.GetPenaltyWithRecoveryOrdersAsync(_userId, _penaltyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(penalty);

        // Act
        var result = await _service.GetPenaltyRecoveryAsync(_userId, _penaltyId);

        // Assert & Log
        PrintSuccess("UTCID02", result, "200 OK");
        result.Should().NotBeNull();
        result.IsRecovered.Should().BeTrue();
        result.OverallPercent.Should().Be(100m);
    }

    // =========================================================================
    // UTCID03: Vi phạm mới chưa có đơn khắc phục nào (Normal - N)
    // Business Rules: BR-054
    // =========================================================================
    [Fact]
    public async Task UTCID03_GetPenaltyRecovery_ZeroProgress_ReturnsZeroPercentAndEmptyOrders_Normal()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var penalty = new NoShowPenalty
        {
            Id = _penaltyId,
            UserId = _userId,
            OrderId = orderId,
            Order = new Order { Id = orderId, OrderCode = "ORD-PEN-03" },
            OccurrenceNumber = 1,
            ScoreDeducted = 10,
            PermanentOfflineLock = false,
            Status = NoShowPenaltyStatus.Recovering,
            RequiredOnlineOrders = 2,
            RequiredOnlineOrderValue = 100000m,
            CompletedOnlineOrders = 0,
            CompletedOnlineOrderValue = 0m,
            CreatedAt = DateTime.UtcNow,
            RecoveryOrders = new List<NoShowPenaltyOrderHistory>()
        };

        _repositoryMock
            .Setup(r => r.GetPenaltyWithRecoveryOrdersAsync(_userId, _penaltyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(penalty);

        // Act
        var result = await _service.GetPenaltyRecoveryAsync(_userId, _penaltyId);

        // Assert & Log
        PrintSuccess("UTCID03", result, "200 OK");
        result.Should().NotBeNull();
        result.IsRecovered.Should().BeFalse();
        result.OverallPercent.Should().Be(0m);
        result.RecoveryOrders.Should().BeEmpty();
    }

    // =========================================================================
    // UTCID04: Không tìm thấy vi phạm (hoặc không thuộc người dùng) -> Ném NotFoundException (Abnormal - A)
    // Business Rules: BR-054
    // =========================================================================
    [Fact]
    public async Task UTCID04_GetPenaltyRecovery_NotFound_ThrowsNotFoundException_Abnormal()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetPenaltyWithRecoveryOrdersAsync(_userId, _nonExistentPenaltyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NoShowPenalty?)null);

        // Act
        Func<Task> act = async () => await _service.GetPenaltyRecoveryAsync(_userId, _nonExistentPenaltyId);

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.Message.Should().Be("Penalty not found.");

        _output.WriteLine("================ UTCID04 ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 404 Not Found");
        _output.WriteLine($"ERROR: {exception.Which.Message}");
        _output.WriteLine("========================================");
    }

    private void PrintSuccess<T>(string tcid, T result, string status = "200 OK")
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {status}");
        _output.WriteLine("RESPONSE:");
        _output.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        _output.WriteLine("========================================");
    }
}
