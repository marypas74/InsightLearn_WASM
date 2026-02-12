using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sqids;

namespace InsightLearn.Application.Services;

/// <summary>
/// Sqids-based implementation of ID encoding service.
/// v2.3.113-dev: Encodes GUIDs to short, URL-safe strings for URL obfuscation.
/// v2.3.114-dev: Fixed encoding to use 4 UInt32 values (always positive when cast to long).
/// </summary>
public class SqidsEncodingService : IIdEncodingService
{
    private readonly SqidsEncoder<long> _encoder;
    private readonly ILogger<SqidsEncodingService> _logger;

    public SqidsEncodingService(IConfiguration config, ILogger<SqidsEncodingService> logger)
    {
        _logger = logger;

        // Get configuration with secure defaults
        var alphabet = config["IdEncoding:Alphabet"]
            ?? "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var minLength = config.GetValue<int>("IdEncoding:MinLength", 10);

        _encoder = new SqidsEncoder<long>(new SqidsOptions
        {
            Alphabet = alphabet,
            MinLength = minLength
        });

        _logger.LogInformation("[SqidsEncoding] Initialized with MinLength={MinLength}", minLength);
    }

    /// <inheritdoc />
    public string Encode(Guid id)
    {
        try
        {
            // Convert GUID to four 32-bit unsigned integers (always positive when cast to long)
            var bytes = id.ToByteArray();
            var a = (long)BitConverter.ToUInt32(bytes, 0);
            var b = (long)BitConverter.ToUInt32(bytes, 4);
            var c = (long)BitConverter.ToUInt32(bytes, 8);
            var d = (long)BitConverter.ToUInt32(bytes, 12);

            return _encoder.Encode(a, b, c, d);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SqidsEncoding] Failed to encode GUID {Id}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public Guid? Decode(string encodedId)
    {
        if (string.IsNullOrWhiteSpace(encodedId))
            return null;

        try
        {
            var numbers = _encoder.Decode(encodedId);
            if (numbers.Count != 4)
            {
                _logger.LogWarning("[SqidsEncoding] Invalid encoded ID format: {EncodedId} (expected 4 parts, got {Count})",
                    encodedId, numbers.Count);
                return null;
            }

            // Convert back to GUID bytes from four UInt32 values
            var bytes = new byte[16];
            BitConverter.GetBytes((uint)numbers[0]).CopyTo(bytes, 0);
            BitConverter.GetBytes((uint)numbers[1]).CopyTo(bytes, 4);
            BitConverter.GetBytes((uint)numbers[2]).CopyTo(bytes, 8);
            BitConverter.GetBytes((uint)numbers[3]).CopyTo(bytes, 12);

            return new Guid(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[SqidsEncoding] Failed to decode: {EncodedId}", encodedId);
            return null;
        }
    }

    /// <inheritdoc />
    public bool TryDecode(string encodedId, out Guid id)
    {
        var result = Decode(encodedId);
        if (result.HasValue)
        {
            id = result.Value;
            return true;
        }
        id = Guid.Empty;
        return false;
    }

    /// <inheritdoc />
    public bool IsValidEncodedId(string encodedId)
    {
        if (string.IsNullOrWhiteSpace(encodedId))
            return false;

        try
        {
            var numbers = _encoder.Decode(encodedId);
            return numbers.Count == 4;
        }
        catch
        {
            return false;
        }
    }
}
