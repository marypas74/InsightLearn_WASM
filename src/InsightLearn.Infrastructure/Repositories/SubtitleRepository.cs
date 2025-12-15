using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for SubtitleTrack entity
/// </summary>
public class SubtitleRepository : ISubtitleRepository
{
    private readonly InsightLearnDbContext _context;
    private readonly ILogger<SubtitleRepository> _logger;

    public SubtitleRepository(InsightLearnDbContext context, ILogger<SubtitleRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SubtitleTrack>> GetByLessonIdAsync(Guid lessonId)
    {
        try
        {
            return await _context.SubtitleTracks
                .Where(st => st.LessonId == lessonId && st.IsActive)
                .OrderBy(st => st.Language)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subtitle tracks for lesson {LessonId}", lessonId);
            throw;
        }
    }

    public async Task<List<SubtitleTrack>> GetAllByLessonIdAsync(Guid lessonId)
    {
        try
        {
            return await _context.SubtitleTracks
                .Where(st => st.LessonId == lessonId)
                .OrderBy(st => st.Language)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all subtitle tracks for lesson {LessonId}", lessonId);
            throw;
        }
    }

    public async Task<SubtitleTrack?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _context.SubtitleTracks
                .FirstOrDefaultAsync(st => st.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subtitle track {Id}", id);
            throw;
        }
    }

    public async Task<SubtitleTrack?> GetByLessonAndLanguageAsync(Guid lessonId, string language)
    {
        try
        {
            return await _context.SubtitleTracks
                .FirstOrDefaultAsync(st => st.LessonId == lessonId && st.Language == language && st.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subtitle track for lesson {LessonId} and language {Language}", lessonId, language);
            throw;
        }
    }

    public async Task<SubtitleTrack?> GetDefaultByLessonIdAsync(Guid lessonId)
    {
        try
        {
            return await _context.SubtitleTracks
                .FirstOrDefaultAsync(st => st.LessonId == lessonId && st.IsDefault && st.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default subtitle track for lesson {LessonId}", lessonId);
            throw;
        }
    }

    public async Task<SubtitleTrack> AddAsync(SubtitleTrack subtitleTrack)
    {
        try
        {
            // Validate that the lesson exists
            var lessonExists = await _context.Lessons.AnyAsync(l => l.Id == subtitleTrack.LessonId);
            if (!lessonExists)
            {
                throw new InvalidOperationException($"Lesson with ID {subtitleTrack.LessonId} not found");
            }

            // Check if subtitle already exists for this language
            var exists = await ExistsAsync(subtitleTrack.LessonId, subtitleTrack.Language);
            if (exists)
            {
                throw new InvalidOperationException($"Subtitle track for language '{subtitleTrack.Language}' already exists for this lesson");
            }

            // If this is marked as default, clear other defaults for this lesson
            if (subtitleTrack.IsDefault)
            {
                await ClearDefaultsForLessonAsync(subtitleTrack.LessonId);
            }

            subtitleTrack.CreatedAt = DateTime.UtcNow;
            _context.SubtitleTracks.Add(subtitleTrack);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Added subtitle track {Id} for lesson {LessonId} in language {Language}",
                subtitleTrack.Id, subtitleTrack.LessonId, subtitleTrack.Language);

            return subtitleTrack;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding subtitle track for lesson {LessonId}", subtitleTrack.LessonId);
            throw;
        }
    }

    public async Task<SubtitleTrack> UpdateAsync(SubtitleTrack subtitleTrack)
    {
        try
        {
            var existing = await _context.SubtitleTracks.FindAsync(subtitleTrack.Id);
            if (existing == null)
            {
                throw new InvalidOperationException($"Subtitle track with ID {subtitleTrack.Id} not found");
            }

            // If setting this as default, clear other defaults
            if (subtitleTrack.IsDefault && !existing.IsDefault)
            {
                await ClearDefaultsForLessonAsync(subtitleTrack.LessonId);
            }

            // Update properties
            existing.Label = subtitleTrack.Label;
            existing.FileUrl = subtitleTrack.FileUrl;
            existing.Kind = subtitleTrack.Kind;
            existing.IsDefault = subtitleTrack.IsDefault;
            existing.IsActive = subtitleTrack.IsActive;
            existing.FileSize = subtitleTrack.FileSize;
            existing.CueCount = subtitleTrack.CueCount;
            existing.DurationSeconds = subtitleTrack.DurationSeconds;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated subtitle track {Id} for lesson {LessonId}",
                subtitleTrack.Id, subtitleTrack.LessonId);

            return existing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subtitle track {Id}", subtitleTrack.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var subtitleTrack = await _context.SubtitleTracks.FindAsync(id);
            if (subtitleTrack == null)
            {
                return false;
            }

            _context.SubtitleTracks.Remove(subtitleTrack);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted subtitle track {Id} for lesson {LessonId}",
                id, subtitleTrack.LessonId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subtitle track {Id}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid lessonId, string language)
    {
        try
        {
            return await _context.SubtitleTracks
                .AnyAsync(st => st.LessonId == lessonId && st.Language == language);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if subtitle exists for lesson {LessonId} and language {Language}",
                lessonId, language);
            throw;
        }
    }

    public async Task<bool> SetAsDefaultAsync(Guid id)
    {
        try
        {
            var subtitleTrack = await _context.SubtitleTracks.FindAsync(id);
            if (subtitleTrack == null)
            {
                return false;
            }

            // Clear other defaults for this lesson
            await ClearDefaultsForLessonAsync(subtitleTrack.LessonId);

            // Set this one as default
            subtitleTrack.IsDefault = true;
            subtitleTrack.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Set subtitle track {Id} as default for lesson {LessonId}",
                id, subtitleTrack.LessonId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting subtitle track {Id} as default", id);
            throw;
        }
    }

    public async Task<int> GetCountByLessonIdAsync(Guid lessonId)
    {
        try
        {
            return await _context.SubtitleTracks
                .CountAsync(st => st.LessonId == lessonId && st.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subtitle track count for lesson {LessonId}", lessonId);
            throw;
        }
    }

    /// <summary>
    /// Helper method to clear default flag from all subtitle tracks for a lesson
    /// </summary>
    private async Task ClearDefaultsForLessonAsync(Guid lessonId)
    {
        var currentDefaults = await _context.SubtitleTracks
            .Where(st => st.LessonId == lessonId && st.IsDefault)
            .ToListAsync();

        foreach (var track in currentDefaults)
        {
            track.IsDefault = false;
            track.UpdatedAt = DateTime.UtcNow;
        }

        if (currentDefaults.Any())
        {
            await _context.SaveChangesAsync();
        }
    }
}
