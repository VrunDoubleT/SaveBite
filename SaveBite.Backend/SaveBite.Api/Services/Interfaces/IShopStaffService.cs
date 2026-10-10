using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IShopStaffService
{

    Task<PagedResult<ShopStaffResponse>> GetShopStaffsAsync(Guid currentUserId, string? keyword, int page, int pageSize, CancellationToken cancellationToken = default);
    Task UpdateStaffInfoAsync(Guid currentUserId, Guid staffId, UpdateStaffInfoRequest request, CancellationToken cancellationToken = default);

    Task RemoveStaffAsync(Guid currentUserId, Guid staffId, CancellationToken cancellationToken = default);
    Task<PagedResult<StaffActivityLogResponse>> GetStaffActivityLogsAsync(Guid currentUserId, Guid staffId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<AssociatedShopResponse>> GetAssociatedShopsAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<StaffShopInfoResponse> GetShopAndStaffInfoAsync(Guid currentUserId, Guid shopId, CancellationToken cancellationToken = default);
   

    Task LeaveShopAsync( Guid currentUserId, Guid shopId, CancellationToken cancellationToken = default);
}