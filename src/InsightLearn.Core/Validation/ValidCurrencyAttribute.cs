using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Validation;

/// <summary>
/// Validates that a string is a valid ISO 4217 currency code
/// Prevents acceptance of arbitrary 3-letter codes
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class ValidCurrencyAttribute : ValidationAttribute
{
    /// <summary>
    /// List of supported ISO 4217 currency codes
    /// https://www.six-group.com/en/products-services/financial-information/data-standards.html
    /// </summary>
    private static readonly HashSet<string> ValidCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        // Major currencies
        "USD", // United States Dollar
        "EUR", // Euro
        "GBP", // British Pound Sterling
        "JPY", // Japanese Yen
        "CHF", // Swiss Franc
        "CAD", // Canadian Dollar
        "AUD", // Australian Dollar
        "NZD", // New Zealand Dollar

        // Asian currencies
        "CNY", // Chinese Yuan
        "HKD", // Hong Kong Dollar
        "SGD", // Singapore Dollar
        "KRW", // South Korean Won
        "INR", // Indian Rupee

        // Latin American currencies
        "BRL", // Brazilian Real
        "MXN", // Mexican Peso
        "ARS", // Argentine Peso

        // Nordic currencies
        "SEK", // Swedish Krona
        "NOK", // Norwegian Krone
        "DKK", // Danish Krone

        // Middle Eastern currencies
        "AED", // UAE Dirham
        "SAR", // Saudi Riyal

        // African currencies
        "ZAR", // South African Rand
        "EGP", // Egyptian Pound

        // Other European currencies
        "PLN", // Polish Zloty
        "CZK", // Czech Koruna
        "HUF", // Hungarian Forint
        "RON", // Romanian Leu
        "BGN", // Bulgarian Lev
        "HRK", // Croatian Kuna
        "RUB", // Russian Ruble
        "TRY", // Turkish Lira

        // Add more as needed for your platform
    };

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            // Let [Required] attribute handle null validation
            return ValidationResult.Success;
        }

        if (value is not string currency)
        {
            return new ValidationResult("Currency must be a string");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return new ValidationResult("Currency cannot be empty");
        }

        if (ValidCurrencies.Contains(currency))
        {
            return ValidationResult.Success;
        }

        // Provide helpful error message with supported currencies (first 10)
        var supportedSample = string.Join(", ", ValidCurrencies.Take(10));
        return new ValidationResult(
            $"Invalid currency code '{currency}'. Supported codes include: {supportedSample}... " +
            $"(Total: {ValidCurrencies.Count} currencies supported)");
    }

    /// <summary>
    /// Public API to check if a currency is valid
    /// </summary>
    public static bool IsValidCurrency(string currency)
    {
        return !string.IsNullOrWhiteSpace(currency) && ValidCurrencies.Contains(currency);
    }

    /// <summary>
    /// Get all supported currencies (for API documentation)
    /// </summary>
    public static IReadOnlyCollection<string> GetSupportedCurrencies()
    {
        return ValidCurrencies.OrderBy(c => c).ToList().AsReadOnly();
    }
}