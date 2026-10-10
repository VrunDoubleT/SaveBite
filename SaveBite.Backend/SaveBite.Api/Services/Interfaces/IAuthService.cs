using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IAuthService
{
    // Retrieve the authenticated account for session verification.
    Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);

    // Stores a pending registration in Redis and queues its verification email.
    Task RequestRegistrationAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    // Consumes the registration OTP and creates the user account.
    Task VerifyRegistrationAsync(
        VerifyRegistrationRequest request,
        CancellationToken cancellationToken = default);

    // Validates the credentials and issues a new access/refresh token pair.
    Task<TokenPairResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    // Stores the new password hash in Redis and queues a reset OTP when the account exists.
    Task RequestPasswordResetAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default);

    // Verifies the OTP, applies the password hash from Redis, and revokes sessions.
    Task ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default);

    // Changes an authenticated user's password and revokes existing sessions.
    Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default);
}
