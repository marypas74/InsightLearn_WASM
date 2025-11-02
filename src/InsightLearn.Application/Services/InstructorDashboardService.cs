using InsightLearn.Core.Entities;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InsightLearn.Application.Services
{
    public interface IInstructorDashboardService
    {
        Task<InstructorDashboardViewModel> GetDashboardDataAsync(Guid instructorId);
    }

    public class InstructorDashboardService : IInstructorDashboardService
    {
        private readonly InsightLearnDbContext _context;

        public InstructorDashboardService(InsightLearnDbContext context)
        {
            _context = context;
        }

        public async Task<InstructorDashboardViewModel> GetDashboardDataAsync(Guid instructorId)
        {
            var courses = await _context.Courses
                .Include(c => c.Enrollments)
                .Include(c => c.Reviews)
                .Where(c => c.InstructorId == instructorId)
                .ToListAsync();

            var totalEnrollments = courses.Sum(c => c.Enrollments.Count);
            var totalReviews = courses.Sum(c => c.Reviews.Count);
            var averageRating = courses.Any() 
                ? (decimal)courses.Average(c => c.Reviews.Any() ? c.Reviews.Average(r => r.Rating) : 0) 
                : 0;

            // Calculate revenue for each course asynchronously
            var instructorCourses = new List<InstructorCourseDto>();
            foreach (var course in courses)
            {
                var revenue = await CalculateCourseRevenueAsync(course.Id);
                instructorCourses.Add(new InstructorCourseDto
                {
                    CourseId = course.Id,
                    CourseTitle = course.Title,
                    Enrollments = course.Enrollments.Count,
                    AverageRating = course.Reviews.Any() 
                        ? Math.Round((decimal)course.Reviews.Average(r => r.Rating), 2)
                        : 0,
                    Revenue = revenue
                });
            }

            return new InstructorDashboardViewModel
            {
                TotalCourses = courses.Count,
                TotalEnrollments = totalEnrollments,
                TotalReviews = totalReviews,
                AverageRating = Math.Round(averageRating, 2),
                Courses = instructorCourses
            };
        }

        private async Task<decimal> CalculateCourseRevenueAsync(Guid courseId)
        {
            return await _context.Payments
                .Where(p => p.CourseId == courseId)
                .SumAsync(p => p.Amount);
        }
    }

    public class InstructorDashboardViewModel
    {
        public int TotalCourses { get; set; }
        public int TotalEnrollments { get; set; }
        public int TotalReviews { get; set; }
        public decimal AverageRating { get; set; }
        public List<InstructorCourseDto> Courses { get; set; } = new();
    }

    public class InstructorCourseDto
    {
        public Guid CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public int Enrollments { get; set; }
        public decimal AverageRating { get; set; }
        public decimal Revenue { get; set; }
    }
}