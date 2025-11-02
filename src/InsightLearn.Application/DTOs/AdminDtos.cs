using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Application.DTOs;

public class AdminUserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public bool IsInstructor { get; set; }
    public DateTime DateJoined { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public decimal WalletBalance { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public List<string> Roles { get; set; } = new();
    public int TotalCourses { get; set; }
    public int TotalEnrollments { get; set; }
}

public class UpdateUserDto
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    public bool IsVerified { get; set; }
    public bool IsInstructor { get; set; }
    public decimal WalletBalance { get; set; }
}

public class AdminDashboardDto
{
    public int TotalUsers { get; set; }
    public int TotalActiveUsers { get; set; }
    public int TotalInstructors { get; set; }
    public int TotalStudents { get; set; }
    public int TotalCourses { get; set; }
    public int TotalEnrollments { get; set; }
    public int TotalPayments { get; set; }
    public decimal TotalRevenue { get; set; }
    
    // Last 24 hours
    public int NewUsersToday { get; set; }
    public int NewCoursesToday { get; set; }
    public int NewEnrollmentsToday { get; set; }
    public int ErrorsToday { get; set; }
    
    // Growth rates
    public double UserGrowthRate { get; set; }
    public double CourseGrowthRate { get; set; }
    public double RevenueGrowthRate { get; set; }
    
    // SEO Statistics
    public int SeoOptimizedPages { get; set; }
    public int SeoTotalPages { get; set; }
    public double SeoAverageScore { get; set; }
    public int SeoIssuesCount { get; set; }
    
    // Recent statistics
    public List<DailyStatsDto> DailyStats { get; set; } = new();
}

public class DailyStatsDto
{
    public DateTime Date { get; set; }
    public int NewUsers { get; set; }
    public int NewCourses { get; set; }
    public int NewEnrollments { get; set; }
    public decimal Revenue { get; set; }
    public int Errors { get; set; }
}

public class SystemHealthDto
{
    public bool DatabaseConnected { get; set; }
    public bool RedisConnected { get; set; }
    public bool ElasticsearchConnected { get; set; }
    public string DatabaseResponseTime { get; set; } = string.Empty;
    public int ActiveSessions { get; set; }
    public long MemoryUsage { get; set; }
    public double CpuUsage { get; set; }
    public List<SystemAlert> Alerts { get; set; } = new();
}

public class SystemAlert
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class RecentActivityDto
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
}

public class AccessLogDto
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string RequestPath { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long? ResponseTimeMs { get; set; }
    public DateTime AccessedAt { get; set; }
    public string? UserAgent { get; set; }
}

public class ErrorLogDto
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public string ExceptionMessage { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? RequestPath { get; set; }
    public string? HttpMethod { get; set; }
    public string Severity { get; set; } = string.Empty;
    public DateTime LoggedAt { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedByUserName { get; set; }
    public string? ResolutionNotes { get; set; }
}

public class AdminAuditLogDto
{
    public Guid Id { get; set; }
    public string AdminUserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? Description { get; set; }
    public DateTime PerformedAt { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
}

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UserCount { get; set; }
    public bool IsSystemRole { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<string> Permissions { get; set; } = new();
}

// Additional DTOs for admin panel functionality

public class AccessStatsDto
{
    public int TotalRequestsToday { get; set; }
    public decimal RequestGrowth { get; set; }
    public int UniqueVisitorsToday { get; set; }
    public int AverageResponseTime { get; set; }
    public int FailedRequestsToday { get; set; }
    public decimal ErrorRate { get; set; }
}

public class AuditLogDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string IpAddress { get; set; } = "";
    public string UserAgent { get; set; } = "";
    public string? Details { get; set; }
    public string Severity { get; set; } = "Info";
}

// System Health DTOs
public class SystemStatusDto
{
    public string OverallStatus { get; set; } = "";
    public string Uptime { get; set; } = "";
    public DateTime LastChecked { get; set; }
    public List<string> Issues { get; set; } = new();
}

public class SystemMetricsDto
{
    public double CpuUsagePercent { get; set; }
    public double MemoryUsagePercent { get; set; }
    public double DiskUsagePercent { get; set; }
    public long TotalMemoryMB { get; set; }
    public long AvailableMemoryMB { get; set; }
    public long DiskTotalGB { get; set; }
    public long DiskAvailableGB { get; set; }
}

public class PerformanceMetricsDto
{
    public int AverageResponseTime { get; set; }
    public int RequestsPerSecond { get; set; }
    public double ErrorRate { get; set; }
    public int ActiveConnections { get; set; }
    public int DatabasePoolUsage { get; set; }
    public double CacheHitRatio { get; set; }
    public int QueueLength { get; set; }
    public double ThroughputPerMinute { get; set; }
}

public class SystemInfoDto
{
    public string ApplicationVersion { get; set; } = "";
    public string Environment { get; set; } = "";
    public DateTime? BuildDate { get; set; }
    public string DotNetVersion { get; set; } = "";
    public string OperatingSystem { get; set; } = "";
    public int TotalMemoryGB { get; set; }
    public int ProcessorCount { get; set; }
    public string MachineName { get; set; } = "";
    public DateTime StartTime { get; set; }
}

public class HealthCheckDto
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public string ResponseTime { get; set; } = "";
    public string? ErrorMessage { get; set; }
    public DateTime LastChecked { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}

public class AlertDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Severity { get; set; } = "";
    public string Message { get; set; } = "";
    public string Source { get; set; } = "";
    public string? Category { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }
}

// Reports DTOs
public class ReportDataDto
{
    public decimal TotalRevenue { get; set; }
    public decimal RevenueGrowth { get; set; }
    public int NewEnrollments { get; set; }
    public decimal EnrollmentGrowth { get; set; }
    public int ActiveUsers { get; set; }
    public decimal UserGrowth { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal CompletionRateChange { get; set; }
    public int TotalCourses { get; set; }
    public int PublishedCourses { get; set; }
    public DateTime ReportDate { get; set; }
    public string Period { get; set; } = "";
}

public class TopCourseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string Instructor { get; set; } = "";
    public int EnrollmentCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal AverageRating { get; set; }
    public decimal CompletionRate { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class DetailedReportDto
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = "";
    public string Category { get; set; } = "";
    public string InstructorName { get; set; } = "";
    public int EnrollmentCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastUpdated { get; set; }
    public string Status { get; set; } = "";
}

// User Creation DTOs
public class CreateUserDto
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, ErrorMessage = "First name must be less than 50 characters")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, ErrorMessage = "Last name must be less than 50 characters")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(100, ErrorMessage = "Email must be less than 100 characters")]
    public string Email { get; set; } = "";

    [Phone(ErrorMessage = "Please enter a valid phone number")]
    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Please select a role")]
    public string Role { get; set; } = "";

    [Range(0, double.MaxValue, ErrorMessage = "Wallet balance must be a positive number")]
    public decimal WalletBalance { get; set; }

    public bool EmailConfirmed { get; set; }
    public bool IsActive { get; set; }
    public bool SendWelcomeEmail { get; set; }

    [StringLength(500, ErrorMessage = "Bio must be less than 500 characters")]
    public string? Bio { get; set; }

    [Url(ErrorMessage = "Please enter a valid URL")]
    public string? ProfilePictureUrl { get; set; }

    [Url(ErrorMessage = "Please enter a valid URL")]
    public string? Website { get; set; }

    [StringLength(100, ErrorMessage = "Company name must be less than 100 characters")]
    public string? Company { get; set; }

    [StringLength(100, ErrorMessage = "Job title must be less than 100 characters")]
    public string? JobTitle { get; set; }

    public string? Country { get; set; }
}

public class UserCreatedDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
    public bool EmailConfirmed { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Pagination DTOs
public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

// Filter DTOs
public class AdminFilterDto
{
    public string? SearchTerm { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Status { get; set; }
    public string? Category { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}