using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

public interface IInstructorPayoutRepository
{
    Task<InstructorPayout?> GetByIdAsync(Guid id);
    Task<InstructorPayout?> GetByInstructorAndPeriodAsync(Guid instructorId, int month, int year);
    Task<List<InstructorPayout>> GetByInstructorIdAsync(Guid instructorId, int page = 1, int pageSize = 50);
    Task<List<InstructorPayout>> GetPendingPayoutsAsync();
    Task<List<InstructorPayout>> GetPayoutsByPeriodAsync(int month, int year);
    Task<InstructorPayout> CreateAsync(InstructorPayout payout);
    Task<InstructorPayout> UpdateAsync(InstructorPayout payout);
    Task<bool> MarkAsPaidAsync(Guid id, string stripeTransferId);
    Task<bool> MarkAsFailedAsync(Guid id, string errorMessage);
    Task<decimal> GetTotalPaidOutAsync(Guid instructorId);
    Task<decimal> GetMonthlyPlatformRevenueAsync(int month, int year);
}
