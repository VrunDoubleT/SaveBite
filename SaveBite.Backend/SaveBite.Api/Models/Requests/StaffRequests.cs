using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Requests;

public sealed record InviteStaffRequest(
    [Required, EmailAddress] string InvitedUserEmail
);


public sealed record UpdateStaffInfoRequest(
    string? StaffNickname,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    ShopStaffStatus Status
);
