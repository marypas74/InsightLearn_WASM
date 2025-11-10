using InsightLearn.Core.DTOs.Course;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace InsightLearn.Application.Services;

/// <summary>
/// Service for course business logic operations
/// </summary>
public class CourseService : ICourseService
{
    private readonly ICourseRepository _courseRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ILogger<CourseService> _logger;

    public CourseService(
        ICourseRepository courseRepository,
        ICategoryRepository categoryRepository,
        IEnrollmentRepository enrollmentRepository,
        ILogger<CourseService> logger)
    {
        _courseRepository = courseRepository;
        _categoryRepository = categoryRepository;
        _enrollmentRepository = enrollmentRepository;
        _logger = logger;
    }

    public async Task<CourseListDto> GetCoursesAsync(int page = 1, int pageSize = 10, Guid? categoryId = null)
    {
        try
        {
            _logger.LogInformation("Fetching courses - Page: {Page}, PageSize: {PageSize}, CategoryId: {CategoryId}",
                page, pageSize, categoryId);

            IEnumerable<Course> courses;
            int totalCount;

            if (categoryId.HasValue)
            {
                courses = await _courseRepository.GetByCategoryIdAsync(categoryId.Value, page, pageSize);
                // Note: In real implementation, you'd need a GetByCategoryCountAsync method
                var allCategoryCourses = await _courseRepository.GetByCategoryIdAsync(categoryId.Value, 1, int.MaxValue);
                totalCount = allCategoryCourses.Count();
            }
            else
            {
                courses = await _courseRepository.GetPublishedCoursesAsync(page, pageSize);
                totalCount = await _courseRepository.GetPublishedCountAsync();
            }

            var courseCards = courses.Select(MapToCourseCardDto).ToList();

            return new CourseListDto
            {
                Courses = courseCards,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching courses");
            throw;
        }
    }

    public async Task<CourseDto?> GetCourseByIdAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Fetching course by ID: {CourseId}", id);

            var course = await _courseRepository.GetByIdAsync(id);
            if (course == null)
            {
                _logger.LogWarning("Course not found: {CourseId}", id);
                return null;
            }

            return MapToDto(course);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching course by ID: {CourseId}", id);
            throw;
        }
    }

    public async Task<CourseDto?> GetCourseBySlugAsync(string slug)
    {
        try
        {
            _logger.LogInformation("Fetching course by slug: {Slug}", slug);

            var course = await _courseRepository.GetBySlugAsync(slug);
            if (course == null)
            {
                _logger.LogWarning("Course not found with slug: {Slug}", slug);
                return null;
            }

            return MapToDto(course);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching course by slug: {Slug}", slug);
            throw;
        }
    }

    public async Task<CourseListDto> GetCoursesByCategoryAsync(Guid categoryId, int page = 1, int pageSize = 10)
    {
        try
        {
            _logger.LogInformation("Fetching courses by category: {CategoryId}", categoryId);

            var courses = await _courseRepository.GetByCategoryIdAsync(categoryId, page, pageSize);
            var allCategoryCourses = await _courseRepository.GetByCategoryIdAsync(categoryId, 1, int.MaxValue);
            var totalCount = allCategoryCourses.Count();

            var courseCards = courses
                .Where(c => c.Status == CourseStatus.Published && c.IsActive)
                .Select(MapToCourseCardDto)
                .ToList();

            return new CourseListDto
            {
                Courses = courseCards,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching courses by category: {CategoryId}", categoryId);
            throw;
        }
    }

    public async Task<List<CourseCardDto>> GetCoursesByInstructorAsync(Guid instructorId)
    {
        try
        {
            _logger.LogInformation("Fetching courses by instructor: {InstructorId}", instructorId);

            var courses = await _courseRepository.GetByInstructorIdAsync(instructorId);
            return courses.Select(MapToCourseCardDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching courses by instructor: {InstructorId}", instructorId);
            throw;
        }
    }

    public async Task<CourseListDto> SearchCoursesAsync(CourseSearchDto searchDto)
    {
        try
        {
            _logger.LogInformation("Searching courses with query: {Query}", searchDto.Query);

            // Get courses for filtering (limited to 1000 to prevent memory issues)
            // TODO Phase 4: Implement database-level filtering with IQueryable repository method
            var allCourses = await _courseRepository.GetPublishedCoursesAsync(1, 1000);
            var query = allCourses.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchDto.Query))
            {
                var searchTerm = searchDto.Query.ToLower();
                query = query.Where(c =>
                    c.Title.ToLower().Contains(searchTerm) ||
                    c.Description.ToLower().Contains(searchTerm));
            }

            if (searchDto.CategoryId.HasValue)
            {
                query = query.Where(c => c.CategoryId == searchDto.CategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchDto.Level))
            {
                if (Enum.TryParse<CourseLevel>(searchDto.Level, true, out var level))
                {
                    query = query.Where(c => c.Level == level);
                }
            }

            if (searchDto.MinPrice.HasValue)
            {
                query = query.Where(c => c.CurrentPrice >= searchDto.MinPrice.Value);
            }

            if (searchDto.MaxPrice.HasValue)
            {
                query = query.Where(c => c.CurrentPrice <= searchDto.MaxPrice.Value);
            }

            if (searchDto.MinRating.HasValue)
            {
                query = query.Where(c => c.AverageRating >= searchDto.MinRating.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchDto.Language))
            {
                query = query.Where(c => c.Language == searchDto.Language);
            }

            if (searchDto.IsFree.HasValue)
            {
                query = searchDto.IsFree.Value
                    ? query.Where(c => c.Price == 0)
                    : query.Where(c => c.Price > 0);
            }

            if (searchDto.HasCertificate.HasValue)
            {
                query = query.Where(c => c.HasCertificate == searchDto.HasCertificate.Value);
            }

            // Apply sorting
            query = searchDto.SortBy?.ToLower() switch
            {
                "newest" => query.OrderByDescending(c => c.PublishedAt),
                "rating" => query.OrderByDescending(c => c.AverageRating),
                "price" => query.OrderBy(c => c.CurrentPrice),
                "popular" => query.OrderByDescending(c => c.EnrollmentCount),
                _ => query.OrderByDescending(c => c.AverageRating) // Default: Relevance (by rating)
            };

            var totalCount = query.Count();

            // Apply pagination
            var courses = query
                .Skip((searchDto.Page - 1) * searchDto.PageSize)
                .Take(searchDto.PageSize)
                .ToList();

            var courseCards = courses.Select(MapToCourseCardDto).ToList();

            return new CourseListDto
            {
                Courses = courseCards,
                TotalCount = totalCount,
                Page = searchDto.Page,
                PageSize = searchDto.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching courses");
            throw;
        }
    }

    public async Task<CourseDto> CreateCourseAsync(CreateCourseDto dto)
    {
        try
        {
            _logger.LogInformation("Creating new course: {Title}", dto.Title);

            // Validate category exists
            var category = await _categoryRepository.GetByIdAsync(dto.CategoryId);
            if (category == null)
            {
                throw new InvalidOperationException($"Category not found: {dto.CategoryId}");
            }

            // Note: In production, you'd validate instructor exists using UserManager or IUserRepository
            // For now, we assume the instructor ID is valid

            // Generate slug from title if not provided
            var slug = GenerateSlug(dto.Title);

            // Check if slug already exists
            var existingCourse = await _courseRepository.GetBySlugAsync(slug);
            if (existingCourse != null)
            {
                slug = $"{slug}-{Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            // Parse level
            if (!Enum.TryParse<CourseLevel>(dto.Level, true, out var level))
            {
                level = CourseLevel.Beginner;
            }

            var course = new Course
            {
                Title = dto.Title,
                Description = dto.Description,
                ShortDescription = dto.ShortDescription,
                InstructorId = dto.InstructorId,
                CategoryId = dto.CategoryId,
                Price = dto.Price,
                DiscountPercentage = dto.DiscountPercentage,
                ThumbnailUrl = dto.ThumbnailUrl,
                PreviewVideoUrl = dto.PreviewVideoUrl,
                Level = level,
                Status = CourseStatus.Draft,
                EstimatedDurationMinutes = dto.EstimatedDurationMinutes,
                Requirements = dto.Requirements,
                WhatYouWillLearn = dto.WhatYouWillLearn,
                Language = dto.Language,
                HasCertificate = dto.HasCertificate,
                MetaDescription = dto.MetaDescription,
                Slug = slug,
                CreatedAt = DateTime.UtcNow
            };

            var createdCourse = await _courseRepository.CreateAsync(course);
            _logger.LogInformation("Course created successfully: {CourseId}", createdCourse.Id);

            // Load navigation properties for DTO mapping
            var fullCourse = await _courseRepository.GetByIdAsync(createdCourse.Id);
            return MapToDto(fullCourse!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating course");
            throw;
        }
    }

    public async Task<CourseDto?> UpdateCourseAsync(Guid id, UpdateCourseDto dto)
    {
        try
        {
            _logger.LogInformation("Updating course: {CourseId}", id);

            var course = await _courseRepository.GetByIdAsync(id);
            if (course == null)
            {
                _logger.LogWarning("Course not found: {CourseId}", id);
                return null;
            }

            // Update only provided fields
            if (!string.IsNullOrWhiteSpace(dto.Title))
            {
                course.Title = dto.Title;
                course.Slug = GenerateSlug(dto.Title);
            }

            if (!string.IsNullOrWhiteSpace(dto.Description))
            {
                course.Description = dto.Description;
            }

            if (dto.ShortDescription != null)
            {
                course.ShortDescription = dto.ShortDescription;
            }

            if (dto.CategoryId.HasValue)
            {
                var category = await _categoryRepository.GetByIdAsync(dto.CategoryId.Value);
                if (category == null)
                {
                    throw new InvalidOperationException($"Category not found: {dto.CategoryId.Value}");
                }
                course.CategoryId = dto.CategoryId.Value;
            }

            if (dto.Price.HasValue)
            {
                course.Price = dto.Price.Value;
            }

            if (dto.DiscountPercentage.HasValue)
            {
                course.DiscountPercentage = dto.DiscountPercentage.Value;
            }

            if (dto.ThumbnailUrl != null)
            {
                course.ThumbnailUrl = dto.ThumbnailUrl;
            }

            if (dto.PreviewVideoUrl != null)
            {
                course.PreviewVideoUrl = dto.PreviewVideoUrl;
            }

            if (!string.IsNullOrWhiteSpace(dto.Level))
            {
                if (Enum.TryParse<CourseLevel>(dto.Level, true, out var level))
                {
                    course.Level = level;
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                if (Enum.TryParse<CourseStatus>(dto.Status, true, out var status))
                {
                    course.Status = status;
                }
            }

            if (dto.EstimatedDurationMinutes.HasValue)
            {
                course.EstimatedDurationMinutes = dto.EstimatedDurationMinutes.Value;
            }

            if (dto.Requirements != null)
            {
                course.Requirements = dto.Requirements;
            }

            if (dto.WhatYouWillLearn != null)
            {
                course.WhatYouWillLearn = dto.WhatYouWillLearn;
            }

            if (dto.Language != null)
            {
                course.Language = dto.Language;
            }

            if (dto.HasCertificate.HasValue)
            {
                course.HasCertificate = dto.HasCertificate.Value;
            }

            if (dto.IsActive.HasValue)
            {
                course.IsActive = dto.IsActive.Value;
            }

            if (dto.MetaDescription != null)
            {
                course.MetaDescription = dto.MetaDescription;
            }

            course.UpdatedAt = DateTime.UtcNow;

            var updatedCourse = await _courseRepository.UpdateAsync(course);
            _logger.LogInformation("Course updated successfully: {CourseId}", id);

            // Reload with navigation properties
            var fullCourse = await _courseRepository.GetByIdAsync(updatedCourse.Id);
            return MapToDto(fullCourse!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating course: {CourseId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteCourseAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting course: {CourseId}", id);

            var course = await _courseRepository.GetByIdAsync(id);
            if (course == null)
            {
                _logger.LogWarning("Course not found: {CourseId}", id);
                return false;
            }

            // Check for active enrollments
            var enrollments = await _enrollmentRepository.GetByCourseIdAsync(id);
            if (enrollments.Any())
            {
                _logger.LogWarning("Cannot delete course {CourseId} - has active enrollments", id);
                throw new InvalidOperationException("Cannot delete course with active enrollments");
            }

            await _courseRepository.DeleteAsync(id);
            _logger.LogInformation("Course deleted successfully: {CourseId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting course: {CourseId}", id);
            throw;
        }
    }

    public async Task<bool> PublishCourseAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Publishing course: {CourseId}", id);

            var course = await _courseRepository.GetByIdAsync(id);
            if (course == null)
            {
                _logger.LogWarning("Course not found: {CourseId}", id);
                return false;
            }

            course.Status = CourseStatus.Published;
            course.PublishedAt = DateTime.UtcNow;
            course.UpdatedAt = DateTime.UtcNow;

            await _courseRepository.UpdateAsync(course);
            await UpdateCourseStatisticsAsync(id);

            _logger.LogInformation("Course published successfully: {CourseId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing course: {CourseId}", id);
            throw;
        }
    }

    public async Task<bool> UnpublishCourseAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Unpublishing course: {CourseId}", id);

            var course = await _courseRepository.GetByIdAsync(id);
            if (course == null)
            {
                _logger.LogWarning("Course not found: {CourseId}", id);
                return false;
            }

            course.Status = CourseStatus.Draft;
            course.UpdatedAt = DateTime.UtcNow;

            await _courseRepository.UpdateAsync(course);
            _logger.LogInformation("Course unpublished successfully: {CourseId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unpublishing course: {CourseId}", id);
            throw;
        }
    }

    public async Task<CourseSummaryDto> GetCourseSummaryAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Fetching course summary: {CourseId}", id);

            var course = await _courseRepository.GetByIdAsync(id);
            if (course == null)
            {
                throw new InvalidOperationException($"Course not found: {id}");
            }

            var enrollments = await _enrollmentRepository.GetByCourseIdAsync(id);
            var enrollmentsList = enrollments.ToList();

            var summary = new CourseSummaryDto
            {
                Id = course.Id,
                Title = course.Title,
                Slug = course.Slug,
                TotalSections = course.Sections.Count,
                TotalLessons = course.Sections.Sum(s => s.Lessons.Count),
                TotalDurationMinutes = course.EstimatedDurationMinutes,
                EnrollmentCount = enrollmentsList.Count,
                ActiveEnrollments = enrollmentsList.Count(e => e.Status == EnrollmentStatus.Active),
                CompletedEnrollments = enrollmentsList.Count(e => e.Status == EnrollmentStatus.Completed),
                AverageRating = course.AverageRating,
                ReviewCount = course.ReviewCount,
                ViewCount = course.ViewCount,
                TotalRevenue = enrollmentsList.Sum(e => e.AmountPaid),
                CreatedAt = course.CreatedAt,
                PublishedAt = course.PublishedAt
            };

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching course summary: {CourseId}", id);
            throw;
        }
    }

    public async Task IncrementViewCountAsync(Guid id)
    {
        try
        {
            var course = await _courseRepository.GetByIdAsync(id);
            if (course != null)
            {
                course.ViewCount++;
                await _courseRepository.UpdateAsync(course);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing view count for course: {CourseId}", id);
            // Don't throw - view counting is non-critical
        }
    }

    public async Task UpdateCourseStatisticsAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Updating course statistics: {CourseId}", id);

            var course = await _courseRepository.GetByIdAsync(id);
            if (course == null)
            {
                return;
            }

            var enrollments = await _enrollmentRepository.GetByCourseIdAsync(id);
            var enrollmentCount = enrollments.Count();

            var averageRating = course.Reviews.Any()
                ? course.Reviews.Average(r => r.Rating)
                : 0;
            var reviewCount = course.Reviews.Count;

            await _courseRepository.UpdateStatisticsAsync(
                id,
                course.ViewCount,
                averageRating,
                reviewCount,
                enrollmentCount);

            _logger.LogInformation("Course statistics updated: {CourseId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating course statistics: {CourseId}", id);
            throw;
        }
    }

    // Private helper methods

    private CourseDto MapToDto(Course course)
    {
        return new CourseDto
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            ShortDescription = course.ShortDescription,
            InstructorId = course.InstructorId,
            InstructorName = $"{course.Instructor?.FirstName ?? ""} {course.Instructor?.LastName ?? ""}".Trim(),
            InstructorProfilePictureUrl = course.Instructor?.ProfilePictureUrl,
            CategoryId = course.CategoryId,
            CategoryName = course.Category?.Name ?? "",
            CategorySlug = course.Category?.Slug,
            Price = course.Price,
            DiscountPercentage = course.DiscountPercentage,
            CurrentPrice = course.CurrentPrice,
            ThumbnailUrl = course.ThumbnailUrl,
            PreviewVideoUrl = course.PreviewVideoUrl,
            Level = course.Level.ToString(),
            Status = course.Status.ToString(),
            CreatedAt = course.CreatedAt,
            UpdatedAt = course.UpdatedAt,
            PublishedAt = course.PublishedAt,
            EstimatedDurationMinutes = course.EstimatedDurationMinutes,
            Requirements = course.Requirements,
            WhatYouWillLearn = course.WhatYouWillLearn,
            Language = course.Language,
            HasCertificate = course.HasCertificate,
            Slug = course.Slug,
            ViewCount = course.ViewCount,
            AverageRating = course.AverageRating,
            ReviewCount = course.ReviewCount,
            EnrollmentCount = course.EnrollmentCount
        };
    }

    private CourseCardDto MapToCourseCardDto(Course course)
    {
        return new CourseCardDto
        {
            Id = course.Id,
            Title = course.Title,
            ShortDescription = course.ShortDescription,
            InstructorName = $"{course.Instructor?.FirstName ?? ""} {course.Instructor?.LastName ?? ""}".Trim(),
            CategoryName = course.Category?.Name ?? "",
            Price = course.Price,
            CurrentPrice = course.CurrentPrice,
            DiscountPercentage = course.DiscountPercentage,
            ThumbnailUrl = course.ThumbnailUrl,
            Level = course.Level.ToString(),
            Slug = course.Slug,
            AverageRating = course.AverageRating,
            ReviewCount = course.ReviewCount,
            EnrollmentCount = course.EnrollmentCount,
            EstimatedDurationMinutes = course.EstimatedDurationMinutes
        };
    }

    private string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Guid.NewGuid().ToString();
        }

        // Convert to lowercase
        text = text.ToLowerInvariant();

        // Remove special characters
        text = Regex.Replace(text, @"[^a-z0-9\s-]", "");

        // Replace spaces with hyphens
        text = Regex.Replace(text, @"\s+", "-");

        // Remove duplicate hyphens
        text = Regex.Replace(text, @"-+", "-");

        // Trim hyphens from start and end
        text = text.Trim('-');

        // Truncate to reasonable length
        if (text.Length > 100)
        {
            text = text.Substring(0, 100).TrimEnd('-');
        }

        return text;
    }
}
