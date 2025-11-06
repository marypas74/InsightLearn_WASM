using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InsightLearn.Infrastructure.Repositories;

public class SystemEndpointRepository : ISystemEndpointRepository
{
    private readonly InsightLearnDbContext _context;

    public SystemEndpointRepository(InsightLearnDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SystemEndpoint>> GetAllActiveAsync()
    {
        return await _context.SystemEndpoints
            .Where(e => e.IsActive)
            .OrderBy(e => e.Category)
            .ThenBy(e => e.EndpointKey)
            .ToListAsync();
    }

    public async Task<SystemEndpoint?> GetByCategoryAndKeyAsync(string category, string endpointKey)
    {
        return await _context.SystemEndpoints
            .FirstOrDefaultAsync(e => e.Category == category && e.EndpointKey == endpointKey && e.IsActive);
    }

    public async Task<IEnumerable<SystemEndpoint>> GetByCategoryAsync(string category)
    {
        return await _context.SystemEndpoints
            .Where(e => e.Category == category && e.IsActive)
            .OrderBy(e => e.EndpointKey)
            .ToListAsync();
    }

    public async Task<SystemEndpoint> UpdateAsync(SystemEndpoint endpoint)
    {
        endpoint.LastModified = DateTime.UtcNow;
        _context.SystemEndpoints.Update(endpoint);
        await _context.SaveChangesAsync();
        return endpoint;
    }

    public async Task<SystemEndpoint> CreateAsync(SystemEndpoint endpoint)
    {
        endpoint.LastModified = DateTime.UtcNow;
        _context.SystemEndpoints.Add(endpoint);
        await _context.SaveChangesAsync();
        return endpoint;
    }

    public async Task DeleteAsync(int id)
    {
        var endpoint = await _context.SystemEndpoints.FindAsync(id);
        if (endpoint != null)
        {
            _context.SystemEndpoints.Remove(endpoint);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string category, string endpointKey)
    {
        return await _context.SystemEndpoints
            .AnyAsync(e => e.Category == category && e.EndpointKey == endpointKey);
    }
}
