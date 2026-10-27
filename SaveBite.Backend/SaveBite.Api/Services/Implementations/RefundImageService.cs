using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;
namespace SaveBite.Backend.Services.Implementations;

public class RefundImageService : IRefundImageService
{
    private readonly ICheckoutRepository _orders;
    private readonly IShopOrderRepository _shops;
    private readonly ICloudinaryService _images;
    public RefundImageService(ICheckoutRepository orders, IShopOrderRepository shops, ICloudinaryService images)
    { _orders = orders; _shops = shops; _images = images; }

    // Authorize before upload; Cloudinary validates image signatures and stores the actual file.
    public async Task<RefundImageResponse> UploadAsync(Guid userId, Guid orderId, Guid? shopId, IFormFile file, string purpose, CancellationToken ct)
    {
        if (shopId.HasValue)
        {
            if (purpose != "transfer-proof" || !await _shops.RequireAccessAsync(userId, shopId.Value, ct)) throw AppException.Forbidden();
            _ = await _shops.GetAsync(shopId.Value, orderId, ct) ?? throw AppException.NotFound();
        }
        else
        {
            _ = await _orders.GetOrderAsync(orderId, userId, ct) ?? throw AppException.NotFound();
            if (purpose is not ("request-evidence" or "destination-qr")) throw AppException.BadRequest("Invalid image purpose.");
        }
        if (file == null || file.Length is <= 0 or > 5 * 1024 * 1024) throw AppException.BadRequest("Upload an image up to 5 MB.");
        var result = new RefundImageResponse();
        if (purpose == "destination-qr")
        {
            var bank = await RefundQrReader.ReadAsync(file, ct);
            result.BankBin = bank.BankBin; result.AccountNumber = bank.AccountNumber; result.AccountHolder = bank.AccountHolder;
        }
        try { result.Url = (await _images.UploadImageAsync(file, ct)).Url; }
        catch (ArgumentException) { throw AppException.BadRequest("Upload a valid PNG, JPEG or WebP image up to 5 MB."); }
        return result;
    }
}
