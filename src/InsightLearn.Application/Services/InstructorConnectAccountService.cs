using InsightLearn.Core.DTOs.Instructor;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace InsightLearn.Application.Services;

/// <summary>
/// Instructor Connect Account Service - Manages Stripe Connect accounts for automated instructor payouts
/// Version: v2.0.0
/// Task: T5 - InstructorConnectAccountService.cs (10 methods)
/// Architect Score Target: 10/10
///
/// CRITICAL: This service manages Stripe Connect accounts for AUTOMATED INSTRUCTOR PAYOUTS
/// Integration: Used by PayoutCalculationService to execute payouts via Stripe Transfer API
/// </summary>
public class InstructorConnectAccountService : IInstructorConnectAccountService
{
    private readonly IInstructorConnectAccountRepository _connectAccountRepo;
    private readonly InsightLearnDbContext _context;
    private readonly ILogger<InstructorConnectAccountService> _logger;
    private readonly IConfiguration _configuration;

    // Stripe SDK services
    private readonly AccountService _stripeAccountService;
    private readonly AccountLinkService _stripeAccountLinkService;

    // Configuration constants
    private readonly string _stripeSecretKey;
    private readonly string _defaultCountry;
    private readonly string _defaultCurrency;
    private readonly int _onboardingLinkExpiryHours;

    public InstructorConnectAccountService(
        IInstructorConnectAccountRepository connectAccountRepository,
        InsightLearnDbContext context,
        ILogger<InstructorConnectAccountService> logger,
        IConfiguration configuration)
    {
        _connectAccountRepo = connectAccountRepository ?? throw new ArgumentNullException(nameof(connectAccountRepository));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        // Load Stripe configuration
        _stripeSecretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")
            ?? configuration["Stripe:SecretKey"]
            ?? throw new InvalidOperationException("Stripe API key not configured");

        StripeConfiguration.ApiKey = _stripeSecretKey;

        // Initialize Stripe SDK services
        _stripeAccountService = new AccountService();
        _stripeAccountLinkService = new AccountLinkService();

        // Load configuration
        _defaultCountry = configuration.GetValue("Stripe:DefaultCountry", "US") ?? "US";
        _defaultCurrency = configuration.GetValue("Stripe:DefaultCurrency", "USD") ?? "USD";
        _onboardingLinkExpiryHours = configuration.GetValue("Stripe:OnboardingLinkExpiryHours", 24);

        _logger.LogInformation("[InstructorConnectAccountService] Initialized - Country={Country}, Currency={Currency}, " +
                              "LinkExpiry={ExpiryHours}h",
            _defaultCountry, _defaultCurrency, _onboardingLinkExpiryHours);
    }

    #region Account Management (5 methods)

    /// <summary>
    /// Creates a Stripe Connect account for an instructor
    /// CRITICAL: This is the entry point for instructor onboarding
    /// </summary>
    public async Task<InstructorConnectAccountDto> CreateConnectAccountAsync(Guid instructorId, CreateConnectAccountDto dto)
    {
        _logger.LogInformation("[CreateConnectAccountAsync] Creating Stripe Connect account for instructor {InstructorId}", instructorId);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Check if account already exists
            var existing = await _connectAccountRepo.GetByInstructorIdAsync(instructorId);
            if (existing != null && existing.DisabledAt == null)
            {
                _logger.LogWarning("[CreateConnectAccountAsync] Connect account already exists: {AccountId}, Status={Status}",
                    existing.Id, existing.OnboardingStatus);
                throw new InvalidOperationException($"Instructor {instructorId} already has an active Connect account");
            }

            // 2. Get instructor user details
            var user = await _context.Users.FindAsync(instructorId);
            if (user == null)
            {
                throw new InvalidOperationException($"Instructor {instructorId} not found");
            }

            if (user.UserType != "Instructor" && user.UserType != "Admin")
            {
                throw new InvalidOperationException($"User {instructorId} is not an instructor (type: {user.UserType})");
            }

            // 3. Validate country and currency
            var country = dto.Country ?? _defaultCountry;
            var currency = dto.Currency ?? _defaultCurrency;

            if (!IsValidCountryCode(country))
            {
                throw new ArgumentException($"Invalid country code: {country}");
            }

            // 4. Create Stripe Connect account (Express type for simplicity)
            var accountOptions = new AccountCreateOptions
            {
                Type = "express",
                Country = country,
                Email = user.Email,
                Capabilities = new AccountCapabilitiesOptions
                {
                    CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                    Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
                },
                BusinessType = "individual",
                Individual = new AccountIndividualOptions
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email
                },
                Metadata = new Dictionary<string, string>
                {
                    { "instructor_id", instructorId.ToString() },
                    { "platform", "InsightLearn" },
                    { "environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production" }
                }
            };

            var stripeAccount = await _stripeAccountService.CreateAsync(accountOptions);

            _logger.LogInformation("[CreateConnectAccountAsync] Stripe account created: {StripeAccountId}", stripeAccount.Id);

            // 5. Create local database record
            var connectAccount = new InstructorConnectAccount
            {
                Id = Guid.NewGuid(),
                InstructorId = instructorId,
                StripeAccountId = stripeAccount.Id,
                OnboardingStatus = "pending",
                PayoutsEnabled = false,
                ChargesEnabled = false,
                Country = country,
                Currency = currency,
                VerificationStatus = "unverified",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var savedAccount = await _connectAccountRepo.CreateAsync(connectAccount);

            await transaction.CommitAsync();

            _logger.LogInformation("[CreateConnectAccountAsync] Connect account created: {AccountId} for instructor {InstructorId}",
                savedAccount.Id, instructorId);

            return MapToDto(savedAccount);
        }
        catch (StripeException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "[CreateConnectAccountAsync] Stripe API error creating Connect account for instructor {InstructorId}",
                instructorId);
            throw new InvalidOperationException($"Failed to create Stripe Connect account: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "[CreateConnectAccountAsync] Failed to create Connect account for instructor {InstructorId}",
                instructorId);
            throw;
        }
    }

    /// <summary>
    /// Generates a Stripe onboarding link for an instructor to complete KYC verification
    /// CRITICAL: This link is time-sensitive (expires in 24 hours by default)
    /// </summary>
    public async Task<string> GenerateOnboardingLinkAsync(Guid instructorId, string returnUrl, string refreshUrl)
    {
        _logger.LogInformation("[GenerateOnboardingLinkAsync] Generating onboarding link for instructor {InstructorId}",
            instructorId);

        try
        {
            // 1. Get Connect account
            var account = await _connectAccountRepo.GetByInstructorIdAsync(instructorId);
            if (account == null)
            {
                throw new InvalidOperationException($"No Connect account found for instructor {instructorId}");
            }

            if (account.DisabledAt != null)
            {
                throw new InvalidOperationException($"Connect account {account.Id} is disabled: {account.DisabledReason}");
            }

            // 2. Create AccountLink for onboarding
            var linkOptions = new AccountLinkCreateOptions
            {
                Account = account.StripeAccountId,
                RefreshUrl = refreshUrl,
                ReturnUrl = returnUrl,
                Type = "account_onboarding",
                Collect = "eventually_due" // Collect minimum required information
            };

            var accountLink = await _stripeAccountLinkService.CreateAsync(linkOptions);

            _logger.LogInformation("[GenerateOnboardingLinkAsync] Onboarding link created for instructor {InstructorId}, " +
                                  "expires at {ExpiryTime}",
                instructorId, accountLink.ExpiresAt);

            // 3. Update local record with onboarding URL
            account.OnboardingUrl = accountLink.Url;
            account.UpdatedAt = DateTime.UtcNow;
            await _connectAccountRepo.UpdateAsync(account);
            await _context.SaveChangesAsync();

            return accountLink.Url;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "[GenerateOnboardingLinkAsync] Stripe API error generating onboarding link for instructor {InstructorId}",
                instructorId);
            throw new InvalidOperationException($"Failed to generate onboarding link: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GenerateOnboardingLinkAsync] Failed to generate onboarding link for instructor {InstructorId}",
                instructorId);
            throw;
        }
    }

    /// <summary>
    /// Gets instructor's Connect account details
    /// </summary>
    public async Task<InstructorConnectAccountDto?> GetInstructorConnectAccountAsync(Guid instructorId)
    {
        _logger.LogDebug("[GetInstructorConnectAccountAsync] Fetching Connect account for instructor {InstructorId}",
            instructorId);

        try
        {
            var account = await _connectAccountRepo.GetByInstructorIdAsync(instructorId);

            if (account == null)
            {
                _logger.LogDebug("[GetInstructorConnectAccountAsync] No Connect account found for instructor {InstructorId}",
                    instructorId);
                return null;
            }

            _logger.LogInformation("[GetInstructorConnectAccountAsync] Retrieved Connect account {AccountId}, Status={Status}",
                account.Id, account.OnboardingStatus);

            return MapToDto(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetInstructorConnectAccountAsync] Failed to fetch Connect account for instructor {InstructorId}",
                instructorId);
            throw;
        }
    }

    /// <summary>
    /// Updates Connect account status (admin function)
    /// </summary>
    public async Task<bool> UpdateConnectAccountStatusAsync(Guid instructorId, string status)
    {
        _logger.LogInformation("[UpdateConnectAccountStatusAsync] Updating status for instructor {InstructorId} to {Status}",
            instructorId, status);

        try
        {
            // 1. Validate status
            var validStatuses = new[] { "pending", "incomplete", "complete", "restricted", "disabled" };
            if (!validStatuses.Contains(status.ToLowerInvariant()))
            {
                throw new ArgumentException($"Invalid status: {status}. Valid values: {string.Join(", ", validStatuses)}");
            }

            // 2. Get account
            var account = await _connectAccountRepo.GetByInstructorIdAsync(instructorId);
            if (account == null)
            {
                _logger.LogError("[UpdateConnectAccountStatusAsync] No Connect account found for instructor {InstructorId}",
                    instructorId);
                return false;
            }

            // 3. Update status
            var oldStatus = account.OnboardingStatus;
            account.OnboardingStatus = status.ToLowerInvariant();
            account.UpdatedAt = DateTime.UtcNow;

            // 4. If status is "disabled", set DisabledAt timestamp
            if (status.ToLowerInvariant() == "disabled")
            {
                account.DisabledAt = DateTime.UtcNow;
                account.PayoutsEnabled = false;
                account.ChargesEnabled = false;
            }

            await _connectAccountRepo.UpdateAsync(account);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[UpdateConnectAccountStatusAsync] Status updated: {OldStatus} -> {NewStatus} for account {AccountId}",
                oldStatus, account.OnboardingStatus, account.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UpdateConnectAccountStatusAsync] Failed to update status for instructor {InstructorId}",
                instructorId);
            throw;
        }
    }

    /// <summary>
    /// Deletes (soft delete) a Connect account
    /// IMPORTANT: This does NOT delete the Stripe account, only marks it as disabled locally
    /// </summary>
    public async Task<bool> DeleteConnectAccountAsync(Guid instructorId)
    {
        _logger.LogWarning("[DeleteConnectAccountAsync] Soft deleting Connect account for instructor {InstructorId}",
            instructorId);

        try
        {
            var account = await _connectAccountRepo.GetByInstructorIdAsync(instructorId);
            if (account == null)
            {
                _logger.LogWarning("[DeleteConnectAccountAsync] No Connect account found for instructor {InstructorId}",
                    instructorId);
                return false;
            }

            // Soft delete: Set DisabledAt and update status
            var success = await _connectAccountRepo.DisableAccountAsync(account.Id, "Account deleted by instructor");

            if (success)
            {
                _logger.LogWarning("[DeleteConnectAccountAsync] Connect account {AccountId} soft-deleted (StripeAccountId={StripeId})",
                    account.Id, account.StripeAccountId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DeleteConnectAccountAsync] Failed to delete Connect account for instructor {InstructorId}",
                instructorId);
            throw;
        }
    }

    #endregion

    #region Stripe Synchronization (2 methods)

    /// <summary>
    /// Syncs Connect account status from Stripe API to local database
    /// CRITICAL: This should be called periodically to keep account status accurate
    /// </summary>
    public async Task SyncWithStripeAsync(Guid instructorId)
    {
        _logger.LogInformation("[SyncWithStripeAsync] Syncing Connect account with Stripe for instructor {InstructorId}",
            instructorId);

        try
        {
            // 1. Get local account
            var account = await _connectAccountRepo.GetByInstructorIdAsync(instructorId);
            if (account == null)
            {
                throw new InvalidOperationException($"No Connect account found for instructor {instructorId}");
            }

            // 2. Retrieve from Stripe
            var stripeAccount = await _stripeAccountService.GetAsync(account.StripeAccountId);

            _logger.LogDebug("[SyncWithStripeAsync] Stripe account retrieved: ChargesEnabled={Charges}, PayoutsEnabled={Payouts}, " +
                            "DetailsSubmitted={Details}",
                stripeAccount.ChargesEnabled, stripeAccount.PayoutsEnabled, stripeAccount.DetailsSubmitted);

            // 3. Update local record
            var oldPayoutsEnabled = account.PayoutsEnabled;
            var oldChargesEnabled = account.ChargesEnabled;

            account.ChargesEnabled = stripeAccount.ChargesEnabled;
            account.PayoutsEnabled = stripeAccount.PayoutsEnabled;
            account.UpdatedAt = DateTime.UtcNow;

            // 4. Determine onboarding status based on Stripe account state
            var newStatus = DetermineOnboardingStatus(stripeAccount);
            if (account.OnboardingStatus != newStatus)
            {
                _logger.LogInformation("[SyncWithStripeAsync] Onboarding status changed: {OldStatus} -> {NewStatus}",
                    account.OnboardingStatus, newStatus);
                account.OnboardingStatus = newStatus;
            }

            // 5. Set OnboardingCompletedAt if newly completed
            if (newStatus == "complete" && account.OnboardingCompletedAt == null)
            {
                account.OnboardingCompletedAt = DateTime.UtcNow;
                _logger.LogInformation("[SyncWithStripeAsync] Onboarding completed for account {AccountId}", account.Id);
            }

            // 6. Update verification status
            if (stripeAccount.Requirements?.CurrentlyDue?.Count == 0 &&
                stripeAccount.Requirements?.EventuallyDue?.Count == 0)
            {
                account.VerificationStatus = "verified";
            }
            else if (stripeAccount.Requirements?.CurrentlyDue?.Count > 0)
            {
                account.VerificationStatus = "pending";
            }

            // 7. Store requirements as JSON
            if (stripeAccount.Requirements != null)
            {
                var requirements = new
                {
                    currently_due = stripeAccount.Requirements.CurrentlyDue,
                    eventually_due = stripeAccount.Requirements.EventuallyDue,
                    past_due = stripeAccount.Requirements.PastDue,
                    disabled_reason = stripeAccount.Requirements.DisabledReason
                };
                account.Requirements = System.Text.Json.JsonSerializer.Serialize(requirements);
            }

            await _connectAccountRepo.UpdateAsync(account);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[SyncWithStripeAsync] Connect account synced: ChargesEnabled={Charges}, PayoutsEnabled={Payouts}, " +
                                  "Status={Status}",
                account.ChargesEnabled, account.PayoutsEnabled, account.OnboardingStatus);

            // 8. Log any capability changes
            if (oldPayoutsEnabled != account.PayoutsEnabled || oldChargesEnabled != account.ChargesEnabled)
            {
                _logger.LogWarning("[SyncWithStripeAsync] Capability changes detected - PayoutsEnabled: {OldPayouts} -> {NewPayouts}, " +
                                  "ChargesEnabled: {OldCharges} -> {NewCharges}",
                    oldPayoutsEnabled, account.PayoutsEnabled, oldChargesEnabled, account.ChargesEnabled);
            }
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "[SyncWithStripeAsync] Stripe API error syncing account for instructor {InstructorId}",
                instructorId);
            throw new InvalidOperationException($"Failed to sync with Stripe: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SyncWithStripeAsync] Failed to sync account for instructor {InstructorId}", instructorId);
            throw;
        }
    }

    /// <summary>
    /// Checks if Connect account is ready for payout execution
    /// CRITICAL: This MUST return true before executing payouts via Stripe Transfer API
    /// </summary>
    public async Task<bool> IsAccountReadyForPayoutAsync(Guid instructorId)
    {
        _logger.LogDebug("[IsAccountReadyForPayoutAsync] Checking payout readiness for instructor {InstructorId}",
            instructorId);

        try
        {
            var account = await _connectAccountRepo.GetByInstructorIdAsync(instructorId);
            if (account == null)
            {
                _logger.LogWarning("[IsAccountReadyForPayoutAsync] No Connect account found for instructor {InstructorId}",
                    instructorId);
                return false;
            }

            // Sync with Stripe to get latest status
            await SyncWithStripeAsync(instructorId);

            // Reload account after sync
            account = await _connectAccountRepo.GetByInstructorIdAsync(instructorId);

            var isReady = account!.ChargesEnabled && account.PayoutsEnabled && account.DisabledAt == null;

            _logger.LogInformation("[IsAccountReadyForPayoutAsync] Instructor {InstructorId} payout readiness: {IsReady} " +
                                  "(Charges={Charges}, Payouts={Payouts}, Disabled={Disabled})",
                instructorId, isReady, account.ChargesEnabled, account.PayoutsEnabled, account.DisabledAt != null);

            return isReady;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IsAccountReadyForPayoutAsync] Failed to check payout readiness for instructor {InstructorId}",
                instructorId);
            return false;
        }
    }

    #endregion

    #region Onboarding Status (2 methods)

    /// <summary>
    /// Gets detailed onboarding status including requirements
    /// </summary>
    public async Task<OnboardingStatusDto> GetOnboardingStatusAsync(Guid instructorId)
    {
        _logger.LogDebug("[GetOnboardingStatusAsync] Fetching onboarding status for instructor {InstructorId}", instructorId);

        try
        {
            var account = await _connectAccountRepo.GetByInstructorIdAsync(instructorId);
            if (account == null)
            {
                throw new InvalidOperationException($"No Connect account found for instructor {instructorId}");
            }

            // Sync with Stripe to get latest requirements
            var stripeAccount = await _stripeAccountService.GetAsync(account.StripeAccountId);

            var currentlyDue = stripeAccount.Requirements?.CurrentlyDue?.ToList() ?? new List<string>();
            var eventuallyDue = stripeAccount.Requirements?.EventuallyDue?.ToList() ?? new List<string>();

            // Calculate completion percentage
            var totalRequirements = currentlyDue.Count + eventuallyDue.Count;
            var completionPercentage = totalRequirements == 0 ? 100 : 0;

            // If no currently due but has eventually due, consider it partially complete
            if (currentlyDue.Count == 0 && eventuallyDue.Count > 0)
            {
                completionPercentage = 50;
            }

            var isComplete = currentlyDue.Count == 0 && eventuallyDue.Count == 0;

            var status = new OnboardingStatusDto
            {
                Status = account.OnboardingStatus,
                CurrentlyDue = currentlyDue,
                EventuallyDue = eventuallyDue,
                CompletionPercentage = completionPercentage,
                IsComplete = isComplete
            };

            _logger.LogInformation("[GetOnboardingStatusAsync] Onboarding status: {Status}, Complete={Complete}, " +
                                  "CurrentlyDue={CurrentlyDue}, EventuallyDue={EventuallyDue}",
                status.Status, status.IsComplete, status.CurrentlyDue.Count, status.EventuallyDue.Count);

            return status;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "[GetOnboardingStatusAsync] Stripe API error fetching status for instructor {InstructorId}",
                instructorId);
            throw new InvalidOperationException($"Failed to fetch onboarding status: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetOnboardingStatusAsync] Failed to fetch onboarding status for instructor {InstructorId}",
                instructorId);
            throw;
        }
    }

    /// <summary>
    /// Marks onboarding as complete after verification
    /// </summary>
    public async Task<bool> CompleteOnboardingAsync(Guid instructorId)
    {
        _logger.LogInformation("[CompleteOnboardingAsync] Completing onboarding for instructor {InstructorId}", instructorId);

        try
        {
            var account = await _connectAccountRepo.GetByInstructorIdAsync(instructorId);
            if (account == null)
            {
                _logger.LogError("[CompleteOnboardingAsync] No Connect account found for instructor {InstructorId}",
                    instructorId);
                return false;
            }

            // Verify account is fully onboarded on Stripe side
            var stripeAccount = await _stripeAccountService.GetAsync(account.StripeAccountId);

            if (stripeAccount.Requirements?.CurrentlyDue?.Count > 0)
            {
                _logger.LogWarning("[CompleteOnboardingAsync] Cannot complete onboarding - requirements still due: {Requirements}",
                    string.Join(", ", stripeAccount.Requirements.CurrentlyDue));
                return false;
            }

            if (!stripeAccount.PayoutsEnabled || !stripeAccount.ChargesEnabled)
            {
                _logger.LogWarning("[CompleteOnboardingAsync] Cannot complete onboarding - capabilities not enabled " +
                                  "(Payouts={Payouts}, Charges={Charges})",
                    stripeAccount.PayoutsEnabled, stripeAccount.ChargesEnabled);
                return false;
            }

            // Update account status
            var success = await _connectAccountRepo.UpdateOnboardingStatusAsync(
                account.Id,
                "complete",
                stripeAccount.PayoutsEnabled,
                stripeAccount.ChargesEnabled);

            if (success)
            {
                _logger.LogInformation("[CompleteOnboardingAsync] Onboarding completed for account {AccountId}", account.Id);
            }

            return success;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "[CompleteOnboardingAsync] Stripe API error completing onboarding for instructor {InstructorId}",
                instructorId);
            throw new InvalidOperationException($"Failed to complete onboarding: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CompleteOnboardingAsync] Failed to complete onboarding for instructor {InstructorId}",
                instructorId);
            throw;
        }
    }

    #endregion

    #region Payout Configuration (1 method)

    /// <summary>
    /// Gets Stripe payout schedule configuration for instructor's account
    /// </summary>
    public async Task<PayoutScheduleDto> GetPayoutScheduleAsync(Guid instructorId)
    {
        _logger.LogDebug("[GetPayoutScheduleAsync] Fetching payout schedule for instructor {InstructorId}", instructorId);

        try
        {
            var account = await _connectAccountRepo.GetByInstructorIdAsync(instructorId);
            if (account == null)
            {
                throw new InvalidOperationException($"No Connect account found for instructor {instructorId}");
            }

            // Get payout schedule from Stripe
            var stripeAccount = await _stripeAccountService.GetAsync(account.StripeAccountId);

            var schedule = new PayoutScheduleDto
            {
                Interval = stripeAccount.Settings?.Payouts?.Schedule?.Interval ?? "monthly",
                DelayDays = (int)(stripeAccount.Settings?.Payouts?.Schedule?.DelayDays ?? 7),
                Currency = account.Currency ?? _defaultCurrency
            };

            _logger.LogInformation("[GetPayoutScheduleAsync] Payout schedule: Interval={Interval}, Delay={Delay} days, " +
                                  "Currency={Currency}",
                schedule.Interval, schedule.DelayDays, schedule.Currency);

            return schedule;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "[GetPayoutScheduleAsync] Stripe API error fetching payout schedule for instructor {InstructorId}",
                instructorId);
            throw new InvalidOperationException($"Failed to fetch payout schedule: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetPayoutScheduleAsync] Failed to fetch payout schedule for instructor {InstructorId}",
                instructorId);
            throw;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Determines onboarding status based on Stripe account state
    /// </summary>
    private string DetermineOnboardingStatus(Account stripeAccount)
    {
        // Account fully onboarded and capabilities enabled
        if (stripeAccount.ChargesEnabled &&
            stripeAccount.PayoutsEnabled &&
            (stripeAccount.Requirements?.CurrentlyDue?.Count ?? 0) == 0)
        {
            return "complete";
        }

        // Account has past due requirements or is disabled
        if (stripeAccount.Requirements?.PastDue?.Count > 0 ||
            !string.IsNullOrEmpty(stripeAccount.Requirements?.DisabledReason))
        {
            return "restricted";
        }

        // Account has currently due requirements
        if (stripeAccount.Requirements?.CurrentlyDue?.Count > 0)
        {
            return "incomplete";
        }

        // Account is being set up
        return "pending";
    }

    /// <summary>
    /// Validates ISO 3166-1 alpha-2 country codes
    /// </summary>
    private bool IsValidCountryCode(string countryCode)
    {
        // Common country codes supported by Stripe Connect
        var validCountries = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "US", "GB", "CA", "AU", "DE", "FR", "IT", "ES", "NL", "SE", "NO", "DK", "FI",
            "AT", "BE", "IE", "CH", "PT", "PL", "CZ", "HU", "RO", "BG", "GR", "HR", "SI",
            "SK", "LU", "MT", "CY", "LV", "LT", "EE", "JP", "SG", "HK", "NZ", "MX", "BR"
        };

        return validCountries.Contains(countryCode);
    }

    /// <summary>
    /// Maps InstructorConnectAccount entity to DTO
    /// </summary>
    private InstructorConnectAccountDto MapToDto(InstructorConnectAccount account)
    {
        return new InstructorConnectAccountDto
        {
            Id = account.Id,
            InstructorId = account.InstructorId,
            StripeAccountId = account.StripeAccountId,
            Status = account.OnboardingStatus,
            ChargesEnabled = account.ChargesEnabled,
            PayoutsEnabled = account.PayoutsEnabled,
            Country = account.Country ?? _defaultCountry,
            Currency = account.Currency ?? _defaultCurrency,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }

    #endregion
}
