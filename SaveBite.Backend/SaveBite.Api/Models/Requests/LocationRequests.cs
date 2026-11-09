using System.ComponentModel.DataAnnotations;

namespace SaveBite.Backend.Models.Requests;

public class UpdateGpsRequest
{
    [Required]
    [Range(-90.0, 90.0)]
    public double Latitude { get; set; }

    [Required]
    [Range(-180.0, 180.0)]
    public double Longitude { get; set; }

    [Range(0.0, 5000.0)]
    public double? Accuracy { get; set; }
}
