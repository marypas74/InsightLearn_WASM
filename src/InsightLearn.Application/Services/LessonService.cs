using InsightLearn.Core.DTOs.Course;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Services;

/// <summary>
/// Service implementation for Lesson business logic
/// </summary>
public class LessonService : ILessonService
{
    private readonly ILessonRepository _lessonRepository;
    private readonly ISectionRepository _sectionRepository;
    private readonly ILogger<LessonService> _logger;

    public LessonService(
        ILessonRepository lessonRepository,
        ISectionRepository sectionRepository,
        ILogger<LessonService> logger)
    {
        _lessonRepository = lessonRepository;
        _sectionRepository = sectionRepository;
        _logger = logger;
    }

    public async Task<List<LessonDto>> GetSectionLessonsAsync(Guid sectionId)
    {
        try
        {
            _logger.LogInformation("[LessonService] Getting lessons for section {SectionId}", sectionId);

            var lessons = await _lessonRepository.GetBySectionIdAsync(sectionId);
            var dtos = lessons.Select(MapToDto).ToList();

            _logger.LogInformation("[LessonService] Retrieved {Count} lessons for section {SectionId}",
                dtos.Count, sectionId);

            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LessonService] Error getting lessons for section {SectionId}", sectionId);
            throw;
        }
    }

    public async Task<LessonDto?> GetLessonByIdAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[LessonService] Getting lesson by id {LessonId}", id);

            var lesson = await _lessonRepository.GetByIdAsync(id);
            if (lesson == null)
            {
                _logger.LogWarning("[LessonService] Lesson {LessonId} not found", id);
                return null;
            }

            var dto = MapToDto(lesson);
            _logger.LogInformation("[LessonService] Retrieved lesson {LessonId}", id);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LessonService] Error getting lesson {LessonId}", id);
            throw;
        }
    }

    public async Task<LessonDto> CreateLessonAsync(LessonDto dto)
    {
        try
        {
            _logger.LogInformation("[LessonService] Creating lesson for section {SectionId} with title {Title}",
                dto.SectionId, dto.Title);

            // Validate section exists
            var section = await _sectionRepository.GetByIdAsync(dto.SectionId);
            if (section == null)
            {
                _logger.LogError("[LessonService] Section {SectionId} not found", dto.SectionId);
                throw new InvalidOperationException($"Section with id {dto.SectionId} not found");
            }

            // Parse lesson type
            if (!Enum.TryParse<LessonType>(dto.Type, out var lessonType))
            {
                _logger.LogError("[LessonService] Invalid lesson type: {Type}", dto.Type);
                throw new ArgumentException($"Invalid lesson type: {dto.Type}");
            }

            // Validate video-specific requirements
            if (lessonType == LessonType.Video)
            {
                if (string.IsNullOrEmpty(dto.VideoUrl))
                {
                    _logger.LogError("[LessonService] Video URL is required for Video type lessons");
                    throw new ArgumentException("Video URL is required for Video type lessons");
                }

                if (dto.DurationMinutes <= 0)
                {
                    _logger.LogError("[LessonService] Duration must be greater than 0 for Video type lessons");
                    throw new ArgumentException("Duration must be greater than 0 for Video type lessons");
                }
            }

            // Auto-assign OrderIndex if not provided
            var existingLessons = await _lessonRepository.GetBySectionIdAsync(dto.SectionId);
            var lessonCount = existingLessons.Count();

            if (dto.OrderIndex <= 0)
            {
                dto.OrderIndex = lessonCount + 1;
                _logger.LogInformation("[LessonService] Auto-assigned OrderIndex {OrderIndex}", dto.OrderIndex);
            }
            else
            {
                // Ensure OrderIndex is unique
                var existingWithOrder = existingLessons.Any(l => l.OrderIndex == dto.OrderIndex);
                if (existingWithOrder)
                {
                    // Shift other lessons
                    _logger.LogInformation("[LessonService] Shifting lessons to accommodate OrderIndex {OrderIndex}",
                        dto.OrderIndex);

                    var lessonsToShift = existingLessons
                        .Where(l => l.OrderIndex >= dto.OrderIndex)
                        .OrderBy(l => l.OrderIndex)
                        .ToList();

                    foreach (var lesson in lessonsToShift)
                    {
                        lesson.OrderIndex++;
                        await _lessonRepository.UpdateAsync(lesson);
                    }
                }
            }

            var newLesson = new Lesson
            {
                Id = Guid.NewGuid(),
                SectionId = dto.SectionId,
                Title = dto.Title,
                Description = dto.Description,
                Type = lessonType,
                OrderIndex = dto.OrderIndex,
                DurationMinutes = dto.DurationMinutes,
                IsFree = dto.IsFree,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                // Content properties
                VideoUrl = dto.VideoUrl,
                VideoThumbnailUrl = dto.VideoThumbnailUrl,
                ContentText = dto.ContentText,
                AttachmentUrl = dto.AttachmentUrl,
                AttachmentName = dto.AttachmentName,
                // Video-specific properties
                VideoQuality = dto.VideoQuality,
                VideoFileSize = dto.VideoFileSize,
                VideoFormat = dto.VideoFormat
            };

            var created = await _lessonRepository.CreateAsync(newLesson);
            var resultDto = MapToDto(created);

            _logger.LogInformation("[LessonService] Created lesson {LessonId} for section {SectionId}",
                created.Id, dto.SectionId);

            return resultDto;
        }
        catch (Exception ex) when (ex is not InvalidOperationException && ex is not ArgumentException)
        {
            _logger.LogError(ex, "[LessonService] Error creating lesson for section {SectionId}", dto.SectionId);
            throw;
        }
    }

    public async Task<LessonDto?> UpdateLessonAsync(Guid id, LessonDto dto)
    {
        try
        {
            _logger.LogInformation("[LessonService] Updating lesson {LessonId}", id);

            var lesson = await _lessonRepository.GetByIdAsync(id);
            if (lesson == null)
            {
                _logger.LogWarning("[LessonService] Lesson {LessonId} not found for update", id);
                return null;
            }

            // Apply partial updates
            var hasChanges = false;

            if (!string.IsNullOrEmpty(dto.Title) && dto.Title != lesson.Title)
            {
                lesson.Title = dto.Title;
                hasChanges = true;
            }

            if (dto.Description != lesson.Description)
            {
                lesson.Description = dto.Description;
                hasChanges = true;
            }

            // Update type if provided and valid
            if (!string.IsNullOrEmpty(dto.Type) && Enum.TryParse<LessonType>(dto.Type, out var lessonType))
            {
                if (lesson.Type != lessonType)
                {
                    lesson.Type = lessonType;
                    hasChanges = true;

                    // Validate video-specific requirements if changing to Video type
                    if (lessonType == LessonType.Video)
                    {
                        if (string.IsNullOrEmpty(dto.VideoUrl) && string.IsNullOrEmpty(lesson.VideoUrl))
                        {
                            _logger.LogError("[LessonService] Video URL is required when changing to Video type");
                            throw new ArgumentException("Video URL is required for Video type lessons");
                        }

                        if (dto.DurationMinutes <= 0 && lesson.DurationMinutes <= 0)
                        {
                            _logger.LogError("[LessonService] Duration must be greater than 0 for Video type lessons");
                            throw new ArgumentException("Duration must be greater than 0 for Video type lessons");
                        }
                    }
                }
            }

            if (dto.DurationMinutes > 0 && dto.DurationMinutes != lesson.DurationMinutes)
            {
                lesson.DurationMinutes = dto.DurationMinutes;
                hasChanges = true;
            }

            if (dto.IsFree != lesson.IsFree)
            {
                lesson.IsFree = dto.IsFree;
                hasChanges = true;
            }

            // Update content properties
            if (dto.VideoUrl != lesson.VideoUrl)
            {
                lesson.VideoUrl = dto.VideoUrl;
                hasChanges = true;
            }

            if (dto.VideoThumbnailUrl != lesson.VideoThumbnailUrl)
            {
                lesson.VideoThumbnailUrl = dto.VideoThumbnailUrl;
                hasChanges = true;
            }

            if (dto.ContentText != lesson.ContentText)
            {
                lesson.ContentText = dto.ContentText;
                hasChanges = true;
            }

            if (dto.AttachmentUrl != lesson.AttachmentUrl)
            {
                lesson.AttachmentUrl = dto.AttachmentUrl;
                hasChanges = true;
            }

            if (dto.AttachmentName != lesson.AttachmentName)
            {
                lesson.AttachmentName = dto.AttachmentName;
                hasChanges = true;
            }

            // Update video-specific properties
            if (dto.VideoQuality != lesson.VideoQuality)
            {
                lesson.VideoQuality = dto.VideoQuality;
                hasChanges = true;
            }

            if (dto.VideoFileSize != lesson.VideoFileSize)
            {
                lesson.VideoFileSize = dto.VideoFileSize;
                hasChanges = true;
            }

            if (dto.VideoFormat != lesson.VideoFormat)
            {
                lesson.VideoFormat = dto.VideoFormat;
                hasChanges = true;
            }

            // Handle OrderIndex change
            if (dto.OrderIndex > 0 && dto.OrderIndex != lesson.OrderIndex)
            {
                var allLessons = await _lessonRepository.GetBySectionIdAsync(lesson.SectionId);
                var otherLessons = allLessons.Where(l => l.Id != id).ToList();

                if (otherLessons.Any(l => l.OrderIndex == dto.OrderIndex))
                {
                    // Need to reorder
                    _logger.LogInformation("[LessonService] Reordering lessons due to OrderIndex conflict");

                    var oldIndex = lesson.OrderIndex;
                    lesson.OrderIndex = dto.OrderIndex;

                    // Shift lessons between old and new positions
                    if (oldIndex < dto.OrderIndex)
                    {
                        // Moving down - shift lessons up
                        var toShift = otherLessons
                            .Where(l => l.OrderIndex > oldIndex && l.OrderIndex <= dto.OrderIndex)
                            .ToList();

                        foreach (var l in toShift)
                        {
                            l.OrderIndex--;
                            await _lessonRepository.UpdateAsync(l);
                        }
                    }
                    else
                    {
                        // Moving up - shift lessons down
                        var toShift = otherLessons
                            .Where(l => l.OrderIndex >= dto.OrderIndex && l.OrderIndex < oldIndex)
                            .ToList();

                        foreach (var l in toShift)
                        {
                            l.OrderIndex++;
                            await _lessonRepository.UpdateAsync(l);
                        }
                    }
                }
                else
                {
                    lesson.OrderIndex = dto.OrderIndex;
                }

                hasChanges = true;
            }

            if (!hasChanges)
            {
                _logger.LogInformation("[LessonService] No changes to apply for lesson {LessonId}", id);
                return MapToDto(lesson);
            }

            var updated = await _lessonRepository.UpdateAsync(lesson);
            var resultDto = MapToDto(updated);

            _logger.LogInformation("[LessonService] Updated lesson {LessonId}", id);

            return resultDto;
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            _logger.LogError(ex, "[LessonService] Error updating lesson {LessonId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteLessonAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[LessonService] Attempting to delete lesson {LessonId}", id);

            var lesson = await _lessonRepository.GetByIdAsync(id);
            if (lesson == null)
            {
                _logger.LogWarning("[LessonService] Lesson {LessonId} not found for deletion", id);
                return false;
            }

            // Check for progress records
            if (lesson.LessonProgress.Any())
            {
                _logger.LogInformation("[LessonService] Lesson {LessonId} has progress records, performing soft delete", id);

                // Soft delete - just mark as inactive
                lesson.IsActive = false;
                await _lessonRepository.UpdateAsync(lesson);

                _logger.LogInformation("[LessonService] Soft deleted lesson {LessonId}", id);
            }
            else
            {
                _logger.LogInformation("[LessonService] Lesson {LessonId} has no progress records, performing hard delete", id);

                // Hard delete
                await _lessonRepository.DeleteAsync(id);

                // Reorder remaining lessons
                var remainingLessons = await _lessonRepository.GetBySectionIdAsync(lesson.SectionId);
                var orderedLessons = remainingLessons.OrderBy(l => l.OrderIndex).ToList();

                for (int i = 0; i < orderedLessons.Count; i++)
                {
                    if (orderedLessons[i].OrderIndex != i + 1)
                    {
                        orderedLessons[i].OrderIndex = i + 1;
                        await _lessonRepository.UpdateAsync(orderedLessons[i]);
                    }
                }

                _logger.LogInformation("[LessonService] Hard deleted lesson {LessonId}", id);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LessonService] Error deleting lesson {LessonId}", id);
            return false;
        }
    }

    public async Task<bool> ReorderLessonsAsync(Guid sectionId, List<Guid> lessonIds)
    {
        try
        {
            _logger.LogInformation("[LessonService] Reordering {Count} lessons for section {SectionId}",
                lessonIds.Count, sectionId);

            // Validate section exists
            var section = await _sectionRepository.GetByIdAsync(sectionId);
            if (section == null)
            {
                _logger.LogError("[LessonService] Section {SectionId} not found", sectionId);
                return false;
            }

            // Get all lessons for the section
            var lessons = await _lessonRepository.GetBySectionIdAsync(sectionId);
            var lessonsList = lessons.ToList();

            // Validate all provided lesson IDs belong to this section
            var sectionLessonIds = lessonsList.Select(l => l.Id).ToHashSet();
            if (!lessonIds.All(id => sectionLessonIds.Contains(id)))
            {
                _logger.LogError("[LessonService] Some lesson IDs do not belong to section {SectionId}", sectionId);
                return false;
            }

            // Validate all active lessons are included
            var activeLessons = lessonsList.Where(l => l.IsActive).ToList();
            if (lessonIds.Count != activeLessons.Count)
            {
                _logger.LogError("[LessonService] Lesson count mismatch. Expected {Expected}, got {Actual}",
                    activeLessons.Count, lessonIds.Count);
                return false;
            }

            // Perform reordering
            await _lessonRepository.ReorderAsync(sectionId, lessonIds);

            _logger.LogInformation("[LessonService] Successfully reordered lessons for section {SectionId}", sectionId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LessonService] Error reordering lessons for section {SectionId}", sectionId);
            return false;
        }
    }

    private LessonDto MapToDto(Lesson lesson)
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