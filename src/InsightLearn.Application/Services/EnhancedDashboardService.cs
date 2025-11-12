using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsightLearn.Core.DTOs.Admin;
using InsightLearn.Core.Entities;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace InsightLearn.Application.Services
{
    public interface IEnhancedDashboardService
    {
        Task<EnhancedDashboardStatsDto> GetEnhancedStatsAsync();
        Task<ChartDataDto> GetChartDataAsync(string chartType, int days);
        Task<PagedResult<ActivityItemDto>> GetRecentActivityAsync(int limit, int offset);
        Task<RealTimeMetricsDto> GetRealTimeMetricsAsync();
    }

    public class EnhancedDashboardService : IEnhancedDashboardService
    {
        private readonly InsightLearnDbContext _dbContext;
        private readonly IMemoryCache _cache;
        private readonly ILogger<EnhancedDashboardService> _logger;
        private readonly IMongoClient _mongoClient;
        private readonly string _connectionString;
        private const string CACHE_KEY_STATS = "enhanced_dashboard_stats";
        private const string CACHE_KEY_REALTIME = "realtime_metrics";
        private const int CACHE_DURATION_MINUTES = 5;
        private const int REALTIME_CACHE_SECONDS = 30;

        public EnhancedDashboardService(
            InsightLearnDbContext dbContext,
            IMemoryCache cache,
            ILogger<EnhancedDashboardService> logger,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _cache = cache;
            _logger = logger;

            // Initialize MongoDB connection
            var mongoConnectionString = configuration["MongoDb:ConnectionString"]
                ?? "mongodb://admin:Admin123!@mongodb:27017/insightlearn_videos?authSource=admin";
            _mongoClient = new MongoClient(mongoConnectionString);
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        }

        public async Task<EnhancedDashboardStatsDto> GetEnhancedStatsAsync()
        {
            // Try to get from cache first
            if (_cache.TryGetValue(CACHE_KEY_STATS, out EnhancedDashboardStatsDto? cachedStats))
            {
                if (cachedStats != null)
                {
                    _logger.LogDebug("Returning cached dashboard stats");
                    return cachedStats;
                }
            }

            _logger.LogInformation("Generating enhanced dashboard statistics");

            var stats = new EnhancedDashboardStatsDto
            {
                LastUpdated = DateTime.UtcNow
            };

            try
            {
                var now = DateTime.UtcNow;
                var weekAgo = now.AddDays(-7);
                var monthAgo = now.AddDays(-30);
                var todayStart = now.Date;
                var weekStart = now.AddDays(-(int)now.DayOfWeek);
                var monthStart = new DateTime(now.Year, now.Month, 1);

                // User Metrics
                stats.TotalUsers = await _dbContext.Users.CountAsync();
                stats.NewUsersToday = await _dbContext.Users
                    .CountAsync(u => u.DateJoined >= todayStart);
                stats.NewUsersThisWeek = await _dbContext.Users
                    .CountAsync(u => u.DateJoined >= weekStart);
                stats.NewUsersThisMonth = await _dbContext.Users
                    .CountAsync(u => u.DateJoined >= monthStart);

                // Calculate user trend
                var previousWeekUsers = await _dbContext.Users
                    .CountAsync(u => u.DateJoined < weekStart && u.DateJoined >= weekStart.AddDays(-7));
                stats.UsersTrend = CalculateTrend(stats.NewUsersThisWeek, previousWeekUsers, "week");

                // Active users (logged in within last hour)
                var oneHourAgo = now.AddHours(-1);
                stats.ActiveUsersNow = await _dbContext.Users
                    .CountAsync(u => u.LastLoginDate != null && u.LastLoginDate >= oneHourAgo);

                // Instructor Metrics
                stats.TotalInstructors = await _dbContext.Users
                    .CountAsync(u => u.IsInstructor);
                stats.ActiveInstructors = stats.TotalInstructors; // For now, all instructors are active
                stats.PendingInstructorApplications = 0; // Placeholder

                // Calculate instructor trend
                var previousWeekInstructors = await _dbContext.Users
                    .CountAsync(u => u.IsInstructor &&
                                u.DateJoined < weekStart && u.DateJoined >= weekStart.AddDays(-7));
                stats.InstructorsTrend = CalculateTrend(
                    stats.TotalInstructors - previousWeekInstructors,
                    previousWeekInstructors,
                    "week");

                // Course Metrics (currently 0 but structure in place)
                stats.TotalCourses = await _dbContext.Courses.CountAsync();
                stats.PublishedCourses = await _dbContext.Courses.CountAsync(c => c.Status == CourseStatus.Published);
                stats.DraftCourses = await _dbContext.Courses.CountAsync(c => c.Status == CourseStatus.Draft);
                stats.ArchivedCourses = await _dbContext.Courses.CountAsync(c => c.Status == CourseStatus.Archived);

                // Enrollment Metrics (placeholder)
                stats.TotalEnrollments = await _dbContext.Enrollments.CountAsync();
                stats.ActiveEnrollments = await _dbContext.Enrollments
                    .CountAsync(e => e.Status == EnrollmentStatus.Active);
                stats.CompletedEnrollments = await _dbContext.Enrollments
                    .CountAsync(e => e.Status == EnrollmentStatus.Completed);
                stats.DroppedEnrollments = await _dbContext.Enrollments
                    .CountAsync(e => e.Status == EnrollmentStatus.Cancelled || e.Status == EnrollmentStatus.Suspended);

                // Revenue Metrics (placeholder - need payment table)
                stats.TotalRevenue = 0m;
                stats.RevenueToday = 0m;
                stats.RevenueThisWeek = 0m;
                stats.RevenueThisMonth = 0m;
                stats.AverageOrderValue = 0m;
                stats.RevenueTrend = new TrendData { Period = "month" };

                // Platform Health
                stats.PlatformStatus = "healthy";
                stats.AverageResponseTime = 125.5; // ms - placeholder
                stats.Uptime = 99.9; // percentage
                stats.ErrorsLast24Hours = 0;

                // Storage Metrics
                stats.Storage = await CalculateStorageMetrics();

                // Cache the results
                _cache.Set(CACHE_KEY_STATS, stats, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating enhanced dashboard stats");
                // Return stats with whatever data we have
                return stats;
            }
        }

        public async Task<ChartDataDto> GetChartDataAsync(string chartType, int days)
        {
            var chartData = new ChartDataDto
            {
                ChartType = chartType,
                GeneratedAt = DateTime.UtcNow
            };

            var endDate = DateTime.UtcNow.Date.AddDays(1); // Include today
            var startDate = endDate.AddDays(-days);

            try
            {
                switch (chartType.ToLower())
                {
                    case "user-growth":
                        chartData.Title = $"User Growth (Last {days} Days)";
                        chartData = await GenerateUserGrowthChart(startDate, endDate);
                        break;

                    case "revenue":
                    case "revenue-trends":
                        chartData.Title = $"Revenue Trends (Last {days} Days)";
                        chartData = await GenerateRevenueChart(startDate, endDate);
                        break;

                    case "top-courses":
                        chartData.Title = "Top 10 Courses by Enrollment";
                        chartData = await GenerateTopCoursesChart();
                        break;

                    case "usage-heatmap":
                        chartData.Title = "Platform Usage Heatmap";
                        chartData = await GenerateUsageHeatmap();
                        break;

                    case "geographic":
                        chartData.Title = "User Distribution by Country";
                        chartData = await GenerateGeographicChart();
                        break;

                    default:
                        chartData.Title = "Unknown Chart Type";
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating chart data for type: {ChartType}", chartType);
            }

            return chartData;
        }

        public async Task<PagedResult<ActivityItemDto>> GetRecentActivityAsync(int limit, int offset)
        {
            var result = new PagedResult<ActivityItemDto>
            {
                PageSize = limit,
                CurrentPage = (offset / limit) + 1
            };

            try
            {
                var activities = new List<ActivityItemDto>();
                var now = DateTime.UtcNow;

                // Get recent user registrations
                var recentUsers = await _dbContext.Users
                    .OrderByDescending(u => u.DateJoined)
                    .Skip(offset)
                    .Take(Math.Min(limit / 2, 10))
                    .Select(u => new ActivityItemDto
                    {
                        Type = ActivityType.UserRegistered,
                        Title = "New User Registration",
                        Description = $"{u.FirstName} {u.LastName} joined the platform",
                        EntityId = u.Id.ToString(),
                        EntityType = "User",
                        UserId = u.Id.ToString(),
                        UserName = $"{u.FirstName} {u.LastName}",
                        Timestamp = u.DateJoined,
                        Severity = ActivitySeverity.Success
                    })
                    .ToListAsync();

                activities.AddRange(recentUsers);

                // Get recent logins
                var recentLogins = await _dbContext.Users
                    .Where(u => u.LastLoginDate != null)
                    .OrderByDescending(u => u.LastLoginDate)
                    .Take(Math.Min(limit / 4, 5))
                    .Select(u => new ActivityItemDto
                    {
                        Type = ActivityType.UserLoggedIn,
                        Title = "User Login",
                        Description = $"{u.FirstName} {u.LastName} logged in",
                        EntityId = u.Id.ToString(),
                        EntityType = "User",
                        UserId = u.Id.ToString(),
                        UserName = $"{u.FirstName} {u.LastName}",
                        Timestamp = u.LastLoginDate ?? DateTime.UtcNow,
                        Severity = ActivitySeverity.Info
                    })
                    .ToListAsync();

                activities.AddRange(recentLogins);

                // Get recent chatbot messages (system activity)
                var recentChats = await _dbContext.ChatbotMessages
                    .OrderByDescending(c => c.Timestamp)
                    .Take(Math.Min(limit / 4, 5))
                    .Select(c => new ActivityItemDto
                    {
                        Type = ActivityType.SystemInfo,
                        Title = "Chatbot Interaction",
                        Description = $"User interacted with AI assistant",
                        EntityId = c.Id.ToString(),
                        EntityType = "ChatMessage",
                        UserId = null, // ChatbotMessage doesn't have UserId, only SessionId
                        Timestamp = c.Timestamp,
                        Severity = ActivitySeverity.Info
                    })
                    .ToListAsync();

                activities.AddRange(recentChats);

                // Add some mock data for demonstration
                if (activities.Count < limit)
                {
                    activities.Add(new ActivityItemDto
                    {
                        Type = ActivityType.CoursePublished,
                        Title = "Course Published",
                        Description = "Introduction to Machine Learning was published",
                        Timestamp = now.AddHours(-2),
                        Severity = ActivitySeverity.Success
                    });

                    activities.Add(new ActivityItemDto
                    {
                        Type = ActivityType.PaymentReceived,
                        Title = "Payment Received",
                        Description = "$99.99 payment processed successfully",
                        Timestamp = now.AddHours(-5),
                        Severity = ActivitySeverity.Success
                    });

                    activities.Add(new ActivityItemDto
                    {
                        Type = ActivityType.VideoUploaded,
                        Title = "Video Uploaded",
                        Description = "New lesson video uploaded (45MB)",
                        Timestamp = now.AddHours(-8),
                        Severity = ActivitySeverity.Info
                    });
                }

                // Sort all activities by timestamp
                result.Items = activities
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .ToList();

                result.TotalCount = activities.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activity");
            }

            return result;
        }

        public async Task<RealTimeMetricsDto> GetRealTimeMetricsAsync()
        {
            // Check cache first
            if (_cache.TryGetValue(CACHE_KEY_REALTIME, out RealTimeMetricsDto? cached))
            {
                if (cached != null)
                {
                    return cached;
                }
            }

            var metrics = new RealTimeMetricsDto
            {
                Timestamp = DateTime.UtcNow
            };

            try
            {
                var oneHourAgo = DateTime.UtcNow.AddHours(-1);

                // Active users
                metrics.ActiveUsers = await _dbContext.Users
                    .CountAsync(u => u.LastLoginDate != null && u.LastLoginDate >= oneHourAgo);

                metrics.ActiveSessions = metrics.ActiveUsers; // Simplified for now
                metrics.RequestsPerSecond = Random.Shared.Next(5, 50); // Mock data
                metrics.AverageResponseTime = Random.Shared.Next(50, 200); // Mock data

                // Active users by page (mock data)
                metrics.ActiveUsersByPage = new Dictionary<string, int>
                {
                    { "/", 12 },
                    { "/courses", 8 },
                    { "/dashboard", 3 },
                    { "/profile", 2 }
                };

                // System health checks
                metrics.SystemHealth = await CheckSystemHealth();

                // Cache for short duration
                _cache.Set(CACHE_KEY_REALTIME, metrics, TimeSpan.FromSeconds(REALTIME_CACHE_SECONDS));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting real-time metrics");
            }

            return metrics;
        }

        private TrendData CalculateTrend(int current, int previous, string period)
        {
            var trend = new TrendData
            {
                Value = current,
                PreviousValue = previous,
                Period = period
            };

            trend.Change = current - previous;
            if (previous > 0)
            {
                trend.ChangePercentage = ((double)trend.Change / previous) * 100;
            }
            else if (current > 0)
            {
                trend.ChangePercentage = 100;
            }

            return trend;
        }

        private async Task<StorageMetrics> CalculateStorageMetrics()
        {
            var metrics = new StorageMetrics();

            try
            {
                // Get MongoDB storage info
                var database = _mongoClient.GetDatabase("insightlearn_videos");
                var bucket = new GridFSBucket(database);

                var filter = Builders<MongoDB.Driver.GridFS.GridFSFileInfo>.Filter.Empty;
                var files = await bucket.FindAsync(filter);
                var filesList = await files.ToListAsync();

                metrics.TotalVideos = filesList.Count;
                metrics.MongoDbUsedBytes = filesList.Sum(f => f.Length);

                // SQL Server size (mock for now - would need sys tables query)
                metrics.SqlServerUsedBytes = 50 * 1024 * 1024; // 50MB mock

                metrics.TotalUsedBytes = metrics.MongoDbUsedBytes + metrics.SqlServerUsedBytes;
                metrics.TotalAvailableBytes = 10L * 1024 * 1024 * 1024; // 10GB mock
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not calculate storage metrics");
            }

            return metrics;
        }

        private async Task<ChartDataDto> GenerateUserGrowthChart(DateTime startDate, DateTime endDate)
        {
            var chart = new ChartDataDto
            {
                ChartType = "line",
                Title = "User Growth",
                GeneratedAt = DateTime.UtcNow
            };

            try
            {
                var dailyUsers = await _dbContext.Users
                    .Where(u => u.DateJoined >= startDate && u.DateJoined <= endDate)
                    .GroupBy(u => u.DateJoined.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                // Fill in missing days with 0
                var currentDate = startDate.Date;
                var cumulativeCount = await _dbContext.Users.CountAsync(u => u.DateJoined < startDate);

                while (currentDate <= endDate.Date)
                {
                    var dayCount = dailyUsers.FirstOrDefault(d => d.Date == currentDate)?.Count ?? 0;
                    cumulativeCount += dayCount;

                    chart.DataPoints.Add(new DataPoint
                    {
                        Label = currentDate.ToString("MMM dd"),
                        Value = cumulativeCount,
                        Date = currentDate
                    });

                    currentDate = currentDate.AddDays(1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating user growth chart");
            }

            return chart;
        }

        private async Task<ChartDataDto> GenerateRevenueChart(DateTime startDate, DateTime endDate)
        {
            var chart = new ChartDataDto
            {
                ChartType = "area",
                Title = "Revenue Trends",
                GeneratedAt = DateTime.UtcNow
            };

            // Mock data for now since we don't have payment data yet
            var currentDate = startDate.Date;
            var random = new Random(42); // Seed for consistent mock data

            while (currentDate <= endDate.Date)
            {
                chart.DataPoints.Add(new DataPoint
                {
                    Label = currentDate.ToString("MMM dd"),
                    Value = (decimal)(random.NextDouble() * 1000 + 500), // $500-$1500 per day
                    Date = currentDate
                });

                currentDate = currentDate.AddDays(1);
            }

            return await Task.FromResult(chart);
        }

        private async Task<ChartDataDto> GenerateTopCoursesChart()
        {
            var chart = new ChartDataDto
            {
                ChartType = "bar",
                Title = "Top Courses",
                GeneratedAt = DateTime.UtcNow
            };

            // Mock data for demonstration
            var mockCourses = new[]
            {
                ("Introduction to Machine Learning", 245),
                ("Web Development Bootcamp", 189),
                ("Data Science Fundamentals", 156),
                ("Cloud Computing Essentials", 134),
                ("Mobile App Development", 98),
                ("Python for Beginners", 87),
                ("JavaScript Mastery", 76),
                ("DevOps Practices", 65),
                ("AI and Deep Learning", 54),
                ("Cybersecurity Basics", 43)
            };

            foreach (var (name, enrollments) in mockCourses)
            {
                chart.DataPoints.Add(new DataPoint
                {
                    Label = name,
                    Value = enrollments
                });
            }

            return await Task.FromResult(chart);
        }

        private async Task<ChartDataDto> GenerateUsageHeatmap()
        {
            var chart = new ChartDataDto
            {
                ChartType = "heatmap",
                Title = "Platform Usage Heatmap",
                GeneratedAt = DateTime.UtcNow
            };

            // Generate mock heatmap data (24 hours x 7 days)
            var random = new Random(42);
            var days = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

            for (int hour = 0; hour < 24; hour++)
            {
                chart.Labels.Add($"{hour:00}:00");
            }

            foreach (var day in days)
            {
                var series = new DataSeries
                {
                    Name = day,
                    Values = new List<decimal>()
                };

                for (int hour = 0; hour < 24; hour++)
                {
                    // Higher values during business hours on weekdays
                    var baseValue = (day == "Sat" || day == "Sun") ? 20 : 50;
                    if (hour >= 9 && hour <= 17)
                        baseValue *= 2;

                    series.Values.Add(baseValue + random.Next(-10, 30));
                }

                chart.Series.Add(series);
            }

            return await Task.FromResult(chart);
        }

        private async Task<ChartDataDto> GenerateGeographicChart()
        {
            var chart = new ChartDataDto
            {
                ChartType = "geographic",
                Title = "User Distribution",
                GeneratedAt = DateTime.UtcNow
            };

            // Mock geographic data
            var countries = new[]
            {
                ("United States", "US", 450),
                ("United Kingdom", "GB", 230),
                ("Canada", "CA", 180),
                ("Germany", "DE", 120),
                ("Australia", "AU", 95)
            };

            foreach (var (country, code, users) in countries)
            {
                chart.DataPoints.Add(new DataPoint
                {
                    Label = country,
                    Value = users,
                    Category = code
                });
            }

            return await Task.FromResult(chart);
        }

        private async Task<SystemHealthStatus> CheckSystemHealth()
        {
            var health = new SystemHealthStatus();

            try
            {
                // Check API health (always healthy if we're here)
                health.ApiStatus = new ServiceStatus
                {
                    Name = "API",
                    IsHealthy = true,
                    ResponseTime = 5,
                    LastChecked = DateTime.UtcNow
                };

                // Check database
                try
                {
                    var start = DateTime.UtcNow;
                    await _dbContext.Database.CanConnectAsync();
                    health.DatabaseStatus = new ServiceStatus
                    {
                        Name = "SQL Server",
                        IsHealthy = true,
                        ResponseTime = (DateTime.UtcNow - start).TotalMilliseconds,
                        LastChecked = DateTime.UtcNow
                    };
                }
                catch (Exception ex)
                {
                    health.DatabaseStatus = new ServiceStatus
                    {
                        Name = "SQL Server",
                        IsHealthy = false,
                        ErrorMessage = ex.Message,
                        LastChecked = DateTime.UtcNow
                    };
                }

                // Check MongoDB
                try
                {
                    var start = DateTime.UtcNow;
                    var database = _mongoClient.GetDatabase("insightlearn_videos");
                    await database.ListCollectionNamesAsync();
                    health.MongoDbStatus = new ServiceStatus
                    {
                        Name = "MongoDB",
                        IsHealthy = true,
                        ResponseTime = (DateTime.UtcNow - start).TotalMilliseconds,
                        LastChecked = DateTime.UtcNow
                    };
                }
                catch (Exception ex)
                {
                    health.MongoDbStatus = new ServiceStatus
                    {
                        Name = "MongoDB",
                        IsHealthy = false,
                        ErrorMessage = ex.Message,
                        LastChecked = DateTime.UtcNow
                    };
                }

                // Mock CPU and Memory usage
                health.CpuUsage = Random.Shared.Next(10, 60);
                health.MemoryUsage = Random.Shared.Next(30, 70);
                health.MemoryUsedBytes = (long)(health.MemoryUsage * 10737418.24); // ~1GB at 100%
                health.MemoryTotalBytes = 1073741824; // 1GB
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking system health");
            }

            return health;
        }
    }
}