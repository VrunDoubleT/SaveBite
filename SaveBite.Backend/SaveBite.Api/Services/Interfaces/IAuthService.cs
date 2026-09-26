using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Stores a pending registration in Redis and queues its verification email.
    /// </summary>
    Task RequestRegistrationAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consumes the registration OTP and creates the user account.
    /// </summary>
    Task VerifyRegistrationAsync(
        VerifyRegistrationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the credentials and issues a new access/refresh token pair.
    /// </summary>
    Task<TokenPairResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the current account data used by the authenticated client shell.
    /// </summary>
    Task<CurrentUserResponse> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores the new password hash in Redis and queues a reset OTP when the account exists.
    /// </summary>
    Task RequestPasswordResetAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the OTP, applies the password hash from Redis, and revokes sessions.
    /// </summary>
    Task ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes an authenticated user's password and revokes existing sessions.
    /// </summary>
    Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default);
}
