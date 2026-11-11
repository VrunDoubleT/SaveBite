using System.Text.Json;
using SaveBite.Backend.Exceptions;
namespace SaveBite.Backend.Helpers;

public static class PlatformFeePaymentQr
{
    // Called only after the PayOS response signature has been verified with the platform checksum key.
    public static string RequireProviderQr(JsonElement data)
    {
        if (!data.TryGetProperty("qrCode", out var qr) || qr.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(qr.GetString()))
            throw AppException.ServiceUnavailable("PayOS did not return a payment QR. Please try creating the payment again.");
        return qr.GetString()!;
    }
}
