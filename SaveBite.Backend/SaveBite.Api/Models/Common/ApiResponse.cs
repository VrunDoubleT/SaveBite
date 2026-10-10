using SaveBite.Backend.Constants;

namespace SaveBite.Backend.Models.Common;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    
    public string? Message { get; init; }
    
    public string? ErrorCode { get; init; }

    public T? Data { get; init; }
    
    public Dictionary<string, string[]>? Errors { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message, string? errorCode = null)
        => new() { Success = false, Message = message, ErrorCode = errorCode };

    public static ApiResponse<T> ValidationFail(
        Dictionary<string, string[]> errors,
        string message = "Invalid Data")
        => new()
        {
            Success = false,
            Message = message,
            ErrorCode = ErrorCodes.ValidationFailed,
            Errors = errors
        };
}

public class ApiResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? ErrorCode { get; init; }
    public Dictionary<string, string[]>? Errors { get; init; }

    public static ApiResponse Ok(string? message = null)
        => new() { Success = true, Message = message };

    public static ApiResponse Fail(string message, string? errorCode = null)
        => new() { Success = false, Message = message, ErrorCode = errorCode };

    public static ApiResponse ValidationFail(
        Dictionary<string, string[]> errors,
        string message = "Invalid Data")
        => new()
        {
            Success = false,
            Message = message,
            ErrorCode = ErrorCodes.ValidationFailed,
            Errors = errors
        };
}