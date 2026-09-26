using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class ShopApplication
{
    public Guid Id { get; set; }
    public Guid ApplicantUserId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BusinessLicenseNo { get; set; }

    public string AddressLine { get; set; } = string.Empty;
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string? City { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public TimeOnly? OpeningTime { get; set; }
    public TimeOnly? ClosingTime { get; set; }

    public ShopApplicationStatus Status { get; set; } = ShopApplicationStatus.Pending;
    
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankAccountHolder { get; set; } = string.Empty;

    public string PayosClientId { get; set; } = string.Empty;
    public string PayosApiKey { get; set; } = string.Empty;
    public string PayosChecksumKey { get; set; } = string.Empty;

    public int RevisionNumber { get; set; } = 1;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User ApplicantUser { get; set; } = null!;
    public Shop? Shop { get; set; }
    public ICollection<ShopApplicationDocument> Documents { get; set; } = new List<ShopApplicationDocument>();
    public ICollection<ShopApplicationReviewLog> ReviewLogs { get; set; } = new List<ShopApplicationReviewLog>();
}
