using InsightLearn.Core.DTOs.Course;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Services;

/// <summary>
/// Service implementation for Section business logic
/// </summary>
public class SectionService : ISectionService
{
    private readonly ISectionRepository _sectionRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly ILogger<SectionService> _logger;
    private const int MaxSectionsPerCourse = 50;

    public SectionService(
        ISectionRepository sectionRepository,
        ICourseRepository courseRepository,
        ILogger<SectionService> logger)
    {
        _sectionRepository = sectionRepository;
        _courseRepository = courseRepository;
        _logger = logger;
    }

    public async Task<List<SectionDto>> GetCourseSectionsAsync(Guid courseId)
    {
        try
        {
            _logger.LogInformation("[SectionService] Getting sections for course {CourseId}", courseId);

            var sections = await _sectionRepository.GetByCourseIdAsync(courseId);
            var dtos = sections.Select(MapToDto).ToList();

            _logger.LogInformation("[SectionService] Retrieved {Count} sections for course {CourseId}",
                dtos.Count, courseId);

            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SectionService] Error getting sections for course {CourseId}", courseId);
            throw;
        }
    }

    public async Task<SectionDto?> GetSectionByIdAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[SectionService] Getting section by id {SectionId}", id);

            var section = await _sectionRepository.GetByIdAsync(id);
            if (section == null)
            {
                _logger.LogWarning("[SectionService] Section {SectionId} not found", id);
                return null;
            }

            var dto = MapToDto(section);
            _logger.LogInformation("[SectionService] Retrieved section {SectionId}", id);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SectionService] Error getting section {SectionId}", id);
            throw;
        }
    }

    public async Task<SectionDto> CreateSectionAsync(Guid courseId, string title, string? description, int orderIndex)
    {
        try
        {
            _logger.LogInformation("[SectionService] Creating section for course {CourseId} with title {Title}",
                courseId, title);

            // Validate course exists
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course == null)
            {
                _logger.LogError("[SectionService] Course {CourseId} not found", courseId);
                throw new InvalidOperationException($"Course with id {courseId} not found");
            }

            // Check section limit
            var existingSections = await _sectionRepository.GetByCourseIdAsync(courseId);
            var sectionCount = existingSections.Count();

            if (sectionCount >= MaxSectionsPerCourse)
            {
                _logger.LogError("[SectionService] Course {CourseId} already has maximum sections ({Max})",
                    courseId, MaxSectionsPerCourse);
                throw new InvalidOperationException($"Course cannot have more than {MaxSectionsPerCourse} sections");
            }

            // Auto-assign OrderIndex if not provided
            if (orderIndex <= 0)
            {
                orderIndex = sectionCount + 1;
                _logger.LogInformation("[SectionService] Auto-assigned OrderIndex {OrderIndex}", orderIndex);
            }
            else
            {
                // Ensure OrderIndex is unique
                var existingWithOrder = existingSections.Any(s => s.OrderIndex == orderIndex);
                if (existingWithOrder)
                {
                    // Shift other sections
                    _logger.LogInformation("[SectionService] Shifting sections to accommodate OrderIndex {OrderIndex}",
                        orderIndex);

                    var sectionsToShift = existingSections
                        .Where(s => s.OrderIndex >= orderIndex)
                        .OrderBy(s => s.OrderIndex)
                        .ToList();

                    foreach (var section in sectionsToShift)
                    {
                        section.OrderIndex++;
                        await _sectionRepository.UpdateAsync(section);
                    }
                }
            }

            var newSection = new Section
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                Title = title,
                Description = description,
                OrderIndex = orderIndex,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _sectionRepository.CreateAsync(newSection);
            var dto = MapToDto(created);

            _logger.LogInformation("[SectionService] Created section {SectionId} for course {CourseId}",
                created.Id, courseId);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SectionService] Error creating section for course {CourseId}", courseId);
            throw;
        }
    }

    public async Task<SectionDto?> UpdateSectionAsync(Guid id, string? title, string? description, int? orderIndex)
    {
        try
        {
            _logger.LogInformation("[SectionService] Updating section {SectionId}", id);

            var section = await _sectionRepository.GetByIdAsync(id);
            if (section == null)
            {
                _logger.LogWarning("[SectionService] Section {SectionId} not found for update", id);
                return null;
            }

            // Apply partial updates
            var hasChanges = false;

            if (!string.IsNullOrEmpty(title) && title != section.Title)
            {
                section.Title = title;
                hasChanges = true;
            }

            if (description != section.Description)
            {
                section.Description = description;
                hasChanges = true;
            }

            if (orderIndex.HasValue && orderIndex.Value > 0 && orderIndex.Value != section.OrderIndex)
            {
                // Validate and handle OrderIndex change
                var allSections = await _sectionRepository.GetByCourseIdAsync(section.CourseId);
                var otherSections = allSections.Where(s => s.Id != id).ToList();

                if (otherSections.Any(s => s.OrderIndex == orderIndex.Value))
                {
                    // Need to reorder
                    _logger.LogInformation("[SectionService] Reordering sections due to OrderIndex conflict");

                    var oldIndex = section.OrderIndex;
                    section.OrderIndex = orderIndex.Value;

                    // Shift sections between old and new positions
                    if (oldIndex < orderIndex.Value)
                    {
                        // Moving down - shift sections up
                        var toShift = otherSections
                            .Where(s => s.OrderIndex > oldIndex && s.OrderIndex <= orderIndex.Value)
                            .ToList();

                        foreach (var s in toShift)
                        {
                            s.OrderIndex--;
                            await _sectionRepository.UpdateAsync(s);
                        }
                    }
                    else
                    {
                        // Moving up - shift sections down
                        var toShift = otherSections
                            .Where(s => s.OrderIndex >= orderIndex.Value && s.OrderIndex < oldIndex)
                            .ToList();

                        foreach (var s in toShift)
                        {
                            s.OrderIndex++;
                            await _sectionRepository.UpdateAsync(s);
                        }
                    }
                }
                else
                {
                    section.OrderIndex = orderIndex.Value;
                }

                hasChanges = true;
            }

            if (!hasChanges)
            {
                _logger.LogInformation("[SectionService] No changes to apply for section {SectionId}", id);
                return MapToDto(section);
            }

            var updated = await _sectionRepository.UpdateAsync(section);
            var dto = MapToDto(updated);

            _logger.LogInformation("[SectionService] Updated section {SectionId}", id);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SectionService] Error updating section {SectionId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteSectionAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[SectionService] Attempting to delete section {SectionId}", id);

            var section = await _sectionRepository.GetByIdAsync(id);
            if (section == null)
            {
                _logger.LogWarning("[SectionService] Section {SectionId} not found for deletion", id);
                return false;
            }

            // Check for lessons
            if (section.Lessons.Any(l => l.IsActive))
            {
                _logger.LogError("[SectionService] Cannot delete section {SectionId} as it has active lessons", id);
                throw new InvalidOperationException("Cannot delete section that contains lessons. Delete or move lessons first.");
            }

            // Delete the section
            await _sectionRepository.DeleteAsync(id);

            // Reorder remaining sections
            var remainingSections = await _sectionRepository.GetByCourseIdAsync(section.CourseId);
            var orderedSections = remainingSections.OrderBy(s => s.OrderIndex).ToList();

            for (int i = 0; i < orderedSections.Count; i++)
            {
                if (orderedSections[i].OrderIndex != i + 1)
                {
                    orderedSections[i].OrderIndex = i + 1;
                    await _sectionRepository.UpdateAsync(orderedSections[i]);
                }
            }

            _logger.LogInformation("[SectionService] Successfully deleted section {SectionId}", id);

            return true;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SectionService] Error deleting section {SectionId}", id);
            return false;
        }
    }

    public async Task<bool> ReorderSectionsAsync(Guid courseId, List<Guid> sectionIds)
    {
        try
        {
            _logger.LogInformation("[SectionService] Reordering {Count} sections for course {CourseId}",
                sectionIds.Count, courseId);

            // Validate course exists
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course == null)
            {
                _logger.LogError("[SectionService] Course {CourseId} not found", courseId);
                return false;
            }

            // Get all sections for the course
            var sections = await _sectionRepository.GetByCourseIdAsync(courseId);
            var sectionsList = sections.ToList();

            // Validate all provided section IDs belong to this course
            var courseSectionIds = sectionsList.Select(s => s.Id).ToHashSet();
            if (!sectionIds.All(id => courseSectionIds.Contains(id)))
            {
                _logger.LogError("[SectionService] Some section IDs do not belong to course {CourseId}", courseId);
                return false;
            }

            // Validate all sections are included
            if (sectionIds.Count != sectionsList.Count)
            {
                _logger.LogError("[SectionService] Section count mismatch. Expected {Expected}, got {Actual}",
                    sectionsList.Count, sectionIds.Count);
                return false;
            }

            // Perform reordering
            await _sectionRepository.ReorderAsync(courseId, sectionIds);

            _logger.LogInformation("[SectionService] Successfully reordered sections for course {CourseId}", courseId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SectionService] Error reordering sections for course {CourseId}", courseId);
            return false;
        }
    }

    private SectionDto MapToDto(Section section)
    {
        var dto = new SectionDto
        {
            Id = section.Id,
            CourseId = section.CourseId,
            Title = section.Title,
            Description = section.Description,
            OrderIndex = section.OrderIndex,
            IsActive = section.IsActive,
            Lessons = new List<LessonDto>()
        };

        // Map lessons if loaded
        if (section.Lessons != null && section.Lessons.Any())
        {
            dto.Lessons = section.Lessons
                .Where(l => l.IsActive)
                .OrderBy(l => l.OrderIndex)
                .Select(MapLessonToDto)
                .ToList();
        }

        return dto;
    }

    private LessonDto MapLessonToDto(Lesson lesson)
    {
        return new LessonDto
        {
            Id = lesson.Id,
            SectionId = lesson.SectionId,
            Title = lesson.Title,
            Description = lesson.Description,
            Type = lesson.Type.ToString(),
            OrderIndex = lesson.OrderIndex,
            DurationMinutes = lesson.DurationMinutes,
            IsFree = lesson.IsFree,
            IsActive = lesson.IsActive,
            VideoUrl = lesson.VideoUrl,
            VideoThumbnailUrl = lesson.VideoThumbnailUrl,
            ContentText = lesson.ContentText,
            AttachmentUrl = lesson.AttachmentUrl,
            AttachmentName = lesson.AttachmentName,
            VideoQuality = lesson.VideoQuality,
            VideoFileSize = lesson.VideoFileSize,
            VideoFormat = lesson.VideoFormat
        };
    }
}