using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

/// <summary>FE-22: Administrative Audit Log (UC-147, BR-163, BR-191).</summary>
[ApiController]
[Route("api/admin/audit-logs")]
[AdminAccess]
public sealed class AdminAuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AdminAuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    // UC-147: View Audit Log
    [HttpGet]
    public async Task<ActionResult<ApiResponse<AuditLogPageResponse>>> GetAuditLogs(
        [FromQuery] GetAuditLogsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _auditLogService.GetAuditLogsAsync(request, cancellationToken);
        return Ok(ApiResponse<AuditLogPageResponse>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AuditLogItemResponse>>> GetAuditLogById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _auditLogService.GetAuditLogByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<AuditLogItemResponse>.Ok(result));
    }
}