using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Middlewares;

/// <summary>
/// Bắt mọi exception chưa xử lý và trả về đúng format ApiResponse,
/// để client không bao giờ nhận HTML error page hay ProblemDetails lẫn lộn.
/// Đăng ký: builder.Services.AddExceptionHandler&lt;GlobalExceptionHandler&gt;();
///          builder.Services.AddProblemDetails();
///          app.UseExceptionHandler();
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ApiResponse response;
        HttpStatusCode status;

        switch (exception)
        {
            case AppException appEx:
                status = appEx.StatusCode;
                response = ApiResponse.Fail(appEx.Message, appEx.ErrorCode);
                _logger.LogWarning(exception,
                    "Lỗi nghiệp vụ {ErrorCode} tại {Path}",
                    appEx.ErrorCode, httpContext.Request.Path);
                break;

            case UnauthorizedAccessException:
                status = HttpStatusCode.Unauthorized;
                response = ApiResponse.Fail(
                    "Bạn cần đăng nhập để tiếp tục.", ErrorCodes.Unauthorized);
                break;

            default:
                status = HttpStatusCode.InternalServerError;
                _logger.LogError(exception,
                    "Lỗi chưa xử lý tại {Path}", httpContext.Request.Path);

                // Chỉ lộ chi tiết exception ở môi trường dev.
                response = ApiResponse.Fail(
                    _env.IsDevelopment()
                        ? exception.Message
                        : "Đã có lỗi xảy ra, vui lòng thử lại sau.",
                    ErrorCodes.InternalError);
                break;
        }

        httpContext.Response.StatusCode = (int)status;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(
            response, cancellationToken: cancellationToken);

        return true; // đã xử lý xong, không chuyển tiếp handler khác
    }
}