using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class FlashDealRepository : IFlashDealRepository
{
    private readonly AppDbContext _context;

    public FlashDealRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<FlashDeal?> GetActiveDealByIdAsync(Guid dealId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FlashDeal>()
            .AsNoTracking()
            .Include(d => d.Shop)
            .Include(d => d.Product).ThenInclude(p => p.Images)
            .Include(d => d.Variants).ThenInclude(v => v.Variant)
            .FirstOrDefaultAsync(d => d.Id == dealId, cancellationToken);
    }

    public async Task<List<FlashDeal>> GetActiveDealsByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FlashDeal>()
            .AsNoTracking()
            .Include(d => d.Shop)
            .Include(d => d.Product).ThenInclude(p => p.Images)
            .Include(d => d.Variants).ThenInclude(v => v.Variant)
            .Where(d => d.ShopId == shopId && d.Status == FlashDealStatus.OnSale)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Shop>> GetSampleShopsForSeedAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _context.Shops
            .AsNoTracking()
            .Where(s => s.Status == ShopStatus.Active)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
