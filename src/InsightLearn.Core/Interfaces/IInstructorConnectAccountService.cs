using InsightLearn.Core.DTOs.Instructor;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Service interface for managing Stripe Connect accounts for instructor payouts
/// </summary>
public interface IInstructorConnectAccountService
{
    #region Account Management (5 methods)

    /// <summary>
    /// Creates a Stripe Connect account for an instructor
    /// </summary>
    Task<InstructorConnectAccountDto> CreateConnectAccountAsync(Guid instructorId, CreateConnectAccountDto dto);

    /// <summary>
    /// Generates a Stripe onboarding link for KYC verification
    /// </summary>
    Task<string> GenerateOnboardingLinkAsync(Guid instructorId, string returnUrl, string refreshUrl);

    /// <summary>
    /// Gets instructor's Connect account details
    /// </summary>
    Task<InstructorConnectAccountDto?> GetInstructorConnectAccountAsync(Guid instructorId);

    /// <summary>
    /// Updates Connect account status (admin function)
    /// </summary>
    Task<bool> UpdateConnectAccountStatusAsync(Guid instructorId, string status);

    /// <summary>
    /// Soft deletes a Connect account
    /// </summary>
    Task<bool> DeleteConnectAccountAsync(Guid instructorId);

    #endregion

    #region Stripe Synchronization (2 methods)

    /// <summary>
    /// Syncs Connect account status from Stripe API
    /// </summary>
    Task SyncWithStripeAsync(Guid instructorId);

    /// <summary>
    /// Checks if account is ready for payout execution
    /// </summary>
    Task<bool> IsAccountReadyForPayoutAsync(Guid instructorId);

    #endregion

    #region Onboarding Status (2 methods)

    /// <summary>
    /// Gets detailed onboarding status with requirements
    /// </summary>
    Task<OnboardingStatusDto> GetOnboardingStatusAsync(Guid instructorId);

    /// <summary>
    /// Marks onboarding as complete after verification
    /// </summary>
    Task<bool> CompleteOnboardingAsync(Guid instructorId);

    #endregion

    #region Payout Configuration (1 method)

    /// <summary>
    /// Gets Stripe payout schedule configuration
    /// </summary>
    Task<PayoutScheduleDto> GetPayoutScheduleAsync(Guid instructorId);

    #endregion
}
