namespace SaveBite.Backend.Models.Enums;

public enum OrderStatus
{
    Pending,
    Confirmed,
    Preparing,
    ReadyForPickup,
    Completed,
    Cancelled,
    Expired,
    NoShow
}