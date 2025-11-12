using System.ComponentModel.DataAnnotations;
using PhoneNumbers;

namespace InsightLearn.Core.Validation;

/// <summary>
/// Validates international phone numbers using Google's libphonenumber library
/// Supports E.164 format: +[country code][number]
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class ValidPhoneNumberAttribute : ValidationAttribute
{
    private static readonly PhoneNumberUtil _phoneUtil = PhoneNumberUtil.GetInstance();

    /// <summary>
    /// Default region for parsing numbers without country code
    /// </summary>
    public string DefaultRegion { get; set; } = "US";

    /// <summary>
    /// Whether to require E.164 format (with country code)
    /// </summary>
    public bool RequireInternationalFormat { get; set; } = false;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            // Let [Required] attribute handle null validation
            return ValidationResult.Success;
        }

        var phoneNumberString = value.ToString()!.Trim();

        // Check for E.164 format requirement
        if (RequireInternationalFormat && !phoneNumberString.StartsWith("+"))
        {
            return new ValidationResult(
                "Phone number must be in international format starting with '+' (E.164 format)");
        }

        try
        {
            // Parse phone number
            var phoneNumber = _phoneUtil.Parse(phoneNumberString, DefaultRegion);

            // Validate phone number
            if (!_phoneUtil.IsValidNumber(phoneNumber))
            {
                return new ValidationResult(
                    "Invalid phone number. Please use international format: +[country code][number]");
            }

            // Additional checks
            var numberType = _phoneUtil.GetNumberType(phoneNumber);
            if (numberType == PhoneNumberType.UNKNOWN)
            {
                return new ValidationResult("Phone number type could not be determined");
            }

            return ValidationResult.Success;
        }
        catch (NumberParseException ex)
        {
            return new ValidationResult(
                $"Invalid phone number format: {ex.Message}. " +
                "Please use international format: +[country code][number] (e.g., +1-202-555-0173)");
        }
        catch (Exception ex)
        {
            // Log unexpected errors but fail safely
            return new ValidationResult($"Phone number validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Public API to validate phone numbers
    /// </summary>
    public static bool IsValidPhoneNumber(string phoneNumber, string region = "US")
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        try
        {
            var phoneUtil = PhoneNumberUtil.GetInstance();
            var number = phoneUtil.Parse(phoneNumber, region);
            return phoneUtil.IsValidNumber(number);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Format phone number to E.164 format
    /// </summary>
    public static string? FormatToE164(string phoneNumber, string region = "US")
    {
        try
        {
            var phoneUtil = PhoneNumberUtil.GetInstance();
            var number = phoneUtil.Parse(phoneNumber, region);
            return phoneUtil.Format(number, PhoneNumberFormat.E164);
        }
        catch
        {
            return null;
        }
    }
}