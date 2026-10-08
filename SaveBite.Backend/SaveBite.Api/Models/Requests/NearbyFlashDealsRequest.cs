namespace SaveBite.Backend.Models.Requests;

public class NearbyFlashDealsRequest
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double RadiusInKm { get; set; } = 5.0; 
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 9;
}