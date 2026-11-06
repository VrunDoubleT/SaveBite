namespace SaveBite.Backend.Models.Requests;

public sealed class UpdateShopPaymentConfigRequest
{
    public string PayosClientId { get; set; } = string.Empty;
    public string PayosApiKey { get; set; } = string.Empty;
    public string PayosChecksumKey { get; set; } = string.Empty;
}
