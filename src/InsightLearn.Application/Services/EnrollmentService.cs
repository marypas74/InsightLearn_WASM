using InsightLearn.Core.DTOs.Enrollment;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Services;

/// <summary>
/// Service implementation for enrollment management and student progress tracking
/// </summary>
public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ICertificateService _certificateService;
    private readonly InsightLearnDbContext _context;
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(
        IEnrollmentRepository enrollmentRepository,
        ICourseRepository courseRepository,
        IPaymentRepository paymentRepository,
        ICertificateService certificateService,
        InsightLearnDbContext context,
        ILogger<EnrollmentService> logger)
    {
        _enrollmentRepository = enrollmentRepository;
        _courseRepository = courseRepository;
        _paymentRepository = paymentRepository;
        _certificateService = certificateService;
        _context = context;
        _logger = logger;
    }

    #region Query Methods

    public async Task<EnrollmentDto?> GetEnrollmentByIdAsync(Guid id)
    {
        _logger.LogInformation("[EnrollmentService] Getting enrollment {EnrollmentId}", id);

        var enrollment = await _enrollmentRepository.GetByIdAsync(id);
        if (enrollment == null)
        {
            _logger.LogWarning("[EnrollmentService] Enrollment {EnrollmentId} not found", id);
            return null;
        }

        return MapToDto(enrollment);
    }

    public async Task<List<EnrollmentDto>> GetUserEnrollmentsAsync(Guid userId)
    {
        _logger.LogInformation("[EnrollmentService] Getting enrollments for user {UserId}", userId);

        var enrollments = await _enrollmentRepository.GetByUserIdAsync(userId);
        return enrollments.Select(MapToDto).ToList();
    }

    public async Task<List<EnrollmentDto>> GetCourseEnrollmentsAsync(Guid courseId)
    {
        _logger.LogInformation("[EnrollmentService] Getting enrollments for course {CourseId}", courseId);

        var enrollments = await _enrollmentRepository.GetByCourseIdAsync(courseId);
        return enrollments.Select(MapToDto).ToList();
    }

    public async Task<bool> IsUserEnrolledAsync(Guid userId, Guid courseId)
    {
        _logger.LogInformation("[EnrollmentService] Checking if user {UserId} is enrolled in course {CourseId}", userId, courseId);
        return await _enrollmentRepository.IsUserEnrolledAsync(userId, courseId);
    }

    public async Task<StudentDashboardDto> GetStudentDashboardAsync(Guid userId)
    {
        _logger.LogInformation("[EnrollmentService] Building dashboard for user {UserId}", userId);

        var allEnrollments = await _enrollmentRepository.GetByUserIdAsync(userId);
        var activeEnrollments = await _enrollmentRepository.GetActiveEnrollmentsAsync(userId);
        var completedEnrollments = await _enrollmentRepository.GetCompletedEnrollmentsAsync(userId);

        var recentEnrollments = allEnrollments
            .OrderByDescending(e => e.EnrolledAt)
            .Take(5)
            .Select(MapToDto)
            .ToList();

        var continueLearning = activeEnrollments
            .OrderByDescending(e => e.LastAccessedAt)
            .Take(5)
            .Select(MapToDto)
            .ToList();

        // Calculate learning streak
        var enrollmentsList = allEnrollments.ToList();
        int daysStreak = CalculateLearningStreak(enrollmentsList);

        return new StudentDashboardDto
        {
            UserId = userId,
            UserName = enrollmentsList.FirstOrDefault()?.User?.FullName ?? string.Empty,
            TotalEnrollments = enrollmentsList.Count,
            ActiveEnrollments = activeEnrollments.Count(),
            CompletedEnrollments = completedEnrollments.Count(),
            TotalCertificates = enrollmentsList.Count(e => e.HasCertificate),
            TotalMinutesLearned = enrollmentsList.Sum(e => e.TotalWatchedMinutes),
            RecentEnrollments = recentEnrollments,
            ContinueLearning = continueLearning,
            LastActivityDate = enrollmentsList.Max(e => e.LastAccessedAt),
            DaysStreak = daysStreak
        };
    }

    #endregion

    #region Command Methods

    public async Task<EnrollmentDto> EnrollUserAsync(CreateEnrollmentDto dto)
    {
        _logger.LogInformation("[EnrollmentService] User {UserId} enrolling in course {CourseId}",
            dto.UserId, dto.CourseId);

        try
        {
            // Verify course exists and is published
            var course = await _courseRepository.GetByIdAsync(dto.CourseId);
            if (course == null)
            {
                throw new ArgumentException($"Course {dto.CourseId} not found");
            }

            if (course.Status != CourseStatus.Published)
            {
                _logger.LogWarning("[EnrollmentService] Cannot enroll in non-published course {CourseId}", dto.CourseId);
                throw new InvalidOperationException("Cannot enroll in a course that is not published");
            }

            // Check if user is already enrolled (active enrollment)
            var existingEnrollment = await _enrollmentRepository.GetActiveEnrollmentAsync(dto.UserId, dto.CourseId);
            if (existingEnrollment != null)
            {
                _logger.LogWarning("[EnrollmentService] User {UserId} already enrolled in course {CourseId}",
                    dto.UserId, dto.CourseId);
                throw new InvalidOperationException("User is already enrolled in this course");
            }

            // Verify payment if payment ID is provided
            if (dto.PaymentId.HasValue)
            {
                var payment = await _paymentRepository.GetByIdAsync(dto.PaymentId.Value);
                if (payment == null || payment.Status != PaymentStatus.Completed)
                {
                    _logger.LogError("[EnrollmentService] Invalid or incomplete payment {PaymentId} for enrollment",
                        dto.PaymentId);
                    throw new InvalidOperationException("Payment verification failed");
                }
            }

            // Create new enrollment
            var enrollment = new Enrollment
            {
                UserId = dto.UserId,
                CourseId = dto.CourseId,
                AmountPaid = dto.AmountPaid,
                Status = EnrollmentStatus.Active,
                EnrolledAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                CurrentLessonIndex = 0,
                CompletedLessons = 0,
                TotalWatchedMinutes = 0,
                HasCertificate = false
            };

            // Set initial lesson if course has lessons
            if (course.Sections?.Any() == true)
            {
                var firstLesson = course.Sections
                    .OrderBy(s => s.OrderIndex)
                    .SelectMany(s => s.Lessons.Where(l => l.IsActive))
                    .OrderBy(l => l.OrderIndex)
                    .FirstOrDefault();

                if (firstLesson != null)
                {
                    enrollment.CurrentLessonId = firstLesson.Id;
                }
            }

            // Use transaction to ensure enrollment and statistics update are atomic
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var createdEnrollment = await _enrollmentRepository.CreateAsync(enrollment);

                // Update course enrollment count
                await _courseRepository.UpdateStatisticsAsync(
                    dto.CourseId,
                    course.ViewCount,
                    course.AverageRating,
                    course.ReviewCount,
                    course.EnrollmentCount + 1);

                await transaction.CommitAsync();

                _logger.LogInformation("[EnrollmentService] User {UserId} successfully enrolled in course {CourseId}",
                    dto.UserId, dto.CourseId);

                return MapToDto(createdEnrollment);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EnrollmentService] Error enrolling user {UserId}", dto.UserId);
            throw;
        }
    }

    public async Task<bool> UpdateProgressAsync(UpdateProgressDto dto)
    {
        _logger.LogInformation("[EnrollmentService] Updating progress for enrollment {EnrollmentId}, lesson {LessonId}",
            dto.EnrollmentId, dto.LessonId);

        try
        {
            var enrollment = await _enrollmentRepository.GetByIdAsync(dto.EnrollmentId);
            if (enrollment == null)
            {
                _logger.LogWarning("[EnrollmentService] Enrollment {EnrollmentId} not found", dto.EnrollmentId);
                return false;
            }

            if (enrollment.UserId != dto.UserId)
            {
                _logger.LogWarning("[EnrollmentService] User {UserId} not authorized to update enrollment {EnrollmentId}",
                    dto.UserId, dto.EnrollmentId);
                return false;
            }

            // Get lesson details
            var course = enrollment.Course ?? await _courseRepository.GetByIdAsync(enrollment.CourseId);
            if (course == null)
            {
                _logger.LogError("[EnrollmentService] Course {CourseId} not found for enrollment {EnrollmentId}",
                    enrollment.CourseId, dto.EnrollmentId);
                return false;
            }

            var lesson = course.Sections
                .SelectMany(s => s.Lessons)
                .FirstOrDefault(l => l.Id == dto.LessonId);

            if (lesson == null)
            {
                _logger.LogWarning("[EnrollmentService] Lesson {LessonId} not found in course {CourseId}",
                    dto.LessonId, enrollment.CourseId);
                return false;
            }

            // Calculate if lesson should be marked as complete
            bool isLessonComplete = dto.IsCompleted ||
                (lesson.DurationMinutes > 0 && dto.WatchedMinutes >= lesson.DurationMinutes * 0.9);

            // Update or create lesson progress
            var lessonProgress = lesson.LessonProgress
                .FirstOrDefault(lp => lp.UserId == dto.UserId);

            if (lessonProgress == null)
            {
                lessonProgress = new LessonProgress
                {
                    LessonId = dto.LessonId,
                    UserId = dto.UserId,
                    WatchedMinutes = dto.WatchedMinutes,
                    IsCompleted = isLessonComplete,
                    CompletedAt = isLessonComplete ? DateTime.UtcNow : null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                lesson.LessonProgress.Add(lessonProgress);
            }
            else
            {
                lessonProgress.WatchedMinutes = Math.Max(lessonProgress.WatchedMinutes, dto.WatchedMinutes);
                if (isLessonComplete && !lessonProgress.IsCompleted)
                {
                    lessonProgress.IsCompleted = true;
                    lessonProgress.CompletedAt = DateTime.UtcNow;
                }
                lessonProgress.UpdatedAt = DateTime.UtcNow;
            }

            // Calculate overall progress
            var allLessons = course.Sections
                .SelectMany(s => s.Lessons.Where(l => l.IsActive))
                .ToList();

            var completedLessonsCount = allLessons
                .Count(l => l.LessonProgress.Any(lp => lp.UserId == dto.UserId && lp.IsCompleted));

            var totalWatchedMinutes = allLessons
                .SelectMany(l => l.LessonProgress.Where(lp => lp.UserId == dto.UserId))
                .Sum(lp => lp.WatchedMinutes);

            // Update enrollment progress
            enrollment.CurrentLessonId = dto.NextLessonId ?? dto.LessonId;
            enrollment.LastAccessedAt = DateTime.UtcNow;

            await _enrollmentRepository.UpdateProgressAsync(
                dto.EnrollmentId,
                completedLessonsCount,
                totalWatchedMinutes);

            // Auto-complete enrollment if all lessons are completed
            if (completedLessonsCount == allLessons.Count && allLessons.Count > 0)
            {
                _logger.LogInformation("[EnrollmentService] All lessons completed, auto-completing enrollment {EnrollmentId}",
                    dto.EnrollmentId);
                await CompleteEnrollmentAsync(dto.EnrollmentId);
            }

            _logger.LogInformation("[EnrollmentService] Progress updated successfully for enrollment {EnrollmentId}",
                dto.EnrollmentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EnrollmentService] Error updating progress for enrollment {EnrollmentId}",
                dto.EnrollmentId);
            return false;
        }
    }

    public async Task<bool> CompleteEnrollmentAsync(Guid enrollmentId)
    {
        _logger.LogInformation("[EnrollmentService] Completing enrollment {EnrollmentId}", enrollmentId);

        try
        {
            var enrollment = await _enrollmentRepository.GetByIdAsync(enrollmentId);
            if (enrollment == null)
            {
                _logger.LogWarning("[EnrollmentService] Enrollment {EnrollmentId} not found", enrollmentId);
                return false;
            }

            if (enrollment.Status == EnrollmentStatus.Completed)
            {
                _logger.LogInformation("[EnrollmentService] Enrollment {EnrollmentId} already completed", enrollmentId);
                return true;
            }

            enrollment.CompletedAt = DateTime.UtcNow;
            enrollment.Status = EnrollmentStatus.Completed;
            enrollment.HasCertificate = true;

            // Use transaction to ensure enrollment completion and certificate generation are atomic
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _enrollmentRepository.CompleteAsync(enrollmentId);

                // Generate certificate
                await _certificateService.GenerateCertificateAsync(enrollmentId);

                await transaction.CommitAsync();

                _logger.LogInformation("[EnrollmentService] Enrollment {EnrollmentId} completed and certificate generated successfully",
                    enrollmentId);
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EnrollmentService] Error completing enrollment {EnrollmentId}", enrollmentId);
            return false;
        }
    }

    public async Task<bool> CancelEnrollmentAsync(Guid enrollmentId)
    {
        _logger.LogInformation("[EnrollmentService] Cancelling enrollment {EnrollmentId}", enrollmentId);

        try
        {
            var enrollment = await _enrollmentRepository.GetByIdAsync(enrollmentId);
            if (enrollment == null)
            {
                _logger.LogWarning("[EnrollmentService] Enrollment {EnrollmentId} not found", enrollmentId);
                return false;
            }

            // Cannot cancel completed enrollment
            if (enrollment.Status == EnrollmentStatus.Completed)
            {
                _logger.LogWarning("[EnrollmentService] Cannot cancel completed enrollment {EnrollmentId}", enrollmentId);
                throw new InvalidOperationException("Cannot cancel a completed enrollment");
            }

            if (enrollment.Status == EnrollmentStatus.Cancelled)
            {
                _logger.LogInformation("[EnrollmentService] Enrollment {EnrollmentId} already cancelled", enrollmentId);
                return true;
            }

            enrollment.Status = EnrollmentStatus.Cancelled;
            await _enrollmentRepository.UpdateAsync(enrollment);

            // Update course enrollment count
            var course = await _courseRepository.GetByIdAsync(enrollment.CourseId);
            if (course != null)
            {
                await _courseRepository.UpdateStatisticsAsync(
                    enrollment.CourseId,
                    course.ViewCount,
                    course.AverageRating,
                    course.ReviewCount,
                    Math.Max(0, course.EnrollmentCount - 1));
            }

            _logger.LogInformation("[EnrollmentService] Enrollment {EnrollmentId} cancelled successfully", enrollmentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EnrollmentService] Error cancelling enrollment {EnrollmentId}", enrollmentId);
            return false;
        }
    }

    #endregion

    #region Progress Tracking

    public async Task<EnrollmentProgressDto> GetEnrollmentProgressAsync(Guid enrollmentId)
    {
        _logger.LogInformation("[EnrollmentService] Getting progress for enrollment {EnrollmentId}", enrollmentId);

        var enrollment = await _enrollmentRepository.GetByIdAsync(enrollmentId);
        if (enrollment == null)
        {
            throw new ArgumentException($"Enrollment {enrollmentId} not found");
        }

        var course = enrollment.Course ?? await _courseRepository.GetByIdAsync(enrollment.CourseId);
        if (course == null)
        {
            throw new InvalidOperationException($"Course {enrollment.CourseId} not found");
        }

        var sections = course.Sections.OrderBy(s => s.OrderIndex).ToList();
        var allLessons = sections.SelectMany(s => s.Lessons.Where(l => l.IsActive)).ToList();

        var completedLessons = allLessons
            .Where(l => l.LessonProgress.Any(lp => lp.UserId == enrollment.UserId && lp.IsCompleted))
            .ToList();

        var totalWatchedMinutes = allLessons
            .SelectMany(l => l.LessonProgress.Where(lp => lp.UserId == enrollment.UserId))
            .Sum(lp => lp.WatchedMinutes);

        var totalCourseMinutes = allLessons.Sum(l => l.DurationMinutes);

        // Estimate completion date based on current pace
        DateTime? estimatedCompletionDate = null;
        if (completedLessons.Count > 0 && completedLessons.Count < allLessons.Count)
        {
            var daysElapsed = (DateTime.UtcNow - enrollment.EnrolledAt).TotalDays;
            var averageLessonsPerDay = completedLessons.Count / Math.Max(1, daysElapsed);
            var remainingLessons = allLessons.Count - completedLessons.Count;
            var daysToComplete = remainingLessons / Math.Max(0.1, averageLessonsPerDay);
            estimatedCompletionDate = DateTime.UtcNow.AddDays(daysToComplete);
        }

        var sectionProgress = sections.Select(section =>
        {
            var sectionLessons = section.Lessons.Where(l => l.IsActive).ToList();
            var completedSectionLessons = sectionLessons
                .Count(l => l.LessonProgress.Any(lp => lp.UserId == enrollment.UserId && lp.IsCompleted));

            return new SectionProgressDto
            {
                SectionId = section.Id,
                SectionTitle = section.Title,
                TotalLessons = sectionLessons.Count,
                CompletedLessons = completedSectionLessons,
                ProgressPercentage = sectionLessons.Count > 0
                    ? (double)completedSectionLessons / sectionLessons.Count * 100
                    : 0
            };
        }).ToList();

        return new EnrollmentProgressDto
        {
            EnrollmentId = enrollmentId,
            UserId = enrollment.UserId,
            CourseId = enrollment.CourseId,
            CourseTitle = course.Title,
            TotalSections = sections.Count,
            TotalLessons = allLessons.Count,
            CompletedLessons = completedLessons.Count,
            ProgressPercentage = allLessons.Count > 0
                ? (double)completedLessons.Count / allLessons.Count * 100
                : 0,
            TotalCourseMinutes = totalCourseMinutes,
            TotalWatchedMinutes = totalWatchedMinutes,
            LastAccessedAt = enrollment.LastAccessedAt,
            EstimatedCompletionDate = estimatedCompletionDate,
            Sections = sectionProgress
        };
    }

    public async Task<LessonProgressDto?> GetLessonProgressAsync(Guid enrollmentId, Guid lessonId)
    {
        _logger.LogInformation("[EnrollmentService] Getting lesson progress for enrollment {EnrollmentId}, lesson {LessonId}",
            enrollmentId, lessonId);

        var enrollment = await _enrollmentRepository.GetByIdAsync(enrollmentId);
        if (enrollment == null)
        {
            return null;
        }

        var course = enrollment.Course ?? await _courseRepository.GetByIdAsync(enrollment.CourseId);
        if (course == null)
        {
            return null;
        }

        var lesson = course.Sections
            .SelectMany(s => s.Lessons)
            .FirstOrDefault(l => l.Id == lessonId);

        if (lesson == null)
        {
            return null;
        }

        var progress = lesson.LessonProgress
            .FirstOrDefault(lp => lp.UserId == enrollment.UserId);

        if (progress == null)
        {
            return new LessonProgressDto
            {
                Id = Guid.Empty,
                LessonId = lessonId,
                LessonTitle = lesson.Title,
                UserId = enrollment.UserId,
                IsCompleted = false,
                WatchedMinutes = 0,
                LessonDurationMinutes = lesson.DurationMinutes,
                ProgressPercentage = 0,
                CompletedAt = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        return new LessonProgressDto
        {
            Id = progress.Id,
            LessonId = progress.LessonId,
            LessonTitle = lesson.Title,
            UserId = progress.UserId,
            IsCompleted = progress.IsCompleted,
            WatchedMinutes = progress.WatchedMinutes,
            LessonDurationMinutes = lesson.DurationMinutes,
            ProgressPercentage = progress.ProgressPercentage,
            CompletedAt = progress.CompletedAt,
            CreatedAt = progress.CreatedAt,
            UpdatedAt = progress.UpdatedAt
        };
    }

    #endregion

    #region Private Helper Methods

    private EnrollmentDto MapToDto(Enrollment enrollment)
    {
        var course = enrollment.Course;
        var totalLessons = 0;

        if (course != null)
        {
            totalLessons = course.Sections
                .SelectMany(s => s.Lessons.Where(l => l.IsActive))
                .Count();
        }

        return new EnrollmentDto
        {
            Id = enrollment.Id,
            UserId = enrollment.UserId,
            UserName = enrollment.User?.FullName ?? string.Empty,
            UserEmail = enrollment.User?.Email,
            CourseId = enrollment.CourseId,
            CourseTitle = course?.Title ?? string.Empty,
            CourseThumbnailUrl = course?.ThumbnailUrl,
            EnrolledAt = enrollment.EnrolledAt,
            CompletedAt = enrollment.CompletedAt,
            LastAccessedAt = enrollment.LastAccessedAt,
            AmountPaid = enrollment.AmountPaid,
            Status = enrollment.Status.ToString(),
            CurrentLessonIndex = enrollment.CurrentLessonIndex,
            CurrentLessonId = enrollment.CurrentLessonId,
            CurrentLessonTitle = enrollment.CurrentLesson?.Title,
            HasCertificate = enrollment.HasCertificate,
            CompletedLessons = enrollment.CompletedLessons,
            TotalLessons = totalLessons,
            ProgressPercentage = totalLessons > 0
                ? (double)enrollment.CompletedLessons / totalLessons * 100
                : 0,
            TotalWatchedMinutes = enrollment.TotalWatchedMinutes
        };
    }

    private int CalculateLearningStreak(List<Enrollment> enrollments)
    {
        if (!enrollments.Any())
            return 0;

        var activityDates = enrollments
            .Select(e => e.LastAccessedAt.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        if (!activityDates.Any() || activityDates.First() < DateTime.UtcNow.Date.AddDays(-1))
            return 0;

        int streak = 1;
        var currentDate = activityDates.First();

        for (int i = 1; i < activityDates.Count; i++)
        {
            if (activityDates[i] == currentDate.AddDays(-1))
            {
                streak++;
                currentDate = activityDates[i];
            }
            else
            {
                break;
            }
        }

        return streak;
    }

    #endregion
}