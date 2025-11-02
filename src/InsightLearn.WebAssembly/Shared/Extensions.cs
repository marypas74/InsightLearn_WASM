using System.Security.Claims;
using System.Text.Json;

namespace InsightLearn.WebAssembly.Shared;

/// <summary>
/// Extension methods for string manipulation
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Truncates a string to a specified length and adds ellipsis if needed
    /// </summary>
    public static string Truncate(this string value, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength - suffix.Length) + suffix;
    }

    /// <summary>
    /// Converts a string to title case
    /// </summary>
    public static string ToTitleCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(value.ToLower());
    }

    /// <summary>
    /// Converts a string to URL-friendly slug
    /// </summary>
    public static string ToSlug(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        value = value.ToLowerInvariant();
        value = System.Text.RegularExpressions.Regex.Replace(value, @"[^a-z0-9\s-]", "");
        value = System.Text.RegularExpressions.Regex.Replace(value, @"\s+", " ").Trim();
        value = value.Replace(" ", "-");
        value = System.Text.RegularExpressions.Regex.Replace(value, @"-+", "-");

        return value;
    }

    /// <summary>
    /// Checks if string is a valid email
    /// </summary>
    public static bool IsValidEmail(this string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Capitalizes the first letter of a string
    /// </summary>
    public static string CapitalizeFirst(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return char.ToUpper(value[0]) + value.Substring(1);
    }
}

/// <summary>
/// Extension methods for DateTime
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Converts DateTime to relative time string (e.g., "2 hours ago")
    /// </summary>
    public static string ToRelativeTime(this DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan.TotalSeconds < 60)
            return "just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes != 1 ? "s" : "")} ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours != 1 ? "s" : "")} ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays != 1 ? "s" : "")} ago";
        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)} week{((int)(timeSpan.TotalDays / 7) != 1 ? "s" : "")} ago";
        if (timeSpan.TotalDays < 365)
            return $"{(int)(timeSpan.TotalDays / 30)} month{((int)(timeSpan.TotalDays / 30) != 1 ? "s" : "")} ago";

        return $"{(int)(timeSpan.TotalDays / 365)} year{((int)(timeSpan.TotalDays / 365) != 1 ? "s" : "")} ago";
    }

    /// <summary>
    /// Formats DateTime to friendly date string
    /// </summary>
    public static string ToFriendlyDate(this DateTime dateTime)
    {
        return dateTime.ToString("MMMM dd, yyyy");
    }

    /// <summary>
    /// Formats DateTime to friendly date and time string
    /// </summary>
    public static string ToFriendlyDateTime(this DateTime dateTime)
    {
        return dateTime.ToString("MMMM dd, yyyy 'at' h:mm tt");
    }

    /// <summary>
    /// Checks if date is today
    /// </summary>
    public static bool IsToday(this DateTime dateTime)
    {
        return dateTime.Date == DateTime.Now.Date;
    }

    /// <summary>
    /// Checks if date is in the future
    /// </summary>
    public static bool IsFuture(this DateTime dateTime)
    {
        return dateTime > DateTime.Now;
    }
}

/// <summary>
/// Extension methods for decimal (for price formatting)
/// </summary>
public static class DecimalExtensions
{
    /// <summary>
    /// Formats decimal as currency
    /// </summary>
    public static string ToCurrency(this decimal value, string currencySymbol = "$")
    {
        if (value == 0)
            return "Free";

        return $"{currencySymbol}{value:N2}";
    }

    /// <summary>
    /// Formats decimal as percentage
    /// </summary>
    public static string ToPercentage(this decimal value, int decimals = 0)
    {
        return $"{value.ToString($"F{decimals}")}%";
    }
}

/// <summary>
/// Extension methods for numbers
/// </summary>
public static class NumberExtensions
{
    /// <summary>
    /// Formats large numbers with K, M, B suffixes
    /// </summary>
    public static string ToShortNumber(this int value)
    {
        if (value >= 1000000000)
            return $"{value / 1000000000.0:F1}B";
        if (value >= 1000000)
            return $"{value / 1000000.0:F1}M";
        if (value >= 1000)
            return $"{value / 1000.0:F1}K";

        return value.ToString();
    }

    /// <summary>
    /// Formats large numbers with K, M, B suffixes
    /// </summary>
    public static string ToShortNumber(this long value)
    {
        if (value >= 1000000000)
            return $"{value / 1000000000.0:F1}B";
        if (value >= 1000000)
            return $"{value / 1000000.0:F1}M";
        if (value >= 1000)
            return $"{value / 1000.0:F1}K";

        return value.ToString();
    }
}

/// <summary>
/// Extension methods for ClaimsPrincipal
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets user ID from claims
    /// </summary>
    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.UserId)?.Value
            ?? principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Gets user email from claims
    /// </summary>
    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Gets user name from claims
    /// </summary>
    public static string? GetUserName(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Name)?.Value
            ?? principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
    }

    /// <summary>
    /// Checks if user has specific role
    /// </summary>
    public static bool HasRole(this ClaimsPrincipal principal, string role)
    {
        return principal.IsInRole(role);
    }

    /// <summary>
    /// Gets all user roles
    /// </summary>
    public static List<string> GetRoles(this ClaimsPrincipal principal)
    {
        return principal.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();
    }
}

/// <summary>
/// Extension methods for IEnumerable
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Checks if collection is null or empty
    /// </summary>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? collection)
    {
        return collection == null || !collection.Any();
    }

    /// <summary>
    /// Performs an action on each element
    /// </summary>
    public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
    {
        foreach (var item in collection)
        {
            action(item);
        }
    }
}

/// <summary>
/// Extension methods for JSON serialization
/// </summary>
public static class JsonExtensions
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Serializes object to JSON string
    /// </summary>
    public static string ToJson<T>(this T obj, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(obj, options ?? DefaultOptions);
    }

    /// <summary>
    /// Deserializes JSON string to object
    /// </summary>
    public static T? FromJson<T>(this string json, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        return JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
    }
}
