using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Payments.Webhooks;

public sealed record PayOsAccount(string Id, PlatformPayOsCredentials Credentials, long Revision = 0,
    bool NeedsValidation = false)
{
    public const string Platform = "platform";
    public static string Shop(Guid id) => "shop:" + id.ToString("N");
    public string CredentialVersion => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        JsonSerializer.Serialize(new[] { Credentials.ClientId, Credentials.ApiKey, Credentials.ChecksumKey })))) + ":" + Revision;
}

public sealed record PayOsWebhookTarget(string AccountId, string CredentialVersion, string WebhookUrl, string Session)
{
    public bool Matches(PayOsWebhookSyncState state) => state.AccountId == AccountId
        && state.CredentialVersion == CredentialVersion && state.WebhookUrl == WebhookUrl && state.EndpointSession == Session;
    public bool IsReady(PayOsWebhookSyncState? state) => state is not null && Matches(state)
        && state.Status == "Synced" && state.CredentialsVerified && state.SucceededAt.HasValue;
}
