using System.Text.Json;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IPlatformFeeStatementService
{
    // UC-79: View System Fees (BR-092, BR-093, BR-192, BR-193)
    Task<PagedResult<PlatformFeeStatementSummaryResponse>> GetShopStatementsAsync(
        Guid shopId,
        Guid currentUserId,
        GetShopPlatformFeesRequest request,
        CancellationToken ct = default);
    
    Task<PagedResult<PlatformFeeStatementSummaryResponse>> GetAdminStatementsAsync(
        GetAdminPlatformFeesRequest request,
        CancellationToken ct = default);

    // UC-80: View Fee Details (BR-092, BR-205)
    Task<PlatformFeeStatementDetailResponse> GetStatementDetailAsync(
        Guid statementId,
        Guid shopId,
        Guid currentUserId,
        CancellationToken ct = default);
    // Admin: View Fee Details for any shop
    Task<PlatformFeeStatementDetailResponse> GetAdminStatementDetailAsync(
        Guid statementId,
        CancellationToken ct = default);
    // UC-81: Pay Platform Fee (BR-092, BR-094, BR-097)
    Task<PlatformFeePaymentLinkResponse> CreatePaymentLinkAsync(
        Guid statementId,
        Guid shopId,
        Guid currentUserId,
        CancellationToken ct = default);

    // UC-151: Update Platform Fee Payment Status via Webhook (BR-097, BR-167, BR-195)
    Task ProcessFeeWebhookAsync(
        JsonElement body,
        CancellationToken ct = default);

    // BR-093: Accrue platform fee on completed order revenue
    Task AccrueOrderPlatformFeeAsync(
        Guid orderId,
        CancellationToken ct = default);

    // BR-095, BR-193: Automatically mark overdue statements and suspend delinquent shops
    Task<int> CheckAndSuspendOverdueShopsAsync(
        CancellationToken ct = default);

    // BR-096, BR-194, BR-195: Admin records external payment reconciliation and reactivates shop
    Task RecordExternalPaymentAsync(
        Guid adminId,
        Guid statementId,
        RecordExternalFeePaymentRequest request,
        CancellationToken ct = default);
    
    // BR-093, BR-192: Aggregate unbilled order fees into monthly fee statements
    Task<int> GenerateMonthlyStatementsAsync(
        int billingYear,
        short billingMonth,
        CancellationToken ct = default);
}
