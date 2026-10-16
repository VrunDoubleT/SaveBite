namespace SaveBite.Backend.Models.Requests;

public sealed class UpdateBusinessInfoRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string? City { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public TimeOnly? OpeningTime { get; set; }
    public TimeOnly? ClosingTime { get; set; }
}

public sealed class UpdateShopImagesRequest
{
    public IFormFile? Logo { get; set; }
    public IFormFile? CoverImage { get; set; }
}