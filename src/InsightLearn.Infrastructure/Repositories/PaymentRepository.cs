using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InsightLearn.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Payment entity with transaction support
/// </summary>
public class PaymentRepository : IPaymentRepository
{
    private readonly InsightLearnDbContext _context;

    public PaymentRepository(InsightLearnDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Payment>> GetAllAsync(int page = 1, int pageSize = 10)
    {
        return await _context.Payments
            .Include(p => p.User)
            .Include(p => p.Course)
            .Include(p => p.Coupon)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Payment?> GetByIdAsync(Guid id)
    {
        return await _context.Payments
            .Include(p => p.User)
            .Include(p => p.Course)
            .Include(p => p.Coupon)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Payment?> GetByTransactionIdAsync(string transactionId)
    {
        return await _context.Payments
            .Include(p => p.User)
            .Include(p => p.Course)
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId);
    }

    public async Task<IEnumerable<Payment>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Payments
            .Include(p => p.Course)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetByCourseIdAsync(Guid courseId)
    {
        return await _context.Payments
            .Include(p => p.User)
            .Where(p => p.CourseId == courseId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Payment> CreateAsync(Payment payment)
    {
        payment.CreatedAt = DateTime.UtcNow;
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task<Payment> UpdateAsync(Payment payment)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task UpdateStatusAsync(Guid paymentId, PaymentStatus status)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment != null)
        {
            payment.Status = status;
            if (status == PaymentStatus.Completed && payment.ProcessedAt == null)
            {
                payment.ProcessedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }
    }

    public async Task ProcessRefundAsync(Guid paymentId, decimal refundAmount)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment != null)
            {
                payment.RefundAmount = refundAmount;
                payment.RefundedAt = DateTime.UtcNow;
                payment.Status = refundAmount >= payment.Amount
                    ? PaymentStatus.Refunded
                    : PaymentStatus.PartiallyRefunded;

                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<Payment>> GetTransactionsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        PaymentStatus? status = null)
    {
        var query = _context.Payments
            .Include(p => p.User)
            .Include(p => p.Course)
            .AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt <= endDate.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Payments
            .Where(p => p.Status == PaymentStatus.Completed)
            .AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(p => p.ProcessedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(p => p.ProcessedAt <= endDate.Value);
        }

        return await query.SumAsync(p => p.Amount - (p.RefundAmount ?? 0));
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Payments.CountAsync();
    }
}
