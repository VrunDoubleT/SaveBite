using SaveBite.Backend.Models.Responses;
namespace SaveBite.Backend.Services.Interfaces;
public interface IRefundImageService
{
    Task<RefundImageResponse> UploadAsync(Guid userId, Guid orderId, bool forShop, IFormFile file, string purpose, CancellationToken ct);
}
