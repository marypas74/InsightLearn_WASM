using InsightLearn.Application.DTOs;
using InsightLearn.Application.Interfaces;
using InsightLearn.Core.Entities;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using System.Globalization;

namespace InsightLearn.Application.Services;

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly IAnalyticsService? _analyticsService;
    private readonly IEnterpriseMonitoringService _monitoringService;

    public ExportService(
        ILogger<ExportService> logger,
        IAnalyticsService? analyticsService,
        IEnterpriseMonitoringService monitoringService)
    {
        _logger = logger;
        _analyticsService = analyticsService;
        _monitoringService = monitoringService;
    }

    public async Task<ExportResult> ExportAnalyticsAsync(
        string reportType,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string format = "Excel")
    {
        try
        {
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            return reportType.ToLower() switch
            {
                "performance" => await ExportPerformanceReportAsync(startDate.Value, endDate.Value, format),
                "security" => await ExportSecurityReportAsync(startDate.Value, endDate.Value, format),
                "users" => await ExportUserAnalyticsAsync(startDate.Value, endDate.Value, format),
                "system" => await ExportSystemHealthReportAsync(startDate.Value, endDate.Value, format),
                "comprehensive" => await ExportComprehensiveReportAsync(startDate.Value, endDate.Value, format),
                _ => throw new ArgumentException($"Unknown report type: {reportType}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting analytics report: {ReportType}", reportType);
            return new ExportResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ExportResult> ExportSystemHealthAsync(string format = "PDF")
    {
        try
        {
            var healthStatus = await _monitoringService.GetEnterpriseHealthStatusAsync();
            var performanceHistory = await _monitoringService.GetPerformanceHistoryAsync(24);
            
            return format.ToUpper() switch
            {
                "PDF" => await GenerateSystemHealthPdfAsync(healthStatus, performanceHistory),
                "EXCEL" => await GenerateSystemHealthExcelAsync(healthStatus, performanceHistory),
                "JSON" => await GenerateSystemHealthJsonAsync(healthStatus, performanceHistory),
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting system health report");
            return new ExportResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ExportResult> ExportRealTimeMetricsAsync(int hours = 1, string format = "CSV")
    {
        try
        {
            var metrics = await _monitoringService.GetRealTimeMetricsAsync();
            var performanceHistory = await _monitoringService.GetPerformanceHistoryAsync(hours);
            
            return format.ToUpper() switch
            {
                "CSV" => await GenerateMetricsCsvAsync(metrics, performanceHistory),
                "EXCEL" => await GenerateMetricsExcelAsync(metrics, performanceHistory),
                "JSON" => await GenerateMetricsJsonAsync(metrics, performanceHistory),
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting real-time metrics");
            return new ExportResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ExportResult> ExportAlertsReportAsync(DateTime? startDate = null, DateTime? endDate = null, string format = "PDF")
    {
        try
        {
            var alerts = await _monitoringService.GetActiveAlertsAsync();
            startDate ??= DateTime.UtcNow.AddDays(-7);
            endDate ??= DateTime.UtcNow;
            
            return format.ToUpper() switch
            {
                "PDF" => await GenerateAlertsPdfAsync(alerts, startDate.Value, endDate.Value),
                "EXCEL" => await GenerateAlertsExcelAsync(alerts, startDate.Value, endDate.Value),
                "CSV" => await GenerateAlertsCsvAsync(alerts, startDate.Value, endDate.Value),
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting alerts report");
            return new ExportResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<ExportResult> ExportPerformanceReportAsync(DateTime startDate, DateTime endDate, string format)
    {
        var performanceAnalytics = await _analyticsService.GetPerformanceAnalyticsAsync(startDate, endDate);
        var apiPerformance = await _analyticsService.GetApiPerformanceAnalysisAsync(startDate, endDate);
        var dbPerformance = await _analyticsService.GetDatabasePerformanceAnalysisAsync(startDate, endDate);

        return format.ToUpper() switch
        {
            "EXCEL" => await GeneratePerformanceExcelAsync(performanceAnalytics, apiPerformance, dbPerformance, startDate, endDate),
            "PDF" => await GeneratePerformancePdfAsync(performanceAnalytics, apiPerformance, dbPerformance, startDate, endDate),
            "CSV" => await GeneratePerformanceCsvAsync(performanceAnalytics, apiPerformance, dbPerformance),
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };
    }

    private async Task<ExportResult> ExportSecurityReportAsync(DateTime startDate, DateTime endDate, string format)
    {
        var securityAnalytics = await _analyticsService.GetSecurityAnalyticsAsync(startDate, endDate);
        var threatAnalysis = await _analyticsService.GetThreatAnalysisAsync(startDate, endDate);

        return format.ToUpper() switch
        {
            "EXCEL" => await GenerateSecurityExcelAsync(securityAnalytics, threatAnalysis, startDate, endDate),
            "PDF" => await GenerateSecurityPdfAsync(securityAnalytics, threatAnalysis, startDate, endDate),
            "CSV" => await GenerateSecurityCsvAsync(securityAnalytics, threatAnalysis),
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };
    }

    private async Task<ExportResult> ExportUserAnalyticsAsync(DateTime startDate, DateTime endDate, string format)
    {
        var userAnalytics = await _analyticsService.GetUserActivityAnalyticsAsync(startDate, endDate);
        var sessionAnalytics = await _analyticsService.GetSessionAnalyticsAsync(startDate, endDate);
        var deviceAnalytics = await _analyticsService.GetDeviceAnalyticsAsync(startDate, endDate);

        return format.ToUpper() switch
        {
            "EXCEL" => await GenerateUserAnalyticsExcelAsync(userAnalytics, sessionAnalytics, deviceAnalytics, startDate, endDate),
            "PDF" => await GenerateUserAnalyticsPdfAsync(userAnalytics, sessionAnalytics, deviceAnalytics, startDate, endDate),
            "CSV" => await GenerateUserAnalyticsCsvAsync(userAnalytics, sessionAnalytics, deviceAnalytics),
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };
    }

    private async Task<ExportResult> ExportSystemHealthReportAsync(DateTime startDate, DateTime endDate, string format)
    {
        var healthStatus = await _monitoringService.GetEnterpriseHealthStatusAsync();
        var performanceHistory = await _monitoringService.GetPerformanceHistoryAsync(24);

        return format.ToUpper() switch
        {
            "PDF" => await GenerateSystemHealthPdfAsync(healthStatus, performanceHistory),
            "EXCEL" => await GenerateSystemHealthExcelAsync(healthStatus, performanceHistory),
            "JSON" => await GenerateSystemHealthJsonAsync(healthStatus, performanceHistory),
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };
    }

    private async Task<ExportResult> ExportComprehensiveReportAsync(DateTime startDate, DateTime endDate, string format)
    {
        // Collect all data types
        var healthStatus = await _monitoringService.GetEnterpriseHealthStatusAsync();
        var performanceHistory = await _monitoringService.GetPerformanceHistoryAsync(24);
        var alerts = await _monitoringService.GetActiveAlertsAsync();
        
        // Create a basic analytics summary since the method isn't in interface
        var analyticsSummary = new EnterpriseAnalyticsSummary
        {
            GeneratedAt = DateTime.UtcNow,
            Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
            Summary = "Comprehensive enterprise analytics report"
        };

        return format.ToUpper() switch
        {
            "PDF" => await GenerateComprehensivePdfAsync(analyticsSummary, healthStatus, performanceHistory, alerts, startDate, endDate),
            "EXCEL" => await GenerateComprehensiveExcelAsync(analyticsSummary, healthStatus, performanceHistory, alerts, startDate, endDate),
            _ => throw new ArgumentException($"Unsupported format for comprehensive report: {format}")
        };
    }

    #region PDF Generation Methods

    private async Task<ExportResult> GenerateSystemHealthPdfAsync(EnterpriseHealthStatus healthStatus, object performanceHistoryObj)
    {
        try
        {
            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            // Title
            var title = new Paragraph("System Health Report")
                .SetFontSize(20)
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER);
            document.Add(title);

            // Generation time
            var genTime = new Paragraph($"Generated: {healthStatus.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC")
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.RIGHT);
            document.Add(genTime);

            // Overall status
            var statusColor = healthStatus.OverallStatus switch
            {
                HealthStatus.Healthy => ColorConstants.GREEN,
                HealthStatus.Warning => ColorConstants.ORANGE,
                HealthStatus.Critical => ColorConstants.RED,
                _ => ColorConstants.GRAY
            };

            var statusPara = new Paragraph($"Overall Status: {healthStatus.OverallStatus}")
                .SetFontSize(14)
                .SetBold()
                .SetFontColor(statusColor);
            document.Add(statusPara);

            // Real-time metrics
            if (healthStatus.RealTimeMetrics?.Any() == true)
            {
                document.Add(new Paragraph("Current Metrics").SetFontSize(16).SetBold());
                var metricsTable = new Table(3).SetWidth(UnitValue.CreatePercentValue(100));
                metricsTable.AddHeaderCell("Metric");
                metricsTable.AddHeaderCell("Value");
                metricsTable.AddHeaderCell("Status");

                foreach (var kvp in healthStatus.RealTimeMetrics)
                {
                    var metric = kvp.Value;
                    var status = GetMetricStatus(metric);
                    metricsTable.AddCell(metric.Name ?? "Unknown");
                    metricsTable.AddCell($"{metric.Value:F2} {metric.Unit ?? ""}");
                    metricsTable.AddCell(status.ToString());
                }
                document.Add(metricsTable);
            }

            // Active alerts
            if (healthStatus.ActiveAlerts.Any())
            {
                document.Add(new Paragraph("Active Alerts").SetFontSize(16).SetBold());
                var alertsTable = new Table(4).SetWidth(UnitValue.CreatePercentValue(100));
                alertsTable.AddHeaderCell("Severity");
                alertsTable.AddHeaderCell("Title");
                alertsTable.AddHeaderCell("Description");
                alertsTable.AddHeaderCell("Triggered");

                foreach (var alert in healthStatus.ActiveAlerts.Take(10))
                {
                    alertsTable.AddCell(alert.Severity.ToString());
                    alertsTable.AddCell(alert.Title);
                    alertsTable.AddCell(alert.Description);
                    alertsTable.AddCell(alert.TriggeredAt.ToString("yyyy-MM-dd HH:mm"));
                }
                document.Add(alertsTable);
            }

            // Recommendations
            if (healthStatus.Recommendations.Any())
            {
                document.Add(new Paragraph("Recommendations").SetFontSize(16).SetBold());
                foreach (var recommendation in healthStatus.Recommendations)
                {
                    document.Add(new Paragraph($"• {recommendation}").SetMarginLeft(20));
                }
            }

            document.Close();

            return new ExportResult
            {
                Success = true,
                Data = stream.ToArray(),
                FileName = $"system_health_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf",
                ContentType = "application/pdf"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating system health PDF");
            return new ExportResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<ExportResult> GenerateComprehensivePdfAsync(
        EnterpriseAnalyticsSummary analyticsSummary,
        EnterpriseHealthStatus healthStatus,
        object performanceHistoryObj,
        IEnumerable<InsightLearn.Core.Entities.SystemAlert> alerts,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            // Title page
            var title = new Paragraph("Comprehensive Enterprise Report")
                .SetFontSize(24)
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER);
            document.Add(title);

            var subtitle = new Paragraph($"Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}")
                .SetFontSize(16)
                .SetTextAlignment(TextAlignment.CENTER);
            document.Add(subtitle);

            var genTime = new Paragraph($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER);
            document.Add(genTime);

            document.Add(new AreaBreak());

            // Executive Summary
            document.Add(new Paragraph("Executive Summary").SetFontSize(18).SetBold());
            document.Add(new Paragraph(analyticsSummary.Summary));

            // System Health Section
            document.Add(new Paragraph("System Health Status").SetFontSize(18).SetBold());
            document.Add(new Paragraph($"Overall Status: {healthStatus.OverallStatus}"));
            document.Add(new Paragraph($"Active Alerts: {healthStatus.ActiveAlertsCount}"));

            // Performance Metrics
            if (analyticsSummary.PerformanceAnalytics != null)
            {
                document.Add(new Paragraph("Performance Analytics").SetFontSize(18).SetBold());
                document.Add(new Paragraph($"Average Response Time: {analyticsSummary.PerformanceAnalytics.AverageResponseTimeMs:F2}ms"));
                document.Add(new Paragraph($"Max Response Time: {analyticsSummary.PerformanceAnalytics.MaxResponseTimeMs:F2}ms"));
                document.Add(new Paragraph($"Min Response Time: {analyticsSummary.PerformanceAnalytics.MinResponseTimeMs:F2}ms"));
                document.Add(new Paragraph($"Total Requests: {analyticsSummary.PerformanceAnalytics.TotalRequests}"));
            }

            // Security Overview
            if (analyticsSummary.SecurityAnalytics != null)
            {
                document.Add(new Paragraph("Security Analytics").SetFontSize(18).SetBold());
                document.Add(new Paragraph($"Total Security Events: {analyticsSummary.SecurityAnalytics.TotalSecurityEvents}"));
                document.Add(new Paragraph($"Critical Events: {analyticsSummary.SecurityAnalytics.CriticalEvents}"));
                document.Add(new Paragraph($"Warning Events: {analyticsSummary.SecurityAnalytics.WarningEvents}"));
                document.Add(new Paragraph($"Info Events: {analyticsSummary.SecurityAnalytics.InfoEvents}"));
            }

            // User Analytics
            if (analyticsSummary.BusinessMetrics != null)
            {
                document.Add(new Paragraph("Business Metrics").SetFontSize(18).SetBold());
                document.Add(new Paragraph($"Active Users: {analyticsSummary.BusinessMetrics.ActiveUsers}"));
                document.Add(new Paragraph($"New Registrations: {analyticsSummary.BusinessMetrics.NewRegistrations}"));
                document.Add(new Paragraph($"Course Enrollments: {analyticsSummary.BusinessMetrics.TotalCourseEnrollments}"));
                document.Add(new Paragraph($"User Retention Rate: {analyticsSummary.BusinessMetrics.UserRetentionRate:F2}%"));
            }

            document.Close();

            return new ExportResult
            {
                Success = true,
                Data = stream.ToArray(),
                FileName = $"comprehensive_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf",
                ContentType = "application/pdf"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating comprehensive PDF");
            return new ExportResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    #endregion

    #region Excel Generation Methods

    private async Task<ExportResult> GenerateSystemHealthExcelAsync(EnterpriseHealthStatus healthStatus, object performanceHistoryObj)
    {
        try
        {
            using var workbook = new XLWorkbook();
            
            // System Health Overview
            var healthSheet = workbook.Worksheets.Add("System Health");
            healthSheet.Cell("A1").Value = "System Health Report";
            healthSheet.Cell("A1").Style.Font.Bold = true;
            healthSheet.Cell("A1").Style.Font.FontSize = 16;
            
            healthSheet.Cell("A3").Value = "Generated At:";
            healthSheet.Cell("B3").Value = healthStatus.GeneratedAt;
            
            healthSheet.Cell("A4").Value = "Overall Status:";
            healthSheet.Cell("B4").Value = healthStatus.OverallStatus.ToString();
            
            healthSheet.Cell("A5").Value = "Active Alerts:";
            healthSheet.Cell("B5").Value = healthStatus.ActiveAlertsCount;

            // Real-time metrics
            var row = 7;
            healthSheet.Cell($"A{row}").Value = "Real-time Metrics";
            healthSheet.Cell($"A{row}").Style.Font.Bold = true;
            row++;
            
            healthSheet.Cell($"A{row}").Value = "Metric";
            healthSheet.Cell($"B{row}").Value = "Value";
            healthSheet.Cell($"C{row}").Value = "Unit";
            healthSheet.Cell($"D{row}").Value = "Status";
            healthSheet.Row(row).Style.Font.Bold = true;
            row++;
            
            if (healthStatus.RealTimeMetrics != null)
            {
                foreach (var kvp in healthStatus.RealTimeMetrics)
                {
                    var metric = kvp.Value;
                    var status = GetMetricStatus(metric);
                    healthSheet.Cell($"A{row}").Value = metric.Name ?? "Unknown";
                    healthSheet.Cell($"B{row}").Value = metric.Value;
                    healthSheet.Cell($"C{row}").Value = metric.Unit ?? "";
                    healthSheet.Cell($"D{row}").Value = status.ToString();
                    row++;
                }
            }

            // Performance History
            if (performanceHistoryObj != null)
            {
                var performanceHistory = ConvertPerformanceHistory(performanceHistoryObj);
                if (performanceHistory.Any())
                {
                    var perfSheet = workbook.Worksheets.Add("Performance History");
                    row = 1;
                    perfSheet.Cell($"A{row}").Value = "Timestamp";
                    perfSheet.Cell($"B{row}").Value = "CPU Usage (%)";
                    perfSheet.Cell($"C{row}").Value = "Memory Usage (MB)";
                    perfSheet.Cell($"D{row}").Value = "Active Users";
                    perfSheet.Cell($"E{row}").Value = "Requests/Min";
                    perfSheet.Cell($"F{row}").Value = "Avg Response Time (ms)";
                    perfSheet.Row(row).Style.Font.Bold = true;
                    row++;

                    foreach (var perf in performanceHistory.OrderBy(p => p.Timestamp))
                    {
                        perfSheet.Cell($"A{row}").Value = perf.Timestamp;
                        perfSheet.Cell($"B{row}").Value = perf.CpuUsage;
                        perfSheet.Cell($"C{row}").Value = perf.MemoryUsage;
                        perfSheet.Cell($"D{row}").Value = perf.ActiveUsers;
                        perfSheet.Cell($"E{row}").Value = perf.RequestsPerMinute;
                        perfSheet.Cell($"F{row}").Value = perf.AverageResponseTime;
                        row++;
                    }
                    
                    perfSheet.Columns().AdjustToContents();
                }
            }

            // Active Alerts
            if (healthStatus.ActiveAlerts.Any())
            {
                var alertSheet = workbook.Worksheets.Add("Active Alerts");
                row = 1;
                alertSheet.Cell($"A{row}").Value = "Severity";
                alertSheet.Cell($"B{row}").Value = "Title";
                alertSheet.Cell($"C{row}").Value = "Description";
                alertSheet.Cell($"D{row}").Value = "Triggered At";
                alertSheet.Row(row).Style.Font.Bold = true;
                row++;

                foreach (var alert in healthStatus.ActiveAlerts)
                {
                    alertSheet.Cell($"A{row}").Value = alert.Severity.ToString();
                    alertSheet.Cell($"B{row}").Value = alert.Title;
                    alertSheet.Cell($"C{row}").Value = alert.Description;
                    alertSheet.Cell($"D{row}").Value = alert.TriggeredAt;
                    row++;
                }
                
                alertSheet.Columns().AdjustToContents();
            }
            
            healthSheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            
            return new ExportResult
            {
                Success = true,
                Data = stream.ToArray(),
                FileName = $"system_health_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating system health Excel");
            return new ExportResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    #endregion

    #region CSV and JSON Generation Methods

    private async Task<ExportResult> GenerateMetricsCsvAsync(Dictionary<string, RealTimeMetric> metricsDict, object performanceHistoryObj)
    {
        try
        {
            var csv = new StringBuilder();
            csv.AppendLine("Metric Name,Value,Unit,Status,Timestamp");
            
            if (metricsDict != null)
            {
                foreach (var kvp in metricsDict)
                {
                    var metric = kvp.Value;
                    var status = GetMetricStatus(metric);
                    csv.AppendLine($"{metric.Name},{metric.Value},{metric.Unit},{status},{metric.Timestamp:yyyy-MM-dd HH:mm:ss}");
                }
            }
            
            if (performanceHistoryObj != null)
            {
                var performanceHistory = ConvertPerformanceHistory(performanceHistoryObj);
                if (performanceHistory.Any())
                {
                    csv.AppendLine();
                    csv.AppendLine("Performance History");
                    csv.AppendLine("Timestamp,CPU Usage (%),Memory Usage (MB),Active Users,Requests/Min,Avg Response Time (ms)");
                    
                    foreach (var perf in performanceHistory.OrderBy(p => p.Timestamp))
                    {
                        csv.AppendLine($"{perf.Timestamp:yyyy-MM-dd HH:mm:ss},{perf.CpuUsage},{perf.MemoryUsage},{perf.ActiveUsers},{perf.RequestsPerMinute},{perf.AverageResponseTime}");
                    }
                }
            }
            
            var data = Encoding.UTF8.GetBytes(csv.ToString());
            
            return new ExportResult
            {
                Success = true,
                Data = data,
                FileName = $"metrics_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv",
                ContentType = "text/csv"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating metrics CSV");
            return new ExportResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<ExportResult> GenerateSystemHealthJsonAsync(EnterpriseHealthStatus healthStatus, object performanceHistoryObj)
    {
        try
        {
            var performanceHistory = ConvertPerformanceHistory(performanceHistoryObj);
            var exportData = new
            {
                GeneratedAt = DateTime.UtcNow,
                SystemHealth = healthStatus,
                PerformanceHistory = performanceHistory
            };
            
            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            var data = Encoding.UTF8.GetBytes(json);
            
            return new ExportResult
            {
                Success = true,
                Data = data,
                FileName = $"system_health_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json",
                ContentType = "application/json"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating system health JSON");
            return new ExportResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    #endregion

    #region Stub Methods for Other Export Types

    private async Task<ExportResult> GeneratePerformanceExcelAsync(PerformanceAnalyticsDto performanceAnalytics, List<ApiPerformanceDto> apiPerformance, DatabasePerformanceDto dbPerformance, DateTime startDate, DateTime endDate)
    {
        // Implementation similar to system health Excel
        return await Task.FromResult(new ExportResult { Success = false, ErrorMessage = "Not implemented" });
    }

    private async Task<ExportResult> GeneratePerformancePdfAsync(PerformanceAnalyticsDto performanceAnalytics, List<ApiPerformanceDto> apiPerformance, DatabasePerformanceDto dbPerformance, DateTime startDate, DateTime endDate)
    {
        return await Task.FromResult(new ExportResult { Success = false, ErrorMessage = "Not implemented" });
    }

    private async Task<ExportResult> GeneratePerformanceCsvAsync(PerformanceAnalyticsDto performanceAnalytics, List<ApiPerformanceDto> apiPerformance, DatabasePerformanceDto dbPerformance)
    {
        return await Task.FromResult(new ExportResult { Success = false, ErrorMessage = "Not implemented" });
    }

    private async Task<ExportResult> GenerateSecurityExcelAsync(SecurityAnalyticsDto securityAnalytics, List<ThreatAnalysisDto> threatAnalysis, DateTime startDate, DateTime endDate)
    {
        return await Task.FromResult(new ExportResult { Success = false, ErrorMessage = "Not implemented" });
    }

    private async Task<ExportResult> GenerateSecurityPdfAsync(SecurityAnalyticsDto securityAnalytics, List<ThreatAnalysisDto> threatAnalysis, DateTime startDate, DateTime endDate)
    {
        return await Task.FromResult(new ExportResult { Success = false, ErrorMessage = "Not implemented" });
    }

    private async Task<ExportResult> GenerateSecurityCsvAsync(SecurityAnalyticsDto securityAnalytics, List<ThreatAnalysisDto> threatAnalysis)
    {
        return await Task.FromResult(new ExportResult { Success = false, ErrorMessage = "Not implemented" });
    }

    private async Task<ExportResult> GenerateUserAnalyticsExcelAsync(UserActivityAnalyticsDto userAnalytics, List<SessionAnalyticsDto> sessionAnalytics, List<DeviceAnalyticsDto> deviceAnalytics, DateTime startDate, DateTime endDate)
    {
        return await Task.FromResult(new ExportResult { Success = false, ErrorMessage = "Not implemented" });
    }

    private async Task<ExportResult> GenerateUserAnalyticsPdfAsync(UserActivityAnalyticsDto userAnalytics, List<SessionAnalyticsDto> sessionAnalytics, List<DeviceAnalyticsDto> deviceAnalytics, DateTime startDate, DateTime endDate)
    {
        return await Task.FromResult(new ExportResult { Success = false, ErrorMessage = "Not implemented" });
    }

    private async Task<ExportResult> GenerateUserAnalyticsCsvAsync(UserActivityAnalyticsDto userAnalytics, List<SessionAnalyticsDto> sessionAnalytics, List<DeviceAnalyticsDto> deviceAnalytics)
    {
        return await Task.FromResult(new ExportResult { Success = false, ErrorMessage = "Not implemented" });
    }

    private async Task<ExportResult> GenerateMetricsExcelAsync(Dictionary<string, RealTimeMetric> metricsDict, object performanceHistoryObj)
    {
        return await Task.FromResult(new ExportResult { Success = false, ErrorMessage = "Not implemented" });
    }

    private async Task<ExportResult> GenerateMetricsJsonAsync(Dictionary<string, RealTimeMetric> metricsDict, object performanceHistoryObj)
    {
        return await Task.FromResult(new ExportResult { Success = false, ErrorMessage = "Not implemented" });
    }

    private async Task<ExportResult> GenerateAlertsPdfAsync(IEnumerable<InsightLearn.Core.Entities.SystemAlert> alerts, DateTime startDate, DateTime endDate)
    {
        return await Task.FromResult(new ExportResult { Success = false, ErrorMessage = "Not implemented" });
    }

    private async Task<ExportResult> GenerateAlertsExcelAsync(IEnumerable<InsightLearn.Core.Entities.SystemAlert> alerts, DateTime startDate, DateTime endDate)
    {
        return await Task.FromResult(new ExportResult { Success = false, ErrorMessage = "Not implemented" });
    }

    private async Task<ExportResult> GenerateAlertsCsvAsync(IEnumerable<InsightLearn.Core.Entities.SystemAlert> alerts, DateTime startDate, DateTime endDate)
    {
        return await Task.FromResult(new ExportResult { Success = false, ErrorMessage = "Not implemented" });
    }

    private async Task<ExportResult> GenerateComprehensiveExcelAsync(EnterpriseAnalyticsSummary analyticsSummary, EnterpriseHealthStatus healthStatus, object performanceHistoryObj, IEnumerable<InsightLearn.Core.Entities.SystemAlert> alerts, DateTime startDate, DateTime endDate)
    {
        return await Task.FromResult(new ExportResult { Success = false, ErrorMessage = "Not implemented" });
    }

    #endregion

    #region Helper Methods

    private static string GetMetricStatus(RealTimeMetric metric)
    {
        if (metric.IsAlert)
            return "Alert";
        
        if (metric.Threshold.HasValue)
        {
            var thresholdValue = metric.Threshold.Value;
            if (metric.Value >= thresholdValue)
                return "Warning";
        }
        
        return "Normal";
    }

    private static List<PerformanceData> ConvertPerformanceHistory(object performanceHistoryObj)
    {
        if (performanceHistoryObj == null)
            return new List<PerformanceData>();

        try
        {
            // Handle dynamic object from GetPerformanceHistoryAsync
            if (performanceHistoryObj is IEnumerable<object> dynamicList)
            {
                var result = new List<PerformanceData>();
                foreach (var item in dynamicList)
                {
                    if (item != null)
                    {
                        var properties = item.GetType().GetProperties();
                        var performanceData = new PerformanceData();
                        
                        foreach (var prop in properties)
                        {
                            var value = prop.GetValue(item);
                            switch (prop.Name.ToLower())
                            {
                                case "timestamp":
                                    if (value is DateTime dt) performanceData.Timestamp = dt;
                                    break;
                                case "cpuusage":
                                    if (value is double cpu) performanceData.CpuUsage = cpu;
                                    else if (value is decimal cpuDecimal) performanceData.CpuUsage = (double)cpuDecimal;
                                    break;
                                case "memoryusage":
                                    if (value is double mem) performanceData.MemoryUsage = mem;
                                    else if (value is decimal memDecimal) performanceData.MemoryUsage = (double)memDecimal;
                                    break;
                                case "activeusers":
                                    if (value is int users) performanceData.ActiveUsers = users;
                                    break;
                                case "requestsperminute":
                                    if (value is int requests) performanceData.RequestsPerMinute = requests;
                                    break;
                                case "averageresponsetime":
                                    if (value is double response) performanceData.AverageResponseTime = response;
                                    else if (value is decimal responseDecimal) performanceData.AverageResponseTime = (double)responseDecimal;
                                    break;
                            }
                        }
                        result.Add(performanceData);
                    }
                }
                return result;
            }
        }
        catch (Exception)
        {
            // If conversion fails, return empty list
            return new List<PerformanceData>();
        }

        return new List<PerformanceData>();
    }

    #endregion
}

public interface IExportService
{
    Task<ExportResult> ExportAnalyticsAsync(string reportType, DateTime? startDate = null, DateTime? endDate = null, string format = "Excel");
    Task<ExportResult> ExportSystemHealthAsync(string format = "PDF");
    Task<ExportResult> ExportRealTimeMetricsAsync(int hours = 1, string format = "CSV");
    Task<ExportResult> ExportAlertsReportAsync(DateTime? startDate = null, DateTime? endDate = null, string format = "PDF");
}

public class ExportResult
{
    public bool Success { get; set; }
    public byte[]? Data { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public string? ErrorMessage { get; set; }
}