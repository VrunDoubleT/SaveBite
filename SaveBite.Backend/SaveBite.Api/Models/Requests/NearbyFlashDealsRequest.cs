namespace SaveBite.Backend.Models.Requests;

public class NearbyFlashDealsRequest
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double RadiusInKm { get; set; } = 5.0; 
}