using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IShopApplicationService
{
    Task<ShopApplicationResponse?> GetMyApplicationAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShopApplicationResponse>> GetMyApplicationHistoryAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ShopApplicationResponse> CreateAsync(Guid userId, CreateShopApplicationRequest request, IFormFile? logo, IFormFile? coverImage, IReadOnlyList<IFormFile> documents, IReadOnlyList<string> documentTypes, CancellationToken cancellationToken = default);
    Task<ShopApplicationResponse> ResubmitAsync(Guid userId, ResubmitShopApplicationRequest request, IFormFile? logo, IFormFile? coverImage, IReadOnlyList<IFormFile> documents, IReadOnlyList<string> documentTypes, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShopApplicationResponse>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<ShopApplicationResponse> GetByIdForAdminAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task<ShopApplicationResponse> ReviewAsync(Guid adminId, Guid applicationId, ReviewShopApplicationRequest request, CancellationToken cancellationToken = default);
}
