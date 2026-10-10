namespace SaveBite.Backend.Models.Requests;

public class UserAddressRequests
{
    public sealed class CreateAddressRequest
    {
        public string? Label { get; set; }
        public string AddressLine { get; set; } = string.Empty;
        public string? Ward { get; set; }
        public string? District { get; set; }
        public string? City { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsDefault { get; set; }
    }

    public sealed class UpdateAddressRequest
    {
        public string? Label { get; set; }
        public string AddressLine { get; set; } = string.Empty;
        public string? Ward { get; set; }
        public string? District { get; set; }
        public string? City { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsDefault { get; set; }
    }
}