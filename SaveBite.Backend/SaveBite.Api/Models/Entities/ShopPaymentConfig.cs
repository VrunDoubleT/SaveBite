namespace SaveBite.Backend.Models.Entities;

public class ShopPaymentConfig
{
    public int Id { get; set; }
    public Guid ShopId { get; set; }

    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankAccountHolder { get; set; }

    public string PayosClientId { get; set; } = string.Empty;
    public string PayosApiKey { get; set; } = string.Empty;
    public string PayosChecksumKey { get; set; } = string.Empty;

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Shop Shop { get; set; } = null!;
}
