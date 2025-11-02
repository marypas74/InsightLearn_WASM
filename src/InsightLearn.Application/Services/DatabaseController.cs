using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace InsightLearn.Application.Services;

public class DatabaseController : IDatabaseController
{
    private readonly InsightLearnDbContext _context;
    private readonly ILogger<DatabaseController> _logger;

    public DatabaseController(InsightLearnDbContext context, ILogger<DatabaseController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<T> ExecuteWithAuditAsync<T>(string operation, string userId, Func<Task<T>> dbOperation)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation("Database operation started: {Operation} by user {UserId}", operation, userId);
            
            var result = await dbOperation();
            
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Database operation completed: {Operation} in {Duration}ms", 
                operation, duration.TotalMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Database operation failed: {Operation} after {Duration}ms", 
                operation, duration.TotalMilliseconds);
            throw;
        }
    }

    public async Task ExecuteWithAuditAsync(string operation, string userId, Func<Task> dbOperation)
    {
        await ExecuteWithAuditAsync<object>(operation, userId, async () =>
        {
            await dbOperation();
            return null!;
        });
    }

    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            await _context.Database.OpenConnectionAsync();
            await _context.Database.CloseConnectionAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}