using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

// Manage grouped customer carts, quantities and cursor-based loading.
public interface ICartService
{
    Task<CartPageResponse> GetPageAsync(Guid userId, string? cursor, int pageSize, CancellationToken ct = default);
    Task<CartSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken ct = default);
    Task<CartResponse> AddItemAsync(Guid userId, AddCartItemRequest request, CancellationToken cancellationToken = default);
    Task<CartMutationResponse> UpdateQuantityAsync(Guid userId, Guid itemId, UpdateCartItemQuantityRequest request, CancellationToken cancellationToken = default);
    Task<CartMutationResponse> RemoveItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);
}
