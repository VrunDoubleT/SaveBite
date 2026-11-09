namespace SaveBite.Backend.Models.DTOs;

public class UserGpsMetadata
{
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public double? Accuracy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long GpsVersion { get; set; }
}
