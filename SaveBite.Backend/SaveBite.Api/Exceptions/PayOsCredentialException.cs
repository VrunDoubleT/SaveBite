using System.Net;
using SaveBite.Backend.Constants;
namespace SaveBite.Backend.Exceptions;

// A rejected create request cannot have produced a payable link; a bad response signature is ambiguous.
public sealed class PayOsCredentialException(bool requestRejected) : AppException(
    "The payment provider could not authenticate or verify this payment. Please retry or contact the shop.",
    ErrorCodes.PaymentCredentialsInvalid, HttpStatusCode.ServiceUnavailable)
{
    public bool RequestRejected { get; } = requestRejected;
}
