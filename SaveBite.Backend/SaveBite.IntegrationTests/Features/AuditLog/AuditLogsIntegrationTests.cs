using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using SaveBite.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.IntegrationTests.Features.AuditLog;

/// <summary>
/// Integration tests for FE-22: Audit Log (UC-147, BR-163, BR-164).
/// Endpoints: /api/admin/audit-logs/*
/// </summary>
public class AuditLogsIntegrationTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _output;

    public AuditLogsIntegrationTests(
        CustomWebApplicationFactory factory,
        ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    [Fact]
    public async Task GetAuditLogs_AsAdmin_ReturnsPagedLogsWithFilters_200()
    {
        // Arrange (UC-147, BR-163, BR-164)
        var admin = await AuthenticateAsAdminAsync();

        var log1 = new Backend.Models.Entities.AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = admin.Id,
            ActorType = AuditActorType.User,
            Action = "SuspendAccount",
            TargetType = "User",
            TargetId = Guid.NewGuid(),
            OldValuesJson = "{\"status\":\"Active\",\"password\":\"secretPass123\",\"apiKey\":\"pk_test_123\"}",
            NewValuesJson = "{\"status\":\"Suspended\",\"password\":\"secretPass123\",\"apiKey\":\"pk_test_123\"}",
            Reason = "Violation of terms",
            CorrelationId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
        DbContext.AuditLogs.Add(log1);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/admin/audit-logs?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuditLogPageResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Page.Items.Should().NotBeEmpty();
        body.Data.FilterOptions.Actions.Should().Contain("SuspendAccount");
        body.Data.FilterOptions.TargetTypes.Should().Contain("User");

        // Verify BR-163 Secrets Redaction on list items
        var matched = body.Data.Page.Items.First(x => x.Id == log1.Id);
        matched.OldValues!["password"]?.ToString().Should().Be(AuditPayloadSanitizer.Redacted);
        matched.OldValues["apiKey"]?.ToString().Should().Be(AuditPayloadSanitizer.Redacted);
        matched.OldValues["status"]?.ToString().Should().Be("Active");

        _output.WriteLine($"[PASS] Retrieved {body.Data.Page.TotalItems} audit log(s) with secrets redacted.");
    }

    [Fact]
    public async Task GetAuditLogById_ExistingId_ReturnsSanitizedDetails_200()
    {
        // Arrange
        var admin = await AuthenticateAsAdminAsync();
        var logId = Guid.NewGuid();

        var log = new Backend.Models.Entities.AuditLog
        {
            Id = logId,
            ActorUserId = admin.Id,
            ActorType = AuditActorType.User,
            Action = "UpdateFeeConfiguration",
            TargetType = "PlatformFeeConfig",
            TargetId = Guid.NewGuid(),
            OldValuesJson = "{\"feeRate\":0.05,\"secretToken\":\"token_xyz\"}",
            NewValuesJson = "{\"feeRate\":0.08,\"secretToken\":\"token_abc\"}",
            Reason = "Annual adjustment",
            CorrelationId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
        DbContext.AuditLogs.Add(log);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/admin/audit-logs/{logId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuditLogItemResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Id.Should().Be(logId);
        body.Data.Action.Should().Be("UpdateFeeConfiguration");
        body.Data.NewValues.Should().NotBeNull();
        body.Data.NewValues!["secretToken"]?.ToString().Should().Be(AuditPayloadSanitizer.Redacted);

        _output.WriteLine($"[PASS] Audit Log details retrieved with secretToken redacted: {body.Data.Id}");
    }

    [Fact]
    public async Task GetAuditLogById_NonExistentId_Returns404NotFound()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/admin/audit-logs/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _output.WriteLine("[PASS] Non-existent audit log returned 404 NotFound as expected.");
    }

    [Fact]
    public async Task GetAuditLogs_DateRangeInverted_Returns400BadRequest()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/admin/audit-logs?fromDate=2026-10-31&toDate=2026-10-01");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _output.WriteLine("[PASS] Inverted date range returned 400 BadRequest as expected.");
    }

    [Fact]
    public async Task GetAuditLogs_AsCustomer_Returns403Forbidden()
    {
        // Arrange
        await AuthenticateAsCustomerAsync();

        // Act
        var response = await Client.GetAsync("/api/admin/audit-logs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _output.WriteLine("[PASS] Customer access to audit logs blocked with 403 Forbidden.");
    }
}
