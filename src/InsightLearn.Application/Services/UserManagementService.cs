using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using InsightLearn.Core.Entities;
using InsightLearn.Infrastructure.Data;
using System.Security.Claims;

namespace InsightLearn.Application.Services;

public interface IUserManagementService
{
    Task<User?> GetUserAsync(ClaimsPrincipal user);
    Task<User?> FindByEmailAsync(string email);
    Task<User?> FindByIdAsync(string userId);
    Task<User?> GetUserWithRolesAsync(ClaimsPrincipal user);
    Task<string> GetUserNameAsync(User user);
    Task<IList<string>> GetRolesAsync(User user);
    Task<bool> IsInRoleAsync(User user, string role);
    Task<User?> RefreshUserDataAsync(string userId);
}

public class UserManagementService : IUserManagementService
{
    private readonly IDbContextFactory<InsightLearnDbContext> _contextFactory;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        IDbContextFactory<InsightLearnDbContext> contextFactory,
        UserManager<User> userManager,
        ILogger<UserManagementService> logger)
    {
        _contextFactory = contextFactory;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<User?> GetUserAsync(ClaimsPrincipal user)
    {
        var correlationId = Guid.NewGuid().ToString();
        try
        {
            _logger.LogInformation("UserManagementService: Getting user from claims principal. CorrelationId: {CorrelationId}", correlationId);
            
            // Get user ID from claims with multiple fallbacks
            var userId = GetUserIdFromClaims(user);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("UserManagementService: No user ID found in claims. CorrelationId: {CorrelationId}", correlationId);
                return null;
            }

            _logger.LogInformation("UserManagementService: Found user ID {UserId} from claims. CorrelationId: {CorrelationId}", userId, correlationId);

            // Use dedicated context to avoid threading issues
            using var context = await CreateContextWithRetryAsync(correlationId);
            if (context == null) return null;
            
            // Query user with optimized performance
            var foundUser = await context.Users
                .AsNoTracking() // Improve performance for read-only operations
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            
            if (foundUser != null)
            {
                _logger.LogInformation("UserManagementService: Successfully retrieved user {UserId}. CorrelationId: {CorrelationId}", foundUser.Id, correlationId);
            }
            else
            {
                _logger.LogWarning("UserManagementService: User {UserId} not found in database. CorrelationId: {CorrelationId}", userId, correlationId);
            }
            
            return foundUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UserManagementService: Error getting user from claims principal. CorrelationId: {CorrelationId}", correlationId);
            return null;
        }
    }
    
    public async Task<User?> GetUserWithRolesAsync(ClaimsPrincipal user)
    {
        var correlationId = Guid.NewGuid().ToString();
        try
        {
            _logger.LogInformation("UserManagementService: Getting user with roles from claims principal. CorrelationId: {CorrelationId}", correlationId);
            
            var userId = GetUserIdFromClaims(user);
            if (string.IsNullOrEmpty(userId)) return null;

            using var context = await CreateContextWithRetryAsync(correlationId);
            if (context == null) return null;
            
            // Load user (roles will be loaded separately via UserManager)
            var foundUser = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            
            return foundUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UserManagementService: Error getting user with roles. CorrelationId: {CorrelationId}", correlationId);
            return null;
        }
    }
    
    private string? GetUserIdFromClaims(ClaimsPrincipal user)
    {
        // Try multiple claim types for user ID
        return _userManager.GetUserId(user) 
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.FindFirst("userId")?.Value;
    }
    
    private async Task<InsightLearnDbContext?> CreateContextWithRetryAsync(string correlationId, int maxRetries = 3)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var context = _contextFactory.CreateDbContext();
                
                // Test the connection with a lightweight query
                await context.Database.CanConnectAsync();
                
                return context;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "UserManagementService: DbContext creation attempt {Attempt} failed. CorrelationId: {CorrelationId}", attempt, correlationId);
                
                if (attempt < maxRetries)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt - 1)));
                }
            }
        }
        
        _logger.LogError("UserManagementService: All DbContext creation attempts failed. CorrelationId: {CorrelationId}", correlationId);
        return null;
    }

    public async Task<User?> FindByEmailAsync(string email)
    {
        var correlationId = Guid.NewGuid().ToString();
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("UserManagementService: Email is null or empty. CorrelationId: {CorrelationId}", correlationId);
                return null;
            }
            
            _logger.LogInformation("UserManagementService: Finding user by email {Email}. CorrelationId: {CorrelationId}", email, correlationId);
            
            using var context = await CreateContextWithRetryAsync(correlationId);
            if (context == null) return null;
            
            var foundUser = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email!.ToLower() == email.ToLower());
            
            if (foundUser != null)
            {
                _logger.LogInformation("UserManagementService: Found user by email {Email}. UserId: {UserId}. CorrelationId: {CorrelationId}", email, foundUser.Id, correlationId);
            }
            else
            {
                _logger.LogInformation("UserManagementService: No user found with email {Email}. CorrelationId: {CorrelationId}", email, correlationId);
            }
            
            return foundUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UserManagementService: Error finding user by email {Email}. CorrelationId: {CorrelationId}", email, correlationId);
            return null;
        }
    }

    public async Task<User?> FindByIdAsync(string userId)
    {
        var correlationId = Guid.NewGuid().ToString();
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("UserManagementService: UserId is null or empty. CorrelationId: {CorrelationId}", correlationId);
                return null;
            }
            
            _logger.LogInformation("UserManagementService: Finding user by ID {UserId}. CorrelationId: {CorrelationId}", userId, correlationId);
            
            using var context = await CreateContextWithRetryAsync(correlationId);
            if (context == null) return null;
            
            if (Guid.TryParse(userId, out var userGuid))
            {
                var foundUser = await context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userGuid);
                
                if (foundUser != null)
                {
                    _logger.LogInformation("UserManagementService: Found user by ID {UserId}. CorrelationId: {CorrelationId}", userId, correlationId);
                }
                else
                {
                    _logger.LogInformation("UserManagementService: No user found with ID {UserId}. CorrelationId: {CorrelationId}", userId, correlationId);
                }
                
                return foundUser;
            }
            else
            {
                _logger.LogWarning("UserManagementService: Invalid GUID format for UserId {UserId}. CorrelationId: {CorrelationId}", userId, correlationId);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UserManagementService: Error finding user by ID {UserId}. CorrelationId: {CorrelationId}", userId, correlationId);
            return null;
        }
    }
    
    public async Task<User?> RefreshUserDataAsync(string userId)
    {
        var correlationId = Guid.NewGuid().ToString();
        try
        {
            _logger.LogInformation("UserManagementService: Refreshing user data for {UserId}. CorrelationId: {CorrelationId}", userId, correlationId);
            
            // Force a fresh database query, bypassing any caching
            using var context = await CreateContextWithRetryAsync(correlationId);
            if (context == null) return null;
            
            if (Guid.TryParse(userId, out var userGuid))
            {
                var refreshedUser = await context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userGuid);
                
                _logger.LogInformation("UserManagementService: User data refreshed for {UserId}. CorrelationId: {CorrelationId}", userId, correlationId);
                return refreshedUser;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UserManagementService: Error refreshing user data for {UserId}. CorrelationId: {CorrelationId}", userId, correlationId);
            return null;
        }
    }

    public async Task<string> GetUserNameAsync(User user)
    {
        return await Task.FromResult(user.UserName ?? user.Email ?? "Unknown User");
    }

    public async Task<IList<string>> GetRolesAsync(User user)
    {
        var correlationId = Guid.NewGuid().ToString();
        try
        {
            if (user == null)
            {
                _logger.LogWarning("UserManagementService: User is null when getting roles. CorrelationId: {CorrelationId}", correlationId);
                return new List<string>();
            }
            
            _logger.LogInformation("UserManagementService: Getting roles for user {UserId}. CorrelationId: {CorrelationId}", user.Id, correlationId);
            
            using var context = await CreateContextWithRetryAsync(correlationId);
            if (context == null) return new List<string>();
            
            var roles = await (from ur in context.UserRoles
                             join r in context.Roles on ur.RoleId equals r.Id
                             where ur.UserId == user.Id
                             select r.Name!)
                             .AsNoTracking()
                             .ToListAsync();
            
            _logger.LogInformation("UserManagementService: Found {RoleCount} roles for user {UserId}. Roles: {Roles}. CorrelationId: {CorrelationId}", 
                roles.Count, user.Id, string.Join(", ", roles), correlationId);
            
            return roles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UserManagementService: Error getting roles for user {UserId}. CorrelationId: {CorrelationId}", user?.Id, correlationId);
            return new List<string>();
        }
    }
    
    public async Task<bool> IsInRoleAsync(User user, string role)
    {
        var correlationId = Guid.NewGuid().ToString();
        try
        {
            if (user == null || string.IsNullOrWhiteSpace(role))
            {
                return false;
            }
            
            var userRoles = await GetRolesAsync(user);
            var isInRole = userRoles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
            
            _logger.LogInformation("UserManagementService: User {UserId} is{IsInRoleResult} in role {Role}. CorrelationId: {CorrelationId}", 
                user.Id, isInRole ? "" : " not", role, correlationId);
            
            return isInRole;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UserManagementService: Error checking if user {UserId} is in role {Role}. CorrelationId: {CorrelationId}", user?.Id, role, correlationId);
            return false;
        }
    }
}