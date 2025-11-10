using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InsightLearn.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Section entity
/// </summary>
public class SectionRepository : ISectionRepository
{
    private readonly InsightLearnDbContext _context;

    public SectionRepository(InsightLearnDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Section>> GetByCourseIdAsync(Guid courseId)
    {
        return await _context.Sections
            .Include(s => s.Lessons.OrderBy(l => l.OrderIndex))
            .Where(s => s.CourseId == courseId && s.IsActive)
            .OrderBy(s => s.OrderIndex)
            .ToListAsync();
    }

    public async Task<Section?> GetByIdAsync(Guid id)
    {
        return await _context.Sections
            .Include(s => s.Lessons.OrderBy(l => l.OrderIndex))
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Section> CreateAsync(Section section)
    {
        section.CreatedAt = DateTime.UtcNow;
        _context.Sections.Add(section);
        await _context.SaveChangesAsync();
        return section;
    }

    public async Task<Section> UpdateAsync(Section section)
    {
        _context.Sections.Update(section);
        await _context.SaveChangesAsync();
        return section;
    }

    public async Task DeleteAsync(Guid id)
    {
        var section = await _context.Sections.FindAsync(id);
        if (section != null)
        {
            _context.Sections.Remove(section);
            await _context.SaveChangesAsync();
        }
    }

    public async Task ReorderAsync(Guid courseId, List<Guid> sectionIds)
    {
        var sections = await _context.Sections
            .Where(s => s.CourseId == courseId)
            .ToListAsync();

        for (int i = 0; i < sectionIds.Count; i++)
        {
            var section = sections.FirstOrDefault(s => s.Id == sectionIds[i]);
            if (section != null)
            {
                section.OrderIndex = i;
            }
        }

        await _context.SaveChangesAsync();
    }
}
