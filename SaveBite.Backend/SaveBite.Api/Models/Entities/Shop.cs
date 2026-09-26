using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class Shop
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    
    public Guid ApplicationId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

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

    public ShopStatus Status { get; set; } = ShopStatus.Active;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User Owner { get; set; } = null!;
    public ShopApplication Application { get; set; } = null!;
    public ShopPaymentConfig? PaymentConfig { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<FlashDeal> FlashDeals { get; set; } = new List<FlashDeal>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<ShopStaff> StaffMembers { get; set; } = new List<ShopStaff>();
    public ICollection<StaffInvitation> StaffInvitations { get; set; } = new List<StaffInvitation>();
    public ICollection<StaffActivityLog> StaffActivityLogs { get; set; } = new List<StaffActivityLog>();
    public ICollection<PlatformFeeStatement> PlatformFeeStatements { get; set; } = new List<PlatformFeeStatement>();
    public ICollection<OrderPlatformFee> OrderPlatformFees { get; set; } = new List<OrderPlatformFee>();
    public ICollection<PlatformFeeAdjustment> PlatformFeeAdjustments { get; set; } = new List<PlatformFeeAdjustment>();
    public ICollection<PlatformFeePayment> PlatformFeePayments { get; set; } = new List<PlatformFeePayment>();
}
