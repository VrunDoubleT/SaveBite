using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Validators;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.TrustScore;

/// <summary>
/// Unit Tests for Feature: View Penalty History (UC-48)
/// Service: TrustScoreService.GetPenaltiesAsync & GetPenaltiesRequestValidator
/// Function Code: UT_ViewPenaltyHistory
/// Sheet Name: UT_ViewPenaltyHistory
/// Business Rules: BR-054, BR-055, BR-172
/// </summary>
public class ViewPenaltyHistoryTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ITrustScoreRepository> _repositoryMock = new();
    private readonly Mock<ILogger<TrustScoreService>> _loggerMock = new();
    private readonly TrustScoreService _service;
    private readonly GetPenaltiesRequestValidator _validator = new();

    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public ViewPenaltyHistoryTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new TrustScoreService(
            _repositoryMock.Object,
            TimeProvider.System,
            _loggerMock.Object
        );
    }

    // =========================================================================
    // UTCID01: Xem lịch sử vi phạm thành công với đầy đủ thông tin phạt và tiến độ khắc phục (Normal - N)
    // Business Rules: BR-054, BR-055, BR-172
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetPenalties_ValidUser_ReturnsPagedPenaltiesWithRecoveryInfo_Normal()
    {
        // Arrange
        var request = new GetPenaltiesRequest
        {
            Page = 1,
            PageSize = 10,
            Status = null
        };

        var orderId1 = Guid.NewGuid();
        var orderId2 = Guid.NewGuid();
        var penalties = new List<NoShowPenalty>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                OrderId = orderId1,
                Order = new Order { Id = orderId1, OrderCode = "ORD-PEN-01" },
                OccurrenceNumber = 1,
                ScoreDeducted = 10,
                PermanentOfflineLock = false,
                LockEndsAt = null,
                Status = NoShowPenaltyStatus.Recovering,
                RequiredOnlineOrders = 2,
                RequiredOnlineOrderValue = 100000m,
                CompletedOnlineOrders = 1,
                CompletedOnlineOrderValue = 50000m,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                RecoveredAt = null
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                OrderId = orderId2,
                Order = new Order { Id = orderId2, OrderCode = "ORD-PEN-02" },
                OccurrenceNumber = 2,
                ScoreDeducted = 15,
                PermanentOfflineLock = false,
                LockEndsAt = null,
                Status = NoShowPenaltyStatus.Recovered,
                RequiredOnlineOrders = 3,
                RequiredOnlineOrderValue = 200000m,
                CompletedOnlineOrders = 3,
                CompletedOnlineOrderValue = 200000m,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                RecoveredAt = DateTime.UtcNow.AddDays(-3)
            }
        };

        var pagedResult = PagedResult<NoShowPenalty>.Create(penalties, 1, 10, 2);

        _repositoryMock
            .Setup(r => r.GetPenaltiesPagedAsync(_userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _service.GetPenaltiesAsync(_userId, request);

        // Assert & Log
        PrintSuccess("UTCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Items.Should().HaveCount(2);

        var firstItem = result.Items[0];
        firstItem.OrderCode.Should().Be("ORD-PEN-01");
        firstItem.OccurrenceNumber.Should().Be(1);
        firstItem.ScoreDeducted.Should().Be(10);
        firstItem.Status.Should().Be("Recovering");
        firstItem.RequiredOnlineOrders.Should().Be(2);
        firstItem.RequiredOnlineOrderValue.Should().Be(100000m);
        firstItem.CompletedOnlineOrders.Should().Be(1);
        firstItem.CompletedOnlineOrderValue.Should().Be(50000m);
        firstItem.PermanentOfflineLock.Should().BeFalse();

        var secondItem = result.Items[1];
        secondItem.OrderCode.Should().Be("ORD-PEN-02");
        secondItem.Status.Should().Be("Recovered");
        secondItem.RecoveredAt.Should().NotBeNull();
    }

    // =========================================================================
    // UTCID02: Lọc danh sách vi phạm theo trạng thái đang khắc phục (Recovering) (Normal - N)
    // Business Rules: BR-054
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetPenalties_FilterByStatusRecovering_ReturnsFilteredPenalties_Normal()
    {
        // Arrange
        var request = new GetPenaltiesRequest
        {
            Page = 1,
            PageSize = 10,
            Status = NoShowPenaltyStatus.Recovering
        };

        var orderId = Guid.NewGuid();
        var penalties = new List<NoShowPenalty>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                OrderId = orderId,
                Order = new Order { Id = orderId, OrderCode = "ORD-PEN-01" },
                OccurrenceNumber = 1,
                ScoreDeducted = 10,
                PermanentOfflineLock = false,
                Status = NoShowPenaltyStatus.Recovering,
                RequiredOnlineOrders = 2,
                RequiredOnlineOrderValue = 100000m,
                CompletedOnlineOrders = 0,
                CompletedOnlineOrderValue = 0m,
                CreatedAt = DateTime.UtcNow
            }
        };

        var pagedResult = PagedResult<NoShowPenalty>.Create(penalties, 1, 10, 1);

        _repositoryMock
            .Setup(r => r.GetPenaltiesPagedAsync(_userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _service.GetPenaltiesAsync(_userId, request);

        // Assert & Log
        PrintSuccess("UTCID02", result, "200 OK");
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be("Recovering");
    }

    // =========================================================================
    // UTCID03: Người dùng chưa có vi phạm nào trong hệ thống (Normal - N)
    // Business Rules: BR-054
    // =========================================================================
    [Fact]
    public async Task UTCID03_GetPenalties_UserHasNoPenalties_ReturnsEmptyPagedResult_Normal()
    {
        // Arrange
        var request = new GetPenaltiesRequest
        {
            Page = 1,
            PageSize = 10,
            Status = null
        };

        var pagedResult = PagedResult<NoShowPenalty>.Create(new List<NoShowPenalty>(), 1, 10, 0);

        _repositoryMock
            .Setup(r => r.GetPenaltiesPagedAsync(_userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _service.GetPenaltiesAsync(_userId, request);

        // Assert & Log
        PrintSuccess("UTCID03", result, "200 OK");
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    // =========================================================================
    // UTCID04: Phân trang không hợp lệ (Page = 0) -> Validator từ chối (Boundary - B)
    // =========================================================================
    [Fact]
    public async Task UTCID04_GetPenalties_PageZero_ValidationFails_Boundary()
    {
        // Arrange
        var request = new GetPenaltiesRequest
        {
            Page = 0,
            PageSize = 10,
            Status = null
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintValidationError("UTCID04", validationResult.Errors.First().ErrorMessage);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Page");
    }

    // =========================================================================
    // UTCID05: Trạng thái vi phạm không hợp lệ nằm ngoài enum -> Validator từ chối (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID05_GetPenalties_InvalidStatusEnum_ValidationFails_Abnormal()
    {
        // Arrange
        var request = new GetPenaltiesRequest
        {
            Page = 1,
            PageSize = 10,
            Status = (NoShowPenaltyStatus)999
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintValidationError("UTCID05", validationResult.Errors.First().ErrorMessage);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Status");
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

    private void PrintValidationError(string tcid, string message)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 400 BadRequest (Validation Failed)");
        _output.WriteLine($"MESSAGE: {message}");
        _output.WriteLine("========================================");
    }
}
