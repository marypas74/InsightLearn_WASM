using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Infrastructure.Services;

/// <summary>
/// Implementation of audit logging service with database persistence
/// Provides compliance with GDPR Article 30, SOC 2, ISO 27001
/// </summary>
public class AuditService : IAuditService
{
    private readonly InsightLearnDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(InsightLearnDbContext context, ILogger<AuditService> _logger)
    {
        _context = context;
        this._logger = _logger;
    }

    public async Task LogAsync(
        string action,
        string? entityType,
        Guid? entityId,
        Guid? userId,
        string? userEmail,
        string? userRoles,
        string ipAddress,
        string httpMethod,
        string path,
        int statusCode,
        long durationMs,
        string? details = null,
        string? userAgent = null,
        string? referer = null,
        string? requestId = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                UserId = userId,
                UserEmail = userEmail,
                UserRoles = userRoles,
                IpAddress = ipAddress,
                HttpMethod = httpMethod,
                Path = path,
                StatusCode = statusCode,
                DurationMs = durationMs,
                Details = details,
                UserAgent = userAgent,
                Referer = referer,
                RequestId = requestId
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogDebug("[AUDIT DB] Audit log persisted: {Action}, User: {UserId}, Status: {StatusCode}",
                action, userId, statusCode);
        }
        catch (Exception ex)
        {
            // Don't throw - audit logging failure should not break application flow
            _logger.LogError(ex, "[AUDIT DB] Failed to persist audit log: {Action}, User: {UserId}",
                action, userId);
        }
    }

    public async Task<(List<AuditLog> Logs, int TotalCount)> GetAuditLogsAsync(
        int page = 1,
        int pageSize = 50,
        Guid? userId = null,
        string? action = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        // Input validation
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 500) pageSize = 500; // Max page size limit

        var query = _context.AuditLogs.AsQueryable();

        // Apply filters
        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action.Contains(action));

        if (fromDate.HasValue)
            query = query.Where(a => a.Timestamp >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.Timestamp <= toDate.Value);

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination
        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        return (logs, totalCount);
    }

    public async Task<List<AuditLog>> GetEntityAuditLogsAsync(string entityType, Guid entityId, int limit = 100)
    {
        // Input validation
        if (limit < 1) limit = 100;
        if (limit > 1000) limit = 1000; // Max limit

        return await _context.AuditLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> CleanupOldLogsAsync(int retentionDays, string? actionPrefix = null)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        var query = _context.AuditLogs.Where(a => a.Timestamp < cutoffDate);

        // Filter by action prefix if provided
        if (!string.IsNullOrEmpty(actionPrefix))
            query = query.Where(a => a.Action.StartsWith(actionPrefix));

        var deleted = await query.ExecuteDeleteAsync();

        _logger.LogInformation("[AUDIT DB] Cleanup completed: {DeletedCount} logs deleted (retention: {RetentionDays} days, prefix: {Prefix})",
            deleted, retentionDays, actionPrefix ?? "all");

        return deleted;
    }
}
