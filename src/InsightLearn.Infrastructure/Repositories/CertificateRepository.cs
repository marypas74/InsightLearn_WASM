using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InsightLearn.Infrastructure.Repositories;

public class CertificateRepository : ICertificateRepository
{
    private readonly InsightLearnDbContext _context;

    public CertificateRepository(InsightLearnDbContext context)
    {
        _context = context;
    }

    public async Task<Certificate> CreateAsync(Certificate certificate)
    {
        _context.Certificates.Add(certificate);
        await _context.SaveChangesAsync();
        return certificate;
    }

    public async Task<Certificate?> GetByIdAsync(Guid id)
    {
        return await _context.Certificates
            .Include(c => c.User)
            .Include(c => c.Course)
            .Include(c => c.Enrollment)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Certificate?> GetByEnrollmentIdAsync(Guid enrollmentId)
    {
        return await _context.Certificates
            .Include(c => c.User)
            .Include(c => c.Course)
            .Include(c => c.Enrollment)
            .FirstOrDefaultAsync(c => c.EnrollmentId == enrollmentId);
    }

    public async Task<Certificate?> GetByCertificateNumberAsync(string certificateNumber)
    {
        return await _context.Certificates
            .Include(c => c.User)
            .Include(c => c.Course)
            .Include(c => c.Enrollment)
            .FirstOrDefaultAsync(c => c.CertificateNumber == certificateNumber);
    }

    public async Task<IEnumerable<Certificate>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Certificates
            .Include(c => c.Course)
            .Include(c => c.Enrollment)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync();
    }

    public async Task<Certificate> UpdateAsync(Certificate certificate)
    {
        _context.Certificates.Update(certificate);
        await _context.SaveChangesAsync();
        return certificate;
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Certificates.CountAsync();
    }
}
