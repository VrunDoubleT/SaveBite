namespace SaveBite.Backend.Models.Entities;

public class UserAddress
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string? Label { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string? City { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
