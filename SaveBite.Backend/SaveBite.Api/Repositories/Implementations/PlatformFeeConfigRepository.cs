using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class PlatformFeeConfigRepository : IPlatformFeeConfigRepository
{
    private readonly AppDbContext _context;

    public PlatformFeeConfigRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PlatformFeeConfig?> GetActiveConfigAsync(DateTime asOf, CancellationToken ct = default)
    {
        return await _context.PlatformFeeConfigs
            .Where(c => c.EffectiveFrom <= asOf && (c.EffectiveTo == null || c.EffectiveTo > asOf))
            .OrderByDescending(c => c.EffectiveFrom)
            .ThenByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PlatformFeeConfig?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.PlatformFeeConfigs
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<IReadOnlyList<PlatformFeeConfig>> GetAllConfigsAsync(CancellationToken ct = default)
    {
        return await _context.PlatformFeeConfigs
            .OrderByDescending(c => c.EffectiveFrom)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync(ct);
    }
    
    public async Task<IReadOnlyList<PlatformFeeConfig>> GetPendingConfigsAsync(DateTime asOf, CancellationToken ct = default)
    {
        return await _context.PlatformFeeConfigs
            .Where(c => c.EffectiveFrom > asOf)
            .OrderByDescending(c => c.EffectiveFrom)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync(ct);
    }
    public async Task<PlatformFeeConfig?> GetUpcomingConfigAsync(DateTime asOf, CancellationToken ct = default)
    {
        return await _context.PlatformFeeConfigs
            .Where(c => c.EffectiveFrom > asOf)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }
    public async Task AddConfigAsync(PlatformFeeConfig config, CancellationToken ct = default)
    {
        await _context.PlatformFeeConfigs.AddAsync(config, ct);
    }

    public void RemoveConfig(PlatformFeeConfig config)
    {
        _context.PlatformFeeConfigs.Remove(config);
    }

    public void AddAuditLog(AuditLog audit)
    {
        _context.Set<AuditLog>().Add(audit);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
