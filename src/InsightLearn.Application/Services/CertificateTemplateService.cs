using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InsightLearn.Application.Services;

/// <summary>
/// Service for generating professional PDF certificates using QuestPDF
/// Phase 5.1: PDF Certificate Generation
/// </summary>
public interface ICertificateTemplateService
{
    /// <summary>
    /// Generates a PDF certificate with professional design
    /// </summary>
    /// <param name="studentName">Full name of the student</param>
    /// <param name="courseName">Name of the completed course</param>
    /// <param name="certificateNumber">Unique certificate identifier</param>
    /// <param name="issuedDate">Date when certificate was issued</param>
    /// <param name="courseHours">Total course duration in hours</param>
    /// <param name="courseRating">Average course rating (0-5)</param>
    /// <returns>PDF file as byte array</returns>
    Task<byte[]> GeneratePdfAsync(
        string studentName,
        string courseName,
        string certificateNumber,
        DateTime issuedDate,
        int courseHours,
        double courseRating);
}

public class CertificateTemplateService : ICertificateTemplateService
{
    private readonly ILogger<CertificateTemplateService> _logger;

    public CertificateTemplateService(ILogger<CertificateTemplateService> logger)
    {
        _logger = logger;

        // Set QuestPDF license (Community license - free for revenue < $1M)
        // See: https://www.questpdf.com/license/
        QuestPDF.Settings.License = LicenseType.Community;

        _logger.LogInformation("[CertificateTemplateService] Initialized with QuestPDF Community License");
    }

    public async Task<byte[]> GeneratePdfAsync(
        string studentName,
        string courseName,
        string certificateNumber,
        DateTime issuedDate,
        int courseHours,
        double courseRating)
    {
        _logger.LogInformation("[CertificateTemplateService] Generating PDF certificate for {StudentName}, Course: {CourseName}",
            studentName, courseName);

        try
        {
            // Generate PDF in background thread to avoid blocking
            return await Task.Run(() =>
            {
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        // A4 Landscape orientation for traditional certificate look
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(50);
                        page.PageColor(Colors.White);

                        // Define decorative border
                        page.Content().Border(5).BorderColor(Colors.Blue.Darken3).Padding(20).Column(column =>
                        {
                            column.Spacing(15);

                            // === HEADER SECTION ===
                            column.Item().AlignCenter().Column(headerColumn =>
                            {
                                // Main title
                                headerColumn.Item().Text("CERTIFICATE OF COMPLETION")
                                    .FontSize(36)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken3);

                                // Subtitle
                                headerColumn.Item().PaddingTop(5).Text("This is to certify that")
                                    .FontSize(14)
                                    .FontColor(Colors.Grey.Darken2);
                            });

                            // Decorative line
                            column.Item().PaddingVertical(10).LineHorizontal(3)
                                .LineColor(Colors.Blue.Darken1);

                            // === STUDENT NAME SECTION ===
                            column.Item().PaddingTop(20).AlignCenter().Text(studentName)
                                .FontSize(32)
                                .Bold()
                                .FontColor(Colors.Black);

                            // === COURSE COMPLETION TEXT ===
                            column.Item().PaddingTop(15).AlignCenter().Text("has successfully completed")
                                .FontSize(14)
                                .FontColor(Colors.Grey.Darken2);

                            // === COURSE NAME SECTION ===
                            column.Item().PaddingTop(10).AlignCenter()
                                .Border(2)
                                .BorderColor(Colors.Blue.Lighten2)
                                .Background(Colors.Blue.Lighten4)
                                .Padding(15)
                                .Text(courseName)
                                .FontSize(24)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);

                            // === COURSE DETAILS SECTION ===
                            column.Item().PaddingTop(30).Row(row =>
                            {
                                row.Spacing(50);

                                // Course Duration
                                row.RelativeItem().AlignCenter().Column(col =>
                                {
                                    col.Item().AlignCenter().Text("Duration")
                                        .FontSize(12)
                                        .FontColor(Colors.Grey.Darken1);

                                    col.Item().AlignCenter().PaddingTop(5).Text($"{courseHours} hours")
                                        .FontSize(18)
                                        .Bold()
                                        .FontColor(Colors.Blue.Darken2);
                                });

                                // Course Rating
                                row.RelativeItem().AlignCenter().Column(col =>
                                {
                                    col.Item().AlignCenter().Text("Course Rating")
                                        .FontSize(12)
                                        .FontColor(Colors.Grey.Darken1);

                                    col.Item().AlignCenter().PaddingTop(5).Text($"{courseRating:F1}/5.0 ★")
                                        .FontSize(18)
                                        .Bold()
                                        .FontColor(Colors.Orange.Darken1);
                                });

                                // Issue Date
                                row.RelativeItem().AlignCenter().Column(col =>
                                {
                                    col.Item().AlignCenter().Text("Issued On")
                                        .FontSize(12)
                                        .FontColor(Colors.Grey.Darken1);

                                    col.Item().AlignCenter().PaddingTop(5).Text(issuedDate.ToString("MMMM dd, yyyy"))
                                        .FontSize(18)
                                        .Bold()
                                        .FontColor(Colors.Blue.Darken2);
                                });
                            });

                            // === FOOTER SECTION ===
                            column.Item().PaddingTop(40).Row(row =>
                            {
                                row.Spacing(20);

                                // Signature line - Left side
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().PaddingBottom(5).LineHorizontal(2)
                                        .LineColor(Colors.Grey.Medium);

                                    col.Item().AlignCenter().Text("Platform Director")
                                        .FontSize(12)
                                        .FontColor(Colors.Grey.Darken1);

                                    col.Item().AlignCenter().Text("InsightLearn Administration")
                                        .FontSize(10)
                                        .FontColor(Colors.Grey.Medium);
                                });

                                // Spacer
                                row.ConstantItem(50);

                                // Signature line - Right side
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().PaddingBottom(5).LineHorizontal(2)
                                        .LineColor(Colors.Grey.Medium);

                                    col.Item().AlignCenter().Text("Course Instructor")
                                        .FontSize(12)
                                        .FontColor(Colors.Grey.Darken1);

                                    col.Item().AlignCenter().Text("InsightLearn Academy")
                                        .FontSize(10)
                                        .FontColor(Colors.Grey.Medium);
                                });
                            });

                            // === CERTIFICATE NUMBER (Bottom) ===
                            column.Item().PaddingTop(20).AlignCenter()
                                .Text($"Certificate Number: {certificateNumber}")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken1);

                            // === VERIFICATION URL ===
                            column.Item().PaddingTop(5).AlignCenter()
                                .Text("Verify this certificate at: https://insightlearn.cloud/verify")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Medium);
                        });
                    });
                });

                // Generate PDF byte array
                var pdfBytes = document.GeneratePdf();

                _logger.LogInformation("[CertificateTemplateService] PDF generated successfully: {Size} bytes",
                    pdfBytes.Length);

                return pdfBytes;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CertificateTemplateService] Failed to generate PDF certificate");
            throw new InvalidOperationException("Failed to generate PDF certificate", ex);
        }
    }
}
