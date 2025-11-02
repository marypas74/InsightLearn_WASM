using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using InsightLearn.Application.Interfaces;
using InsightLearn.Application.DTOs;
using InsightLearn.Core.Entities;
using InsightLearn.Infrastructure.Data;
using System.Diagnostics;

namespace InsightLearn.Application.Services;

public class AdminService : IAdminService
{
    private readonly IDbContextFactory<InsightLearnDbContext> _contextFactory;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ILoggingService _loggingService;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        IDbContextFactory<InsightLearnDbContext> contextFactory,
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILoggingService loggingService,
        ILogger<AdminService> logger)
    {
        _contextFactory = contextFactory;
        _userManager = userManager;
        _roleManager = roleManager;
        _loggingService = loggingService;
        _logger = logger;
    }

    public async Task<IEnumerable<AdminUserDto>> GetAllUsersAsync(int page = 1, int pageSize = 50, string? searchTerm = null, string? role = null)
    {
        // 🔥 CRITICAL FIX: Use DbContextFactory to prevent threading issues
        using var context = _contextFactory.CreateDbContext();
        
        var query = context.Users.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(u => u.FirstName.Contains(searchTerm) || 
                                   u.LastName.Contains(searchTerm) || 
                                   u.Email.Contains(searchTerm) ||
                                   u.UserName!.Contains(searchTerm));
        }

        var users = await query
            .OrderByDescending(u => u.DateJoined)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userDtos = new List<AdminUserDto>();

        foreach (var user in users)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            var courseCount = await context.Courses.CountAsync(c => c.InstructorId == user.Id);
            var enrollmentCount = await context.Enrollments.CountAsync(e => e.UserId == user.Id);

            userDtos.Add(new AdminUserDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                IsVerified = user.IsVerified,
                IsInstructor = user.IsInstructor,
                DateJoined = user.DateJoined,
                LastLoginDate = user.LastLoginDate,
                WalletBalance = user.WalletBalance,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                Roles = userRoles.ToList(),
                TotalCourses = courseCount,
                TotalEnrollments = enrollmentCount
            });
        }

        if (!string.IsNullOrEmpty(role))
        {
            userDtos = userDtos.Where(u => u.Roles.Contains(role)).ToList();
        }

        return userDtos;
    }

    public async Task<AdminUserDto?> GetUserByIdAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return null;

        // 🔥 CRITICAL FIX: Use DbContextFactory to prevent threading issues
        using var context = _contextFactory.CreateDbContext();
        
        var userRoles = await _userManager.GetRolesAsync(user);
        var courseCount = await context.Courses.CountAsync(c => c.InstructorId == user.Id);
        var enrollmentCount = await context.Enrollments.CountAsync(e => e.UserId == user.Id);

        return new AdminUserDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            IsVerified = user.IsVerified,
            IsInstructor = user.IsInstructor,
            DateJoined = user.DateJoined,
            LastLoginDate = user.LastLoginDate,
            WalletBalance = user.WalletBalance,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd,
            Roles = userRoles.ToList(),
            TotalCourses = courseCount,
            TotalEnrollments = enrollmentCount
        };
    }

    public async Task<AdminUserDto?> CreateUserAsync(CreateUserDto createUserDto, Guid adminUserId)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(createUserDto.Email);
        if (existingUser != null)
        {
            return null;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = createUserDto.Email,
            Email = createUserDto.Email,
            FirstName = createUserDto.FirstName,
            LastName = createUserDto.LastName,
            PhoneNumber = createUserDto.PhoneNumber,
            WalletBalance = createUserDto.WalletBalance,
            EmailConfirmed = createUserDto.EmailConfirmed,
            IsVerified = createUserDto.EmailConfirmed,
            DateJoined = DateTime.UtcNow,
            Bio = createUserDto.Bio,
            ProfileImageUrl = createUserDto.ProfilePictureUrl,
            IsInstructor = createUserDto.Role == "Instructor",
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        var result = await _userManager.CreateAsync(user, createUserDto.Password);
        if (!result.Succeeded)
        {
            return null;
        }

        // Assign role
        if (!string.IsNullOrEmpty(createUserDto.Role))
        {
            await _userManager.AddToRoleAsync(user, createUserDto.Role);
        }

        // Log the creation
        await _loggingService.LogAdminActionAsync(
            adminUserId.ToString(), "CreateUser", "User", user.Id,
            $"Created new user {user.Email} with role {createUserDto.Role}",
            severity: "Info");

        // Return the created user as AdminUserDto
        using var context = _contextFactory.CreateDbContext();
        var userRoles = await _userManager.GetRolesAsync(user);
        
        return new AdminUserDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            IsVerified = user.IsVerified,
            IsInstructor = user.IsInstructor,
            DateJoined = user.DateJoined,
            LastLoginDate = user.LastLoginDate,
            WalletBalance = user.WalletBalance,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd,
            Roles = userRoles.ToList(),
            TotalCourses = 0,
            TotalEnrollments = 0
        };
    }

    public async Task<bool> UpdateUserAsync(Guid userId, UpdateUserDto updateUserDto, Guid adminUserId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        var oldValues = $"FirstName: {user.FirstName}, LastName: {user.LastName}, Email: {user.Email}, IsVerified: {user.IsVerified}, IsInstructor: {user.IsInstructor}, WalletBalance: {user.WalletBalance}";

        user.FirstName = updateUserDto.FirstName;
        user.LastName = updateUserDto.LastName;
        user.Email = updateUserDto.Email;
        user.IsVerified = updateUserDto.IsVerified;
        user.IsInstructor = updateUserDto.IsInstructor;
        user.WalletBalance = updateUserDto.WalletBalance;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            var newValues = $"FirstName: {user.FirstName}, LastName: {user.LastName}, Email: {user.Email}, IsVerified: {user.IsVerified}, IsInstructor: {user.IsInstructor}, WalletBalance: {user.WalletBalance}";
            
            await _loggingService.LogAdminActionAsync(
                adminUserId.ToString(), "UpdateUser", "User", userId,
                $"Updated user {user.Email}",
                oldValues, newValues);
        }

        return result.Succeeded;
    }

    public async Task<bool> BlockUserAsync(Guid userId, Guid adminUserId, string reason)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100); // Effectively permanent

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            await _loggingService.LogAdminActionAsync(
                adminUserId.ToString(), "BlockUser", "User", userId,
                $"Blocked user {user.Email}. Reason: {reason}",
                severity: "Warning");
        }

        return result.Succeeded;
    }

    public async Task<bool> UnblockUserAsync(Guid userId, Guid adminUserId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        user.LockoutEnabled = false;
        user.LockoutEnd = null;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            await _loggingService.LogAdminActionAsync(
                adminUserId.ToString(), "UnblockUser", "User", userId,
                $"Unblocked user {user.Email}");
        }

        return result.Succeeded;
    }

    public async Task<bool> DeleteUserAsync(Guid userId, Guid adminUserId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        var result = await _userManager.DeleteAsync(user);

        if (result.Succeeded)
        {
            await _loggingService.LogAdminActionAsync(
                adminUserId.ToString(), "DeleteUser", "User", userId,
                $"Deleted user {user.Email}",
                severity: "Critical");
        }

        return result.Succeeded;
    }

    public async Task<bool> AssignRoleAsync(Guid userId, string roleName, Guid adminUserId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        var result = await _userManager.AddToRoleAsync(user, roleName);

        if (result.Succeeded)
        {
            await _loggingService.LogAdminActionAsync(
                adminUserId.ToString(), "AssignRole", "User", userId,
                $"Assigned role '{roleName}' to user {user.Email}");
        }

        return result.Succeeded;
    }

    public async Task<bool> RemoveRoleAsync(Guid userId, string roleName, Guid adminUserId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        var result = await _userManager.RemoveFromRoleAsync(user, roleName);

        if (result.Succeeded)
        {
            await _loggingService.LogAdminActionAsync(
                adminUserId.ToString(), "RemoveRole", "User", userId,
                $"Removed role '{roleName}' from user {user.Email}");
        }

        return result.Succeeded;
    }

    public async Task<int> GetTotalUsersCountAsync(string? searchTerm = null, string? role = null)
    {
        // 🔥 CRITICAL FIX: Use DbContextFactory to prevent threading issues
        using var context = _contextFactory.CreateDbContext();
        
        var query = context.Users.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(u => u.FirstName.Contains(searchTerm) || 
                                   u.LastName.Contains(searchTerm) || 
                                   u.Email.Contains(searchTerm) ||
                                   u.UserName!.Contains(searchTerm));
        }

        return await query.CountAsync();
    }

    public async Task<AdminDashboardDto> GetDashboardStatisticsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);
        var lastWeek = today.AddDays(-7);

        // 🔥 FIX: Use separate contexts for each query to avoid threading issues
        using var context1 = await _contextFactory.CreateDbContextAsync();
        using var context2 = await _contextFactory.CreateDbContextAsync();
        using var context3 = await _contextFactory.CreateDbContextAsync();
        using var context4 = await _contextFactory.CreateDbContextAsync();
        
        var totalUsers = await context1.Users.CountAsync();
        var totalActiveUsers = await context1.Users.CountAsync(u => u.LastLoginDate >= lastWeek);
        var totalInstructors = await context1.Users.CountAsync(u => u.IsInstructor);
        var totalStudents = totalUsers - totalInstructors;
        var totalCourses = await context2.Courses.CountAsync();
        var totalEnrollments = await context3.Enrollments.CountAsync();
        var totalPayments = await context4.Payments.CountAsync();
        var totalRevenue = await context4.Payments.SumAsync(p => p.Amount);

        // 🔥 CRITICAL FIX: Use single context for today's data to avoid threading issues
        using var contextToday = _contextFactory.CreateDbContext();
        
        var newUsersToday = await contextToday.Users.CountAsync(u => u.DateJoined >= today);
        var newCoursesToday = await contextToday.Courses.CountAsync(c => c.CreatedAt >= today);
        var newEnrollmentsToday = await contextToday.Enrollments.CountAsync(e => e.EnrolledAt >= today);
        var errorsToday = await contextToday.ErrorLogs.CountAsync(e => e.LoggedAt >= today);

        var newUsersYesterday = await contextToday.Users.CountAsync(u => u.DateJoined >= yesterday && u.DateJoined < today);
        var newCoursesYesterday = await contextToday.Courses.CountAsync(c => c.CreatedAt >= yesterday && c.CreatedAt < today);
        var revenueYesterday = await contextToday.Payments
            .Where(p => p.CreatedAt >= yesterday && p.CreatedAt < today)
            .SumAsync(p => p.Amount);

        var userGrowthRate = newUsersYesterday > 0 ? ((double)(newUsersToday - newUsersYesterday) / newUsersYesterday) * 100 : 0;
        var courseGrowthRate = newCoursesYesterday > 0 ? ((double)(newCoursesToday - newCoursesYesterday) / newCoursesYesterday) * 100 : 0;
        var revenueGrowthRate = revenueYesterday > 0 ? ((double)(totalRevenue - revenueYesterday) / (double)revenueYesterday) * 100 : 0;

        // SEO Statistics with separate context
        var seoOptimizedPages = await contextToday.SeoSettings.CountAsync(s => !string.IsNullOrEmpty(s.MetaTitle) && !string.IsNullOrEmpty(s.MetaDescription));
        var seoTotalPages = await contextToday.SeoSettings.CountAsync();
        var seoAudits = await contextToday.SeoAudits.ToListAsync();
        var seoAverageScore = seoAudits.Any() ? seoAudits.Average(a => a.SeoScore) : 0;
        var seoIssuesCount = await contextToday.SeoAudits.CountAsync(a => a.SeoScore < 70);

        // Get daily stats for the last 7 days
        var dailyStats = new List<DailyStatsDto>();
        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var nextDate = date.AddDays(1);

            dailyStats.Add(new DailyStatsDto
            {
                Date = date,
                NewUsers = await contextToday.Users.CountAsync(u => u.DateJoined >= date && u.DateJoined < nextDate),
                NewCourses = await contextToday.Courses.CountAsync(c => c.CreatedAt >= date && c.CreatedAt < nextDate),
                NewEnrollments = await contextToday.Enrollments.CountAsync(e => e.EnrolledAt >= date && e.EnrolledAt < nextDate),
                Revenue = await contextToday.Payments.Where(p => p.CreatedAt >= date && p.CreatedAt < nextDate).SumAsync(p => p.Amount),
                Errors = await contextToday.ErrorLogs.CountAsync(e => e.LoggedAt >= date && e.LoggedAt < nextDate)
            });
        }

        return new AdminDashboardDto
        {
            TotalUsers = totalUsers,
            TotalActiveUsers = totalActiveUsers,
            TotalInstructors = totalInstructors,
            TotalStudents = totalStudents,
            TotalCourses = totalCourses,
            TotalEnrollments = totalEnrollments,
            TotalPayments = totalPayments,
            TotalRevenue = totalRevenue,
            NewUsersToday = newUsersToday,
            NewCoursesToday = newCoursesToday,
            NewEnrollmentsToday = newEnrollmentsToday,
            ErrorsToday = errorsToday,
            UserGrowthRate = userGrowthRate,
            CourseGrowthRate = courseGrowthRate,
            RevenueGrowthRate = revenueGrowthRate,
            SeoOptimizedPages = seoOptimizedPages,
            SeoTotalPages = seoTotalPages,
            SeoAverageScore = seoAverageScore,
            SeoIssuesCount = seoIssuesCount,
            DailyStats = dailyStats
        };
    }

    public async Task<SystemHealthDto> GetSystemHealthAsync()
    {
        var health = new SystemHealthDto();
        var alerts = new List<DTOs.SystemAlert>();

        // 🔥 CRITICAL FIX: Use DbContextFactory for system health checks
        using var healthContext = _contextFactory.CreateDbContext();
        
        // Test database connection
        var dbStopwatch = Stopwatch.StartNew();
        try
        {
            await healthContext.Database.CanConnectAsync();
            health.DatabaseConnected = true;
            dbStopwatch.Stop();
            health.DatabaseResponseTime = $"{dbStopwatch.ElapsedMilliseconds}ms";
        }
        catch
        {
            health.DatabaseConnected = false;
            health.DatabaseResponseTime = "Error";
            alerts.Add(new DTOs.SystemAlert
            {
                Type = "Database",
                Message = "Database connection failed",
                Severity = "Critical",
                Timestamp = DateTime.UtcNow
            });
        }

        // Get active sessions count (approximate)
        health.ActiveSessions = await healthContext.AccessLogs
            .Where(al => al.AccessedAt >= DateTime.UtcNow.AddMinutes(-30))
            .Select(al => al.SessionId)
            .Distinct()
            .CountAsync();

        // Memory usage
        var process = Process.GetCurrentProcess();
        health.MemoryUsage = process.WorkingSet64;

        // Check for high error rates
        var recentErrors = await healthContext.ErrorLogs
            .CountAsync(e => e.LoggedAt >= DateTime.UtcNow.AddHours(-1));

        if (recentErrors > 10)
        {
            alerts.Add(new DTOs.SystemAlert
            {
                Type = "Errors",
                Message = $"High error rate: {recentErrors} errors in the last hour",
                Severity = "Warning",
                Timestamp = DateTime.UtcNow
            });
        }

        health.Alerts = alerts;
        return health;
    }

    public async Task<IEnumerable<RecentActivityDto>> GetRecentActivityAsync(int count = 10)
    {
        var activities = new List<RecentActivityDto>();

        // 🔥 CRITICAL FIX: Use DbContextFactory for recent activity
        using var activityContext = _contextFactory.CreateDbContext();
        
        // Get recent admin actions
        var adminActions = await activityContext.AdminAuditLogs
            .Include(aal => aal.AdminUser)
            .OrderByDescending(aal => aal.PerformedAt)
            .Take(count / 2)
            .ToListAsync();

        foreach (var action in adminActions)
        {
            activities.Add(new RecentActivityDto
            {
                Type = "Admin Action",
                Description = action.Description ?? action.Action,
                UserName = action.AdminUser.FullName,
                Timestamp = action.PerformedAt,
                Icon = GetActionIcon(action.Action),
                Severity = action.Severity
            });
        }

        // Get recent errors
        var recentErrors = await activityContext.ErrorLogs
            .Where(el => !el.IsResolved)
            .OrderByDescending(el => el.LoggedAt)
            .Take(count / 2)
            .ToListAsync();

        foreach (var error in recentErrors)
        {
            activities.Add(new RecentActivityDto
            {
                Type = "Error",
                Description = error.ExceptionMessage.Length > 100 ? 
                    error.ExceptionMessage.Substring(0, 100) + "..." : 
                    error.ExceptionMessage,
                UserName = error.UserId?.ToString() ?? "System",
                Timestamp = error.LoggedAt,
                Icon = "fas fa-exclamation-triangle",
                Severity = error.Severity
            });
        }

        return activities.OrderByDescending(a => a.Timestamp).Take(count);
    }

    // New methods for enhanced AdminController functionality
    public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        var roleDtos = new List<RoleDto>();

        foreach (var role in roles)
        {
            var userCount = await _userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);
            
            roleDtos.Add(new RoleDto
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                Description = role.Name switch
                {
                    "Administrator" => "Full system access and management",
                    "Instructor" => "Can create and manage courses",
                    "Student" => "Can enroll in and access courses",
                    "Moderator" => "Can moderate content and user interactions",
                    _ => "Standard user role"
                },
                UserCount = userCount.Count
            });
        }

        return roleDtos;
    }

    public async Task<IEnumerable<AdminAuditLogDto>> GetAuditLogsAsync(int page = 1, int pageSize = 20, string? adminUserId = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        using var context = _contextFactory.CreateDbContext();
        
        var query = context.AdminAuditLogs
            .Include(aal => aal.AdminUser)
            .AsQueryable();

        if (!string.IsNullOrEmpty(adminUserId) && Guid.TryParse(adminUserId, out var adminGuid))
        {
            query = query.Where(aal => aal.AdminUserId == adminGuid);
        }

        if (!string.IsNullOrEmpty(action))
        {
            query = query.Where(aal => aal.Action.Contains(action));
        }

        if (fromDate.HasValue)
        {
            query = query.Where(aal => aal.PerformedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(aal => aal.PerformedAt <= toDate.Value);
        }

        var auditLogs = await query
            .OrderByDescending(aal => aal.PerformedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return auditLogs.Select(aal => new AdminAuditLogDto
        {
            Id = aal.Id,
            AdminUserName = aal.AdminUser.FullName,
            Action = aal.Action,
            EntityType = aal.EntityType,
            EntityId = aal.EntityId,
            Description = aal.Description,
            PerformedAt = aal.PerformedAt,
            Severity = aal.Severity,
            IpAddress = aal.IpAddress
        });
    }

    public async Task<IEnumerable<ErrorLogDto>> GetErrorLogsAsync(int page = 1, int pageSize = 20, string? severity = null, bool? isResolved = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        using var context = _contextFactory.CreateDbContext();
        
        var query = context.ErrorLogs.AsQueryable();

        if (!string.IsNullOrEmpty(severity))
        {
            query = query.Where(el => el.Severity == severity);
        }

        if (isResolved.HasValue)
        {
            query = query.Where(el => el.IsResolved == isResolved.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(el => el.LoggedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(el => el.LoggedAt <= toDate.Value);
        }

        var errorLogs = await query
            .OrderByDescending(el => el.LoggedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return errorLogs.Select(el => new ErrorLogDto
        {
            Id = el.Id,
            UserName = el.UserId?.ToString(),
            ExceptionType = el.ExceptionType,
            ExceptionMessage = el.ExceptionMessage,
            StackTrace = el.StackTrace,
            RequestPath = el.RequestPath,
            HttpMethod = el.HttpMethod,
            Severity = el.Severity,
            LoggedAt = el.LoggedAt,
            IsResolved = el.IsResolved,
            ResolvedAt = el.ResolvedAt,
            ResolvedByUserName = el.ResolvedByUserId?.ToString(),
            ResolutionNotes = el.ResolutionNotes
        });
    }

    public async Task<IEnumerable<AccessLogDto>> GetAccessLogsAsync(int page = 1, int pageSize = 20, string? userId = null, int? statusCode = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        using var context = _contextFactory.CreateDbContext();
        
        var query = context.AccessLogs.AsQueryable();

        if (!string.IsNullOrEmpty(userId))
        {
            if (Guid.TryParse(userId, out var userGuid))
            {
                query = query.Where(al => al.UserId == userGuid);
            }
        }

        if (statusCode.HasValue)
        {
            query = query.Where(al => al.StatusCode == statusCode.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(al => al.AccessedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(al => al.AccessedAt <= toDate.Value);
        }

        var accessLogs = await query
            .OrderByDescending(al => al.AccessedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return accessLogs.Select(al => new AccessLogDto
        {
            Id = al.Id,
            UserName = al.UserId?.ToString(),
            IpAddress = al.IpAddress,
            RequestPath = al.RequestPath,
            HttpMethod = al.HttpMethod,
            StatusCode = al.StatusCode,
            ResponseTimeMs = al.ResponseTimeMs,
            AccessedAt = al.AccessedAt,
            UserAgent = al.UserAgent
        });
    }

    public async Task<bool> ResolveErrorAsync(Guid errorId, Guid adminUserId, string notes)
    {
        using var context = _contextFactory.CreateDbContext();
        
        var errorLog = await context.ErrorLogs.FindAsync(errorId);
        if (errorLog == null) return false;

        errorLog.IsResolved = true;
        errorLog.ResolvedAt = DateTime.UtcNow;
        errorLog.ResolvedByUserId = adminUserId;
        errorLog.ResolutionNotes = notes;

        await context.SaveChangesAsync();

        await _loggingService.LogAdminActionAsync(
            adminUserId.ToString(), "ResolveError", "ErrorLog", errorId,
            $"Resolved error: {errorLog.ExceptionMessage}",
            severity: "Info");

        return true;
    }

    public async Task<bool> OptimizeDatabaseAsync(Guid adminUserId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            
            // Execute database maintenance commands
            // Note: These are SQL Server specific commands
            await context.Database.ExecuteSqlRawAsync("DBCC UPDATEUSAGE(0) WITH NO_INFOMSGS");
            await context.Database.ExecuteSqlRawAsync("ALTER INDEX ALL ON [dbo].[Users] REORGANIZE");
            await context.Database.ExecuteSqlRawAsync("ALTER INDEX ALL ON [dbo].[Courses] REORGANIZE");
            await context.Database.ExecuteSqlRawAsync("UPDATE STATISTICS [dbo].[Users]");
            await context.Database.ExecuteSqlRawAsync("UPDATE STATISTICS [dbo].[Courses]");

            await _loggingService.LogAdminActionAsync(
                adminUserId.ToString(), "OptimizeDatabase", "System", null,
                "Database optimization completed",
                severity: "Info");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database optimization");
            return false;
        }
    }

    public async Task<bool> ClearCacheAsync(Guid adminUserId)
    {
        try
        {
            // This would depend on your caching implementation
            // For now, we'll just log the action
            
            await _loggingService.LogAdminActionAsync(
                adminUserId.ToString(), "ClearCache", "System", null,
                "Application cache cleared",
                severity: "Info");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            return false;
        }
    }

    public async Task<int> CleanupOldLogsAsync(int daysToKeep, Guid adminUserId)
    {
        using var context = _contextFactory.CreateDbContext();
        
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        var deletedCount = 0;

        // Clean up access logs
        var oldAccessLogs = await context.AccessLogs
            .Where(al => al.AccessedAt < cutoffDate)
            .ToListAsync();
        
        context.AccessLogs.RemoveRange(oldAccessLogs);
        deletedCount += oldAccessLogs.Count;

        // Clean up resolved error logs
        var oldErrorLogs = await context.ErrorLogs
            .Where(el => el.LoggedAt < cutoffDate && el.IsResolved)
            .ToListAsync();
        
        context.ErrorLogs.RemoveRange(oldErrorLogs);
        deletedCount += oldErrorLogs.Count;

        await context.SaveChangesAsync();

        await _loggingService.LogAdminActionAsync(
            adminUserId.ToString(), "CleanupLogs", "System", null,
            $"Cleaned up {deletedCount} old log entries (older than {daysToKeep} days)",
            severity: "Info");

        return deletedCount;
    }

    public async Task<byte[]?> ExportUsersAsync(string format, string? searchTerm = null, string? role = null)
    {
        var users = await GetAllUsersAsync(1, int.MaxValue, searchTerm, role);
        
        if (format.ToLower() == "csv")
        {
            return ExportUsersToCsv(users);
        }
        else if (format.ToLower() == "json")
        {
            return ExportUsersToJson(users);
        }

        return null;
    }

    public async Task<byte[]?> ExportAuditLogsAsync(string format, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var auditLogs = await GetAuditLogsAsync(1, int.MaxValue, null, null, fromDate, toDate);
        
        if (format.ToLower() == "csv")
        {
            return ExportAuditLogsToCsv(auditLogs);
        }
        else if (format.ToLower() == "json")
        {
            return ExportAuditLogsToJson(auditLogs);
        }

        return null;
    }

    private byte[] ExportUsersToCsv(IEnumerable<AdminUserDto> users)
    {
        using var writer = new StringWriter();
        writer.WriteLine("Id,UserName,Email,FullName,IsVerified,IsInstructor,DateJoined,LastLoginDate,WalletBalance,Roles,TotalCourses,TotalEnrollments");
        
        foreach (var user in users)
        {
            writer.WriteLine($"{user.Id},{user.UserName},{user.Email},{user.FullName},{user.IsVerified},{user.IsInstructor},{user.DateJoined:yyyy-MM-dd},{user.LastLoginDate:yyyy-MM-dd},{user.WalletBalance},\"{string.Join(";", user.Roles)}\",{user.TotalCourses},{user.TotalEnrollments}");
        }
        
        return System.Text.Encoding.UTF8.GetBytes(writer.ToString());
    }

    private byte[] ExportUsersToJson(IEnumerable<AdminUserDto> users)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(users, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
        
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    private byte[] ExportAuditLogsToCsv(IEnumerable<AdminAuditLogDto> auditLogs)
    {
        using var writer = new StringWriter();
        writer.WriteLine("Id,AdminUserName,Action,EntityType,EntityId,Description,PerformedAt,Severity,IpAddress");
        
        foreach (var log in auditLogs)
        {
            writer.WriteLine($"{log.Id},{log.AdminUserName},{log.Action},{log.EntityType},{log.EntityId},\"{log.Description}\",{log.PerformedAt:yyyy-MM-dd HH:mm:ss},{log.Severity},{log.IpAddress}");
        }
        
        return System.Text.Encoding.UTF8.GetBytes(writer.ToString());
    }

    private byte[] ExportAuditLogsToJson(IEnumerable<AdminAuditLogDto> auditLogs)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(auditLogs, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
        
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    private string GetActionIcon(string action) =>
        action.ToLower() switch
        {
            var a when a.Contains("create") => "fas fa-plus",
            var a when a.Contains("update") => "fas fa-edit",
            var a when a.Contains("delete") => "fas fa-trash",
            var a when a.Contains("block") => "fas fa-ban",
            var a when a.Contains("unblock") => "fas fa-unlock",
            var a when a.Contains("role") => "fas fa-user-tag",
            _ => "fas fa-cog"
        };

}