namespace InsightLearn.Application.Interfaces;

/// <summary>
/// Service interface for generating platform reports.
/// Part of Admin Console v2.1.0-dev.
/// </summary>
public interface IReportService
{
    Task<ReportResult> GenerateRevenueReportAsync(DateTime startDate, DateTime endDate);
    Task<ReportResult> GenerateUserGrowthReportAsync(DateTime startDate, DateTime endDate);
    Task<ReportResult> GenerateCoursePerformanceReportAsync(DateTime startDate, DateTime endDate);
    Task<ReportResult> GenerateEnrollmentReportAsync(DateTime startDate, DateTime endDate);
    Task<ReportResult> GenerateEngagementReportAsync(DateTime startDate, DateTime endDate);
    Task<ReportResult> GenerateInstructorEarningsReportAsync(DateTime startDate, DateTime endDate);
    Task<byte[]> ExportToCsvAsync(ReportResult report);
    Task<byte[]> ExportToPdfAsync(ReportResult report);
    Task<byte[]> ExportToExcelAsync(ReportResult report);
}

/// <summary>
/// Report data result with metrics, columns, and rows.
/// </summary>
public class ReportResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Completed";
    public List<ReportMetric> KeyMetrics { get; set; } = new();
    public List<string> Columns { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
}

/// <summary>
/// Key metric for report summary cards.
/// </summary>
public class ReportMetric
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string? Change { get; set; }
    public bool IsPositive { get; set; }
}
