using System.ComponentModel.DataAnnotations;
namespace SaveBite.Backend.Models.Requests;

// Amount and scope are determined by the server; clients cannot request partial refunds.
public class CreateRefundRequest
{
    [Required, StringLength(1000, MinimumLength = 1)] public string Reason { get; set; } = string.Empty;
    [Required, MinLength(1), MaxLength(10)] public List<string> EvidenceUrls { get; set; } = [];
}
public class RefundBankRequest
{
    public string ReceiverMethod { get; set; } = "BankAccount";
    public string? QrImageUrl { get; set; }
    [Required, RegularExpression("^[0-9]{6}$")] public string BankBin { get; set; } = string.Empty;
    [Required, StringLength(100)] public string BankName { get; set; } = string.Empty;
    [Required, RegularExpression("^[0-9]{1,50}$")] public string AccountNumber { get; set; } = string.Empty;
    [Required, StringLength(150)] public string AccountHolder { get; set; } = string.Empty;
}
public class RefundTransferRequest
{
    [Required, StringLength(150)] public string TransferReference { get; set; } = string.Empty;
    public DateTime? TransferredAt { get; set; }
    [Required] public string? ProofUrl { get; set; }
}
public class RefundDeadlineRequest
{
    public DateTime DeadlineAt { get; set; }
    [Required, StringLength(1000)] public string Reason { get; set; } = string.Empty;
}

public class RefundImageUploadRequest
{
    [Required] public IFormFile File { get; set; } = null!;
    public string Purpose { get; set; } = "request-evidence";
}
