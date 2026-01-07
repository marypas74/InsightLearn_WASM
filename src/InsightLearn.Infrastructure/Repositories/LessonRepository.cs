using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InsightLearn.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Lesson entity
/// </summary>
public class LessonRepository : ILessonRepository
{
    private readonly InsightLearnDbContext _context;

    public LessonRepository(InsightLearnDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Lesson>> GetBySectionIdAsync(Guid sectionId)
    {
        return await _context.Lessons
            .Where(l => l.SectionId == sectionId && l.IsActive)
            .OrderBy(l => l.OrderIndex)
            .ToListAsync();
    }

    public async Task<Lesson?> GetByIdAsync(Guid id)
    {
        return await _context.Lessons
            .Include(l => l.Section)
                .ThenInclude(s => s.Course)
            .Include(l => l.SubtitleTracks.Where(st => st.IsActive))
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<Lesson> CreateAsync(Lesson lesson)
    {
        lesson.CreatedAt = DateTime.UtcNow;
        _context.Lessons.Add(lesson);
        await _context.SaveChangesAsync();
        return lesson;
    }

    public async Task<Lesson> UpdateAsync(Lesson lesson)
    {
        _context.Lessons.Update(lesson);
        await _context.SaveChangesAsync();
        return lesson;
    }

    public async Task DeleteAsync(Guid id)
    {
        var lesson = await _context.Lessons.FindAsync(id);
        if (lesson != null)
        {
            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();
        }
    }

    public async Task ReorderAsync(Guid sectionId, List<Guid> lessonIds)
    {
        var lessons = await _context.Lessons
            .Where(l => l.SectionId == sectionId)
            .ToListAsync();

        for (int i = 0; i < lessonIds.Count; i++)
        {
            var lesson = lessons.FirstOrDefault(l => l.Id == lessonIds[i]);
            if (lesson != null)
            {
                lesson.OrderIndex = i;
            }
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Get all lessons that do NOT have COMPLETED or PROCESSING transcripts in MongoDB.
    /// Filters for lessons with VideoUrl (has video) but no VideoTranscriptMetadata entry with Status="Completed" or "Processing".
    /// This prevents duplicate job creation while allowing automatic retry of failed transcripts.
    /// v2.3.23-dev - Part of Batch Transcription System.
    /// v2.3.46-dev - BUGFIX: Added Status="Completed" filter to enable retry of failed transcripts.
    /// v2.3.47-dev - BUGFIX: Exclude Status="Processing" to prevent job duplication (fix for 27 duplicate jobs issue).
    /// </summary>
    public async Task<List<Lesson>> GetLessonsWithoutTranscriptsAsync()
    {
        return await _context.Lessons
            .Where(l => !_context.VideoTranscriptMetadata.Any(vt =>
                vt.LessonId == l.Id &&
                (vt.Status == "Completed" || vt.Status == "Processing")))
            .Where(l => !string.IsNullOrEmpty(l.VideoUrl))
            .OrderBy(l => l.CreatedAt) // Oldest first (FIFO processing)
            .ToListAsync();
    }
}
