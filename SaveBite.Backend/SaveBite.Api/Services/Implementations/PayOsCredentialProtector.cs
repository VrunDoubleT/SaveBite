using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;
namespace SaveBite.Backend.Services.Implementations;
public sealed class PayOsCredentialProtector(IDataProtectionProvider provider) : IPayOsCredentialProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("SaveBite.PaymentPayOsCredentials.v1");
    public string Protect(PlatformPayOsCredentials credentials) => _protector.Protect(JsonSerializer.Serialize(credentials));
    public PlatformPayOsCredentials Unprotect(string value) =>
        JsonSerializer.Deserialize<PlatformPayOsCredentials>(_protector.Unprotect(value))
        ?? throw new InvalidOperationException("Invalid protected payment credentials.");
}
