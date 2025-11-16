# Certificate Generation Test Guide

## Prerequisites
1. API must be running (or use test harness)
2. Database must have:
   - A completed enrollment (IsCompleted = true)
   - Associated course
   - Associated user

## Test Method 1: API Endpoint Test

### Step 1: Create Test Data
```sql
-- Verify completed enrollment exists
SELECT TOP 1
    e.Id AS EnrollmentId,
    e.UserId,
    e.CourseId,
    e.IsCompleted,
    u.FirstName,
    u.LastName,
    u.Email,
    c.Title,
    c.EstimatedDurationMinutes,
    c.AverageRating
FROM Enrollments e
INNER JOIN Users u ON e.UserId = u.Id
INNER JOIN Courses c ON e.CourseId = c.Id
WHERE e.IsCompleted = 1
ORDER BY e.CompletedAt DESC;
```

### Step 2: Call Certificate Generation Endpoint
```bash
# Replace ENROLLMENT_ID with actual enrollment ID from SQL query
# Replace JWT_TOKEN with valid admin/instructor token

curl -X POST "http://localhost:31081/api/certificates/generate" \
  -H "Authorization: Bearer JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "enrollmentId": "ENROLLMENT_ID"
  }'
```

### Step 3: Verify PDF Generated
```bash
# Check if PDF exists in wwwroot/certificates/
ls -lh wwwroot/certificates/*.pdf

# Example output:
# -rw-r--r-- 1 user user 42K Nov 16 14:30 12345678-1234-1234-1234-123456789abc.pdf
```

### Step 4: Download and Verify PDF
```bash
# Get certificate details from API response
# Example response:
# {
#   "id": "12345678-1234-1234-1234-123456789abc",
#   "certificateNumber": "CERT-2025-A1B2C3-D4E5F6-9K7M",
#   "pdfUrl": "/certificates/12345678-1234-1234-1234-123456789abc.pdf"
# }

# Download PDF (if API is public)
curl -O "http://localhost:31081/certificates/12345678-1234-1234-1234-123456789abc.pdf"

# Or access via browser
# http://localhost:31081/certificates/12345678-1234-1234-1234-123456789abc.pdf
```

## Test Method 2: Direct Service Test (Unit Test)

Create test file: `src/InsightLearn.Application.Tests/Services/CertificateServiceTests.cs`

```csharp
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using InsightLearn.Application.Services;
using InsightLearn.Core.Interfaces;
using InsightLearn.Core.Entities;

public class CertificateServiceTests
{
    [Fact]
    public async Task GenerateCertificateAsync_ShouldCreatePDF()
    {
        // Arrange
        var enrollmentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var enrollment = new Enrollment
        {
            Id = enrollmentId,
            UserId = userId,
            CourseId = courseId,
            IsCompleted = true,
            User = new User
            {
                Id = userId,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com"
            }
        };

        var course = new Course
        {
            Id = courseId,
            Title = "Advanced C# Programming",
            EstimatedDurationMinutes = 600, // 10 hours
            AverageRating = 4.8
        };

        var mockEnrollmentRepo = new Mock<IEnrollmentRepository>();
        mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId))
            .ReturnsAsync(enrollment);

        var mockCourseRepo = new Mock<ICourseRepository>();
        mockCourseRepo.Setup(r => r.GetByIdAsync(courseId))
            .ReturnsAsync(course);

        var mockCertificateRepo = new Mock<ICertificateRepository>();
        mockCertificateRepo.Setup(r => r.GetByEnrollmentIdAsync(enrollmentId))
            .ReturnsAsync((Certificate)null);
        mockCertificateRepo.Setup(r => r.CreateAsync(It.IsAny<Certificate>()))
            .ReturnsAsync((Certificate c) => c);

        var templateService = new CertificateTemplateService(
            new Mock<ILogger<CertificateTemplateService>>().Object);

        var certificateService = new CertificateService(
            mockCertificateRepo.Object,
            mockEnrollmentRepo.Object,
            mockCourseRepo.Object,
            templateService,
            new Mock<ILogger<CertificateService>>().Object);

        // Act
        var certificate = await certificateService.GenerateCertificateAsync(enrollmentId);

        // Assert
        Assert.NotNull(certificate);
        Assert.NotNull(certificate.PdfUrl);
        Assert.Contains("/certificates/", certificate.PdfUrl);
        Assert.Contains(certificate.Id.ToString(), certificate.PdfUrl);

        // Verify PDF exists
        var pdfPath = Path.Combine("wwwroot", "certificates", $"{certificate.Id}.pdf");
        Assert.True(File.Exists(pdfPath));

        // Verify PDF size (should be reasonable, ~20-100KB)
        var fileInfo = new FileInfo(pdfPath);
        Assert.True(fileInfo.Length > 10000); // > 10KB
        Assert.True(fileInfo.Length < 500000); // < 500KB

        Console.WriteLine($"PDF generated successfully: {fileInfo.Length} bytes");
    }
}
```

## Expected PDF Output

The generated PDF should contain:
1. **Header**: "CERTIFICATE OF COMPLETION" (large, bold, blue)
2. **Student Name**: Full name centered (e.g., "John Doe")
3. **Course Name**: Course title in colored box (e.g., "Advanced C# Programming")
4. **Course Details**:
   - Duration: "10 hours"
   - Rating: "4.8/5.0 ★"
   - Issue Date: "November 16, 2025"
5. **Signatures**: Platform Director and Course Instructor signature lines
6. **Certificate Number**: Unique ID (e.g., "CERT-2025-A1B2C3-D4E5F6-9K7M")
7. **Verification URL**: "Verify at: https://insightlearn.cloud/verify"
8. **Design**: A4 Landscape, blue border, professional typography

## PDF Verification Checklist

- [ ] PDF opens in Adobe Reader / browser
- [ ] All text is readable and properly formatted
- [ ] Student name matches enrollment user
- [ ] Course name matches course title
- [ ] Course hours calculated correctly (EstimatedDurationMinutes / 60)
- [ ] Rating displays correctly with star symbol
- [ ] Certificate number is unique and follows format CERT-YYYY-XXXXXX-XXXXXX-XXXX
- [ ] File size is reasonable (~20-50KB expected)
- [ ] Border and decorative elements render correctly
- [ ] Professional appearance suitable for LinkedIn/portfolio

## Troubleshooting

### Error: "Failed to generate PDF certificate"
- Check QuestPDF license is set (Community license)
- Verify QuestPDF NuGet package installed (v2024.10.3)
- Check logs for detailed error message

### Error: "Enrollment not found"
- Verify enrollment exists in database
- Check enrollment ID is correct GUID

### Error: "Cannot generate certificate for incomplete enrollment"
- Ensure enrollment.IsCompleted = true
- Update enrollment: `UPDATE Enrollments SET IsCompleted = 1 WHERE Id = 'ENROLLMENT_ID'`

### PDF Not Found in wwwroot/certificates/
- Check directory permissions
- Verify wwwroot directory exists
- Check application has write permissions

### PDF File Size Issues
- **Too small (<5KB)**: PDF generation failed, check for exceptions
- **Too large (>200KB)**: May indicate rendering issue, but not necessarily wrong
- **Expected size**: 20-100KB for typical certificate

## Performance Benchmarks

Expected timings:
- PDF Generation: 50-200ms
- File Write: 5-20ms
- Database Save: 10-50ms
- **Total**: 100-300ms per certificate

For 1000 certificates:
- Sequential: ~2-5 minutes
- Parallel (10 threads): ~30-60 seconds
