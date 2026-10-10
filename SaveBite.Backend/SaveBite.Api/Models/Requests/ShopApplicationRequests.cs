namespace SaveBite.Backend.Models.Requests;

public class CreateShopApplicationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BusinessLicenseNo { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string? City { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public TimeOnly? OpeningTime { get; set; }
    public TimeOnly? ClosingTime { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankAccountHolder { get; set; } = string.Empty;
    public string PayosClientId { get; set; } = string.Empty;
    public string PayosApiKey { get; set; } = string.Empty;
    public string PayosChecksumKey { get; set; } = string.Empty;
}

public sealed class ResubmitShopApplicationRequest : CreateShopApplicationRequest
{
    public Guid ApplicationId { get; set; }
}

// ADMIN REQUESTS
public sealed class ReviewShopApplicationRequest
{
    public string Decision { get; set; } = string.Empty;
    public string? Note { get; set; }
}

