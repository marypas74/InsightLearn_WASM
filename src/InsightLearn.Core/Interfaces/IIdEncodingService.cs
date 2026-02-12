namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Service for encoding/decoding GUIDs to URL-safe strings.
/// v2.3.113-dev: Implements Sqids-based URL obfuscation for security.
/// </summary>
public interface IIdEncodingService
{
    /// <summary>
    /// Encode a GUID to a URL-safe string.
    /// </summary>
    string Encode(Guid id);

    /// <summary>
    /// Decode a URL-safe string back to a GUID.
    /// Returns null if the string is invalid.
    /// </summary>
    Guid? Decode(string encodedId);

    /// <summary>
    /// Try to decode a URL-safe string to a GUID.
    /// </summary>
    bool TryDecode(string encodedId, out Guid id);

    /// <summary>
    /// Check if a string is a valid encoded ID.
    /// </summary>
    bool IsValidEncodedId(string encodedId);
}
