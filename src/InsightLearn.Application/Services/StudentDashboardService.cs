using InsightLearn.Core.Entities;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InsightLearn.Application.Services
{
    public interface IStudentDashboardService
    {
        Task<StudentDashboardViewModel> GetDashboardDataAsync(Guid studentId);
    }

    public class StudentDashboardService : IStudentDashboardService
    {
        private readonly InsightLearnDbContext _context;

        public StudentDashboardService(InsightLearnDbContext context)
        {
            _context = context;
        }

        public async Task<StudentDashboardViewModel> GetDashboardDataAsync(Guid studentId)
        {
            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Category)
                .Where(e => e.UserId == studentId)
                .ToListAsync();

            var totalCompletedLessons = await _context.LessonProgress
                .CountAsync(lp => lp.UserId == studentId && lp.IsCompleted);

            var certificates = await _context.Certificates
                .Include(c => c.Course)
                .Where(c => c.UserId == studentId)
                .ToListAsync();

            // Calculate progress for each course asynchronously
            var courseEnrollments = new List<CourseEnrollmentDto>();
            foreach (var enrollment in enrollments)
            {
                var progress = await CalculateCourseProgressAsync(enrollment.CourseId, studentId);
                courseEnrollments.Add(new CourseEnrollmentDto
                {
                    CourseId = enrollment.CourseId,
                    CourseName = enrollment.Course.Title,
                    CategoryName = enrollment.Course.Category.Name,
                    EnrollmentDate = enrollment.EnrolledAt,
                    Progress = progress
                });
            }

            return new StudentDashboardViewModel
            {
                EnrolledCourses = courseEnrollments,
                TotalCompletedLessons = totalCompletedLessons,
                Certificates = certificates.Select(c => new CertificateDto
                {
                    CourseTitle = c.Course.Title,
                    AwardedDate = c.IssuedAt
                }).ToList()
            };
        }

        private async Task<decimal> CalculateCourseProgressAsync(Guid courseId, Guid studentId)
        {
            var totalLessons = await _context.Lessons
                .Where(l => l.Section.CourseId == courseId && l.IsActive)
                .CountAsync();
                
            var completedLessons = await _context.LessonProgress
                .Where(lp => lp.UserId == studentId && 
                             lp.Lesson.Section.CourseId == courseId && 
                             lp.IsCompleted)
                .CountAsync();

            return totalLessons > 0 
                ? (decimal)completedLessons / totalLessons * 100 
                : 0;
        }
    }

    public class StudentDashboardViewModel
    {
        public List<CourseEnrollmentDto> EnrolledCourses { get; set; } = new();
        public int TotalCompletedLessons { get; set; }
        public List<CertificateDto> Certificates { get; set; } = new();
    }

    public class CourseEnrollmentDto
    {
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public DateTime EnrollmentDate { get; set; }
        public decimal Progress { get; set; }
    }

    public class CertificateDto
    {
        public string CourseTitle { get; set; } = string.Empty;
        public DateTime AwardedDate { get; set; }
    }
}