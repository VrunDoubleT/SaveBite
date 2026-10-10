using SaveBite.Backend.Constants;
using System.Net;

namespace SaveBite.Backend.Exceptions;

public class AppException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public string ErrorCode { get; }

    public AppException(
        string message,
        string errorCode = ErrorCodes.InternalError,
        HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    public static AppException NotFound(
        string message = "The requested resource was not found.")
        => new(
            message,
            ErrorCodes.NotFound,
            HttpStatusCode.NotFound);

    public static AppException Unauthorized(
        string message = "Authentication is required.")
        => new(
            message,
            ErrorCodes.Unauthorized,
            HttpStatusCode.Unauthorized);

    public static AppException Forbidden(
        string message = "You do not have permission to perform this action.")
        => new(
            message,
            ErrorCodes.Forbidden,
            HttpStatusCode.Forbidden);

    public static AppException Conflict(
        string message = "The request conflicts with the current state.")
        => new(
            message,
            ErrorCodes.Conflict,
            HttpStatusCode.Conflict);

    public static AppException BadRequest(
        string message,
        string errorCode = ErrorCodes.ValidationFailed)
        => new(message, errorCode, HttpStatusCode.BadRequest);

    public static AppException TooManyRequests(
        string message = "Too many requests. Please try again later.")
        => new(
            message,
            ErrorCodes.TooManyRequests,
            HttpStatusCode.TooManyRequests);

    public static AppException ServiceUnavailable(
        string message = "The service is temporarily unavailable.")
        => new(
            message,
            ErrorCodes.ServiceUnavailable,
            HttpStatusCode.ServiceUnavailable);

    public static AppException FoodExpired(
        string message = "The food is no longer available for purchase.")
        => new(
            message,
            ErrorCodes.FoodExpired,
            HttpStatusCode.BadRequest);

    public static AppException OutOfStock(
        string message = "The food is out of stock.")
        => new(
            message,
            ErrorCodes.OutOfStock,
            HttpStatusCode.Conflict);

    public static AppException OrderClosed(
        string message = "The order window is closed.")
        => new(
            message,
            ErrorCodes.OrderClosed,
            HttpStatusCode.Conflict);

    public static AppException InternalError(
        string message = "An unexpected error occurred.")
        => new(
            message,
            ErrorCodes.InternalError,
            HttpStatusCode.InternalServerError);
}
