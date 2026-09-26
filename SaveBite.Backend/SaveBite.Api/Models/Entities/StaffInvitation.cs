namespace SaveBite.Backend.Models.Entities;

public class StaffInvitation
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid InvitedUserId { get; set; }
    public Guid InvitedBy { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime InvitedAt { get; set; }
    public DateTime? RespondedAt { get; set; }

    public Shop Shop { get; set; } = null!;
    public User InvitedUser { get; set; } = null!;
    public User InvitedByUser { get; set; } = null!;
}
