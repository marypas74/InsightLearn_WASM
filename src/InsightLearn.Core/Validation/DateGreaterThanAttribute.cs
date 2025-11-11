using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Validation;

/// <summary>
/// Validates that a DateTime property is greater than another DateTime property
/// </summary>
/// <example>
/// public class EventDto
/// {
///     public DateTime StartDate { get; set; }
///
///     [DateGreaterThan(nameof(StartDate))]
///     public DateTime EndDate { get; set; }
/// }
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class DateGreaterThanAttribute : ValidationAttribute
{
    private readonly string _comparisonProperty;

    public DateGreaterThanAttribute(string comparisonProperty)
    {
        _comparisonProperty = comparisonProperty;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // Allow null (let [Required] handle null checks)
        if (value == null)
        {
            return ValidationResult.Success;
        }

        var currentValue = (DateTime)value;

        // Get comparison property via reflection
        var property = validationContext.ObjectType.GetProperty(_comparisonProperty);

        if (property == null)
        {
            return new ValidationResult($"Unknown property: {_comparisonProperty}");
        }

        var comparisonValue = (DateTime?)property.GetValue(validationContext.ObjectInstance);

        // Allow null comparison value
        if (comparisonValue == null)
        {
            return ValidationResult.Success;
        }

        // Validate: current > comparison
        if (currentValue <= comparisonValue)
        {
            return new ValidationResult(
                ErrorMessage ?? $"{validationContext.DisplayName} must be later than {_comparisonProperty}");
        }

        return ValidationResult.Success;
    }
}