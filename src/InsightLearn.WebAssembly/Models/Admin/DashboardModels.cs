namespace InsightLearn.WebAssembly.Models.Admin;

public class DashboardStats
{
    public int TotalUsers { get; set; }
    public int TotalCourses { get; set; }
    public int TotalEnrollments { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalActiveUsers { get; set; }
    public int ActiveStudents { get; set; }
    public int ActiveInstructors { get; set; }
    public int TotalInstructors { get; set; }
    public int TotalStudents { get; set; }
    public int PublishedCourses { get; set; }
    public int DraftCourses { get; set; }
    public int TotalPayments { get; set; }
}

public class RecentActivity
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Icon { get; set; } = "info-circle";
    public string Severity { get; set; } = "Info";
}
