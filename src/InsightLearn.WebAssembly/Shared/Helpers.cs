using System.Text.RegularExpressions;

namespace InsightLearn.WebAssembly.Shared;

/// <summary>
/// Helper methods for common operations
/// </summary>
public static class Helpers
{
    /// <summary>
    /// Formats a price for display
    /// </summary>
    public static string FormatPrice(decimal price, string currencySymbol = "$")
    {
        if (price == 0)
            return "Free";

        return $"{currencySymbol}{price:N2}";
    }

    /// <summary>
    /// Formats a date for display
    /// </summary>
    public static string FormatDate(DateTime date, string format = "MMM dd, yyyy")
    {
        return date.ToString(format);
    }

    /// <summary>
    /// Formats a duration in minutes to a friendly string (e.g., "2h 30m")
    /// </summary>
    public static string FormatDuration(int totalMinutes)
    {
        if (totalMinutes < 60)
            return $"{totalMinutes}m";

        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;

        if (minutes == 0)
            return $"{hours}h";

        return $"{hours}h {minutes}m";
    }

    /// <summary>
    /// Formats a file size in bytes to a friendly string (e.g., "1.5 MB")
    /// </summary>
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Gets initials from a full name (e.g., "John Doe" -> "JD")
    /// </summary>
    public static string GetInitials(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "?";

        var names = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (names.Length >= 2)
            return $"{names[0][0]}{names[^1][0]}".ToUpper();

        if (names.Length == 1)
            return names[0].Length >= 2 ? names[0].Substring(0, 2).ToUpper() : names[0][0].ToString().ToUpper();

        return "?";
    }

    /// <summary>
    /// Generates a random color for avatars
    /// </summary>
    public static string GetAvatarColor(string seed)
    {
        var colors = new[]
        {
            "#FF6B6B", "#4ECDC4", "#45B7D1", "#FFA07A",
            "#98D8C8", "#F7DC6F", "#BB8FCE", "#85C1E2",
            "#F8B88B", "#FAD390", "#6C5CE7", "#A29BFE"
        };

        var hash = seed.GetHashCode();
        var index = Math.Abs(hash) % colors.Length;

        return colors[index];
    }

    /// <summary>
    /// Validates password strength
    /// </summary>
    public static PasswordStrength GetPasswordStrength(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return PasswordStrength.VeryWeak;

        var score = 0;

        // Length
        if (password.Length >= 8) score++;
        if (password.Length >= 12) score++;
        if (password.Length >= 16) score++;

        // Contains lowercase
        if (Regex.IsMatch(password, @"[a-z]")) score++;

        // Contains uppercase
        if (Regex.IsMatch(password, @"[A-Z]")) score++;

        // Contains digit
        if (Regex.IsMatch(password, @"\d")) score++;

        // Contains special character
        if (Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>]")) score++;

        return score switch
        {
            0 or 1 or 2 => PasswordStrength.VeryWeak,
            3 or 4 => PasswordStrength.Weak,
            5 or 6 => PasswordStrength.Medium,
            7 => PasswordStrength.Strong,
            _ => PasswordStrength.VeryStrong
        };
    }

    /// <summary>
    /// Gets a friendly message for password strength
    /// </summary>
    public static string GetPasswordStrengthMessage(PasswordStrength strength)
    {
        return strength switch
        {
            PasswordStrength.VeryWeak => "Very weak - Use a stronger password",
            PasswordStrength.Weak => "Weak - Add more characters and variety",
            PasswordStrength.Medium => "Medium - Good but could be stronger",
            PasswordStrength.Strong => "Strong - Good password!",
            PasswordStrength.VeryStrong => "Very strong - Excellent password!",
            _ => ""
        };
    }

    /// <summary>
    /// Gets a CSS class for password strength indicator
    /// </summary>
    public static string GetPasswordStrengthClass(PasswordStrength strength)
    {
        return strength switch
        {
            PasswordStrength.VeryWeak => "strength-very-weak",
            PasswordStrength.Weak => "strength-weak",
            PasswordStrength.Medium => "strength-medium",
            PasswordStrength.Strong => "strength-strong",
            PasswordStrength.VeryStrong => "strength-very-strong",
            _ => ""
        };
    }

    /// <summary>
    /// Generates a slug from a string
    /// </summary>
    public static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Convert to lowercase
        text = text.ToLowerInvariant();

        // Remove invalid characters
        text = Regex.Replace(text, @"[^a-z0-9\s-]", "");

        // Replace multiple spaces with single space
        text = Regex.Replace(text, @"\s+", " ").Trim();

        // Replace spaces with hyphens
        text = text.Replace(" ", "-");

        // Remove multiple hyphens
        text = Regex.Replace(text, @"-+", "-");

        return text;
    }

    /// <summary>
    /// Truncates text to specified length with ellipsis
    /// </summary>
    public static string TruncateText(string text, int maxLength, string ellipsis = "...")
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength - ellipsis.Length) + ellipsis;
    }

    /// <summary>
    /// Calculates reading time for text (words per minute)
    /// </summary>
    public static string CalculateReadingTime(string text, int wordsPerMinute = 200)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "0 min";

        var wordCount = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var minutes = Math.Ceiling((double)wordCount / wordsPerMinute);

        return minutes == 1 ? "1 min" : $"{minutes} min";
    }

    /// <summary>
    /// Validates if a string is a valid URL
    /// </summary>
    public static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Gets a random placeholder image URL
    /// </summary>
    public static string GetPlaceholderImage(int width = 400, int height = 300, string text = "")
    {
        var displayText = string.IsNullOrWhiteSpace(text) ? $"{width}x{height}" : text;
        return $"https://via.placeholder.com/{width}x{height}?text={Uri.EscapeDataString(displayText)}";
    }

    /// <summary>
    /// Gets a Gravatar URL for an email address
    /// Note: MD5 is not supported in browser, using UI Avatars as placeholder
    /// </summary>
    public static string GetGravatarUrl(string email, int size = 80)
    {
        if (string.IsNullOrWhiteSpace(email))
            return GetPlaceholderImage(size, size, "?");

        // MD5 not supported in browser - use UI Avatars as alternative
        var displayName = email.Split('@')[0];
        return $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(displayName)}&size={size}&background=random";
    }

    /// <summary>
    /// Sanitizes HTML to prevent XSS (basic implementation)
    /// </summary>
    public static string SanitizeHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        // Remove script tags
        html = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);

        // Remove event handlers
        html = Regex.Replace(html, @"\s*on\w+\s*=\s*[""'][^""']*[""']", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"\s*on\w+\s*=\s*[^\s>]*", "", RegexOptions.IgnoreCase);

        // Remove javascript: protocol
        html = Regex.Replace(html, @"javascript:", "", RegexOptions.IgnoreCase);

        return html;
    }
}

/// <summary>
/// Password strength levels
/// </summary>
public enum PasswordStrength
{
    VeryWeak,
    Weak,
    Medium,
    Strong,
    VeryStrong
}
