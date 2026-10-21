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
/// Unit Tests for Feature: View Trust Score History (UC-46)
/// Service: TrustScoreService.GetHistoryAsync & GetTrustScoreHistoryRequestValidator
/// Function Code: UT_ViewTrustScoreHistory
/// Sheet Name: UT_ViewTrustScoreHistory
/// Business Rules: BR-050, BR-052
/// </summary>
public class ViewTrustScoreHistoryTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ITrustScoreRepository> _repositoryMock = new();
    private readonly Mock<ILogger<TrustScoreService>> _loggerMock = new();
    private readonly TrustScoreService _service;
    private readonly GetTrustScoreHistoryRequestValidator _validator = new();

    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public ViewTrustScoreHistoryTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new TrustScoreService(
            _repositoryMock.Object,
            TimeProvider.System,
            _loggerMock.Object
        );
    }

    // =========================================================================
    // UTCID01: Xem lịch sử điểm uy tín thành công với đầy đủ thông tin biến động (Normal - N)
    // Business Rules: BR-050, BR-052
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetHistory_ValidUser_ReturnsPagedHistoryWithEventDetails_Normal()
    {
        // Arrange
        var request = new GetTrustScoreHistoryRequest
        {
            Page = 1,
            PageSize = 10,
            EventType = null
        };

        var orderId = Guid.NewGuid();
        var historyList = new List<TrustScoreHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                EventType = TrustScoreEventType.OrderCompleted,
                ScoreBefore = 95,
                ScoreDelta = 5,
                ScoreAfter = 100,
                Reason = "Hoàn thành đơn hàng",
                OrderId = orderId,
                Order = new Order { Id = orderId, OrderCode = "ORD-12345" },
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                EventType = TrustScoreEventType.NoShow,
                ScoreBefore = 100,
                ScoreDelta = -10,
                ScoreAfter = 90,
                Reason = "Không đến nhận hàng",
                OrderId = null,
                Order = null,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        var pagedResult = PagedResult<TrustScoreHistory>.Create(historyList, 1, 10, 2);

        _repositoryMock
            .Setup(r => r.GetHistoryPagedAsync(_userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _service.GetHistoryAsync(_userId, request);

        // Assert & Log
        PrintSuccess("UTCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Items.Should().HaveCount(2);

        var firstItem = result.Items[0];
        firstItem.EventType.Should().Be("OrderCompleted");
        firstItem.ScoreBefore.Should().Be(95);
        firstItem.ScoreDelta.Should().Be(5);
        firstItem.ScoreAfter.Should().Be(100);
        firstItem.Reason.Should().Be("Hoàn thành đơn hàng");
        firstItem.OrderCode.Should().Be("ORD-12345");

        var secondItem = result.Items[1];
        secondItem.EventType.Should().Be("NoShow");
        secondItem.ScoreBefore.Should().Be(100);
        secondItem.ScoreDelta.Should().Be(-10);
        secondItem.ScoreAfter.Should().Be(90);
    }

    // =========================================================================
    // UTCID02: Lọc lịch sử điểm uy tín theo loại sự kiện cụ thể (Normal - N)
    // Business Rules: BR-052
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetHistory_FilterByEventType_ReturnsFilteredHistory_Normal()
    {
        // Arrange
        var request = new GetTrustScoreHistoryRequest
        {
            Page = 1,
            PageSize = 10,
            EventType = TrustScoreEventType.OrderCompleted
        };

        var historyList = new List<TrustScoreHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                EventType = TrustScoreEventType.OrderCompleted,
                ScoreBefore = 95,
                ScoreDelta = 5,
                ScoreAfter = 100,
                Reason = "Hoàn thành đơn hàng",
                CreatedAt = DateTime.UtcNow
            }
        };

        var pagedResult = PagedResult<TrustScoreHistory>.Create(historyList, 1, 10, 1);

        _repositoryMock
            .Setup(r => r.GetHistoryPagedAsync(_userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _service.GetHistoryAsync(_userId, request);

        // Assert & Log
        PrintSuccess("UTCID02", result, "200 OK");
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].EventType.Should().Be("OrderCompleted");
    }

    // =========================================================================
    // UTCID03: Người dùng chưa có lịch sử biến động điểm uy tín nào (Normal - N)
    // Business Rules: BR-050, BR-052
    // =========================================================================
    [Fact]
    public async Task UTCID03_GetHistory_UserHasNoHistory_ReturnsEmptyPagedResult_Normal()
    {
        // Arrange
        var request = new GetTrustScoreHistoryRequest
        {
            Page = 1,
            PageSize = 10,
            EventType = null
        };

        var pagedResult = PagedResult<TrustScoreHistory>.Create(new List<TrustScoreHistory>(), 1, 10, 0);

        _repositoryMock
            .Setup(r => r.GetHistoryPagedAsync(_userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _service.GetHistoryAsync(_userId, request);

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
    public async Task UTCID04_GetHistory_PageZero_ValidationFails_Boundary()
    {
        // Arrange
        var request = new GetTrustScoreHistoryRequest
        {
            Page = 0,
            PageSize = 10,
            EventType = null
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintValidationError("UTCID04", validationResult.Errors.First().ErrorMessage);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Page");
    }

    // =========================================================================
    // UTCID05: Loại sự kiện không hợp lệ nằm ngoài enum -> Validator từ chối (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID05_GetHistory_InvalidEventTypeEnum_ValidationFails_Abnormal()
    {
        // Arrange
        var request = new GetTrustScoreHistoryRequest
        {
            Page = 1,
            PageSize = 10,
            EventType = (TrustScoreEventType)999
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintValidationError("UTCID05", validationResult.Errors.First().ErrorMessage);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "EventType");
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
