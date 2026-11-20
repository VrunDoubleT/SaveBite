using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;
// Documentation host only. Never contacts or validates a live payment account.
public sealed class LocalDemoPayOsValidator : IPayOsCredentialsValidator {
 public Task ValidateAsync(PlatformPayOsCredentials credentials,CancellationToken ct=default)=>Task.CompletedTask;
 public Task ValidateShopAsync(PlatformPayOsCredentials credentials,CancellationToken ct=default)=>Task.CompletedTask;
 public Task ValidatePlatformAsync(PlatformPayOsCredentials credentials,CancellationToken ct=default)=>Task.CompletedTask;
}
