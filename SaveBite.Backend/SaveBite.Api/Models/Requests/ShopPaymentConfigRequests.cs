namespace SaveBite.Backend.Models.Requests;

public sealed class UpdateShopPaymentConfigRequest
{
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankAccountHolder { get; set; }
    public string PayosClientId { get; set; } = string.Empty;
    public string PayosApiKey { get; set; } = string.Empty;
    public string PayosChecksumKey { get; set; } = string.Empty;
}
