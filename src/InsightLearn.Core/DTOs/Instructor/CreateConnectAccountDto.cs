using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Instructor;

/// <summary>
/// DTO for creating a Stripe Connect account for an instructor
/// Security: Prevents over-posting attacks by separating input from system-managed fields
/// </summary>
public class CreateConnectAccountDto
{
    /// <summary>
    /// Country code (ISO 3166-1 alpha-2)
    /// Examples: US, GB, CA, IT, DE, FR
    /// </summary>
    [RegularExpression("^[A-Z]{2}$", ErrorMessage = "Country must be a 2-letter ISO 3166-1 alpha-2 code (e.g., US, GB, IT)")]
    public string? Country { get; set; }

    /// <summary>
    /// Currency code (ISO 4217)
    /// Examples: USD, EUR, GBP
    /// </summary>
    [RegularExpression("^[A-Z]{3}$", ErrorMessage = "Currency must be a 3-letter ISO 4217 code (e.g., USD, EUR, GBP)")]
    public string? Currency { get; set; }
}
