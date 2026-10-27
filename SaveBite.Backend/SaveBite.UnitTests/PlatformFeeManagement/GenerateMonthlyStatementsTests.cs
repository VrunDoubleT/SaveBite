using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

using SaveBite.Backend.BackgroundJobs.Jobs;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Tests.PlatformFeeManagement;

/// <summary>
/// Unit Tests for Feature: Generate Monthly Statements
/// Service: PlatformFeeStatementService.GenerateMonthlyStatementsAsync
/// Function Code: UT-109
/// Sheet Name: UT_GenerateMonthlyStatements
/// </summary>
public class GenerateMonthlyStatementsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IPlatformFeeStatementRepository> _repositoryMock = new();
    private readonly Mock<ILogger<PlatformFeeStatementService>> _loggerMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly PlatformFeeStatementService _service;

    // Explicit GUIDs for test determinism and traceability
    public static readonly Guid ValidShopId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid OrderId1 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid OrderFeeId1 = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid FeeConfigId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid AdjustmentId1 = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid RefundId1 = Guid.Parse("66666666-6666-6666-6666-666666666666");
    public static readonly Guid ExistingStatementId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid PaidStatementId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public const int DefaultBillingYear = 2026;
    public const short DefaultBillingMonth = 10;

    public GenerateMonthlyStatementsTests(ITestOutputHelper output)
    {
        _output = output;
        _configurationMock.Setup(c => c["Checkout:PaymentProvider"]).Returns("Simulated");

        _service = new PlatformFeeStatementService(
            _repositoryMock.Object,
            TimeProvider.System,
            _loggerMock.Object,
            _configurationMock.Object,
            _httpClientFactoryMock.Object
        );
    }

    private OrderPlatformFee CreateOrderPlatformFee(Guid shopId, decimal grossAmount = 1000000m, decimal feeAmount = 100000m) => new()
    {
        Id = OrderFeeId1,
        OrderId = OrderId1,
        ShopId = shopId,
        FeeConfigId = FeeConfigId,
        GrossAmount = grossAmount,
        FeeRate = 10.0m,
        FeeAmount = feeAmount,
        CalculatedAt = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        StatementId = null
    };

    private PlatformFeeAdjustment CreateAdjustment(Guid shopId, decimal amount = -10000m) => new()
    {
        Id = AdjustmentId1,
        ShopId = shopId,
        OrderFeeId = OrderFeeId1,
        RefundId = RefundId1,
        Amount = amount,
        Reason = "Customer partial refund fee adjustment",
        CreatedAt = DateTime.UtcNow,
        StatementId = null
    };

    private PlatformFeeStatement CreateExistingStatement(Guid id, Guid shopId, PlatformFeeStatementStatus status, decimal totalFeeAmount) => new()
    {
        Id = id,
        ShopId = shopId,
        BillingYear = DefaultBillingYear,
        BillingMonth = DefaultBillingMonth,
        Status = status,
        TotalFeeAmount = totalFeeAmount,
        IssuedAt = DateTime.UtcNow.AddDays(-1),
        DueAt = status == PlatformFeeStatementStatus.Paid ? null : DateTime.UtcNow.AddDays(6),
        PaidAt = status == PlatformFeeStatementStatus.Paid ? DateTime.UtcNow.AddDays(-1) : null,
        CreatedAt = DateTime.UtcNow.AddDays(-1),
        UpdatedAt = DateTime.UtcNow.AddDays(-1)
    };

}
