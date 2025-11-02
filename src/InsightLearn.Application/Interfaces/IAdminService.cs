using InsightLearn.Core.Entities;
using InsightLearn.Application.DTOs;

namespace InsightLearn.Application.Interfaces;

public interface IAdminService
{
    // User Management
    Task<IEnumerable<AdminUserDto>> GetAllUsersAsync(int page = 1, int pageSize = 50, string? searchTerm = null, string? role = null);
    Task<AdminUserDto?> GetUserByIdAsync(Guid userId);
    Task<AdminUserDto?> CreateUserAsync(CreateUserDto createUserDto, Guid adminUserId);
    Task<bool> UpdateUserAsync(Guid userId, UpdateUserDto updateUserDto, Guid adminUserId);
    Task<bool> BlockUserAsync(Guid userId, Guid adminUserId, string reason);
    Task<bool> UnblockUserAsync(Guid userId, Guid adminUserId);
    Task<bool> DeleteUserAsync(Guid userId, Guid adminUserId);
    Task<bool> AssignRoleAsync(Guid userId, string roleName, Guid adminUserId);
    Task<bool> RemoveRoleAsync(Guid userId, string roleName, Guid adminUserId);
    Task<int> GetTotalUsersCountAsync(string? searchTerm = null, string? role = null);
    
    // Dashboard Statistics
    Task<AdminDashboardDto> GetDashboardStatisticsAsync();
    
    // System Health
    Task<SystemHealthDto> GetSystemHealthAsync();
    
    // Recent Activity
    Task<IEnumerable<RecentActivityDto>> GetRecentActivityAsync(int count = 10);
    
    // Role Management
    Task<IEnumerable<RoleDto>> GetAllRolesAsync();
    
    // Audit and Logging
    Task<IEnumerable<AdminAuditLogDto>> GetAuditLogsAsync(int page = 1, int pageSize = 20, string? adminUserId = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<ErrorLogDto>> GetErrorLogsAsync(int page = 1, int pageSize = 20, string? severity = null, bool? isResolved = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<AccessLogDto>> GetAccessLogsAsync(int page = 1, int pageSize = 20, string? userId = null, int? statusCode = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<bool> ResolveErrorAsync(Guid errorId, Guid adminUserId, string notes);
    
    // System Maintenance
    Task<bool> OptimizeDatabaseAsync(Guid adminUserId);
    Task<bool> ClearCacheAsync(Guid adminUserId);
    Task<int> CleanupOldLogsAsync(int daysToKeep, Guid adminUserId);
    
    // Data Export
    Task<byte[]?> ExportUsersAsync(string format, string? searchTerm = null, string? role = null);
    Task<byte[]?> ExportAuditLogsAsync(string format, DateTime? fromDate = null, DateTime? toDate = null);
}