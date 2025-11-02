namespace InsightLearn.Core.Entities;

public class EnterpriseAnalyticsSummary
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    
    public string Period { get; set; } = string.Empty;
    
    public string Summary { get; set; } = string.Empty;
    
    public LoginAnalytics? LoginAnalytics { get; set; }
    
    public ErrorAnalytics? ErrorAnalytics { get; set; }
    
    public PerformanceAnalytics? PerformanceAnalytics { get; set; }
    
    public SecurityAnalytics? SecurityAnalytics { get; set; }
    
    public BusinessMetrics? BusinessMetrics { get; set; }
}

public class LoginAnalytics
{
    public int TotalAttempts { get; set; }
    public int SuccessfulLogins { get; set; }
    public int FailedLogins { get; set; }  // Keep for backward compatibility
    public decimal SuccessRate { get; set; }  // Changed from computed double to decimal
    public Dictionary<string, int> LoginsByMethod { get; set; } = new();
    public Dictionary<DateTime, int> LoginsByHour { get; set; } = new();
}

public class ErrorAnalytics
{
    public int TotalErrors { get; set; }
    public int ResolvedErrors { get; set; }
    public int PendingErrors { get; set; }  // Keep for backward compatibility
    public decimal ResolutionRate { get; set; }  // Changed from computed double to decimal
    public Dictionary<string, int> ErrorsByType { get; set; } = new();
    public Dictionary<DateTime, int> ErrorsByHour { get; set; } = new();
}

public class PerformanceAnalytics
{
    public double AverageResponseTimeMs { get; set; }
    public double MaxResponseTimeMs { get; set; }
    public double MinResponseTimeMs { get; set; }
    public int TotalRequests { get; set; }
    public Dictionary<string, double> EndpointResponseTimes { get; set; } = new();
    public Dictionary<DateTime, double> ResponseTimesByHour { get; set; } = new();
}

public class SecurityAnalytics
{
    public int TotalSecurityEvents { get; set; }
    public int CriticalEvents { get; set; }
    public int WarningEvents { get; set; }
    public int InfoEvents { get; set; }
    public Dictionary<string, int> EventsByType { get; set; } = new();
    public Dictionary<DateTime, int> EventsByHour { get; set; } = new();
}

public class BusinessMetrics
{
    public int ActiveUsers { get; set; }
    public int NewRegistrations { get; set; }
    public int TotalCourseEnrollments { get; set; }
    public int CompletedCourses { get; set; }
    public double UserRetentionRate { get; set; }  // Keep as double for entity
    public decimal TotalRevenue { get; set; }
    public Dictionary<string, int> PopularCourses { get; set; } = new();
}