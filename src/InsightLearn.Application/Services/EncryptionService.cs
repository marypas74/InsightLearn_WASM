using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using InsightLearn.Application.Interfaces;

namespace InsightLearn.Application.Services;

/// <summary>
/// Production-ready encryption service implementing AES-256-GCM encryption
/// Provides secure encryption for sensitive payment data with key rotation support
/// Compliant with PCI DSS requirements for credit card data protection
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EncryptionService> _logger;
    private readonly Dictionary<string, byte[]> _encryptionKeys;
    private readonly string _currentKeyId;
    private readonly IDataProtector _dataProtector;

    // AES-256-GCM parameters
    private const int KeySize = 32; // 256 bits
    private const int NonceSize = 12; // 96 bits for GCM
    private const int TagSize = 16; // 128 bits authentication tag

    public EncryptionService(
        IDataProtectionProvider dataProtectionProvider,
        IConfiguration configuration,
        ILogger<EncryptionService> logger)
    {
        _dataProtectionProvider = dataProtectionProvider;
        _configuration = configuration;
        _logger = logger;
        _encryptionKeys = new Dictionary<string, byte[]>();

        // Create a dedicated data protector for payment data
        _dataProtector = _dataProtectionProvider.CreateProtector("PaymentData.Encryption.v1");

        // Initialize encryption keys
        _currentKeyId = InitializeEncryptionKeys();

        _logger.LogInformation("🔐 EncryptionService initialized with current key ID: {KeyId}", _currentKeyId);
    }

    public async Task<string> EncryptAsync(string plainText, string? keyId = null)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentException("Plain text cannot be null or empty", nameof(plainText));

        try
        {
            var actualKeyId = keyId ?? _currentKeyId;
            var encryptionKey = GetEncryptionKey(actualKeyId);

            // Generate random nonce
            var nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            // Convert plain text to bytes
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            // Prepare ciphertext and tag buffers
            var cipherText = new byte[plainTextBytes.Length];
            var tag = new byte[TagSize];

            // Perform AES-GCM encryption
            using var aesGcm = new AesGcm(encryptionKey, TagSize);
            aesGcm.Encrypt(nonce, plainTextBytes, cipherText, tag);

            // Create encrypted data structure
            var encryptedData = new EncryptedData
            {
                KeyId = actualKeyId,
                Nonce = Convert.ToBase64String(nonce),
                CipherText = Convert.ToBase64String(cipherText),
                Tag = Convert.ToBase64String(tag),
                Timestamp = DateTimeOffset.UtcNow,
                Algorithm = "AES-256-GCM"
            };

            // Serialize and protect the encrypted data structure
            var serializedData = JsonSerializer.Serialize(encryptedData);
            var protectedData = _dataProtector.Protect(serializedData);

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(protectedData));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔐 Error encrypting data with key ID: {KeyId}", keyId ?? _currentKeyId);
            throw new InvalidOperationException("Failed to encrypt data", ex);
        }
    }

    public async Task<string> DecryptAsync(string encryptedData, string? keyId = null)
    {
        if (string.IsNullOrEmpty(encryptedData))
            throw new ArgumentException("Encrypted data cannot be null or empty", nameof(encryptedData));

        try
        {
            // Unprotect and deserialize encrypted data structure
            var protectedDataBytes = Convert.FromBase64String(encryptedData);
            var protectedData = Encoding.UTF8.GetString(protectedDataBytes);
            var serializedData = _dataProtector.Unprotect(protectedData);
            var encryptedDataStructure = JsonSerializer.Deserialize<EncryptedData>(serializedData)
                ?? throw new InvalidOperationException("Failed to deserialize encrypted data");

            // Validate algorithm
            if (encryptedDataStructure.Algorithm != "AES-256-GCM")
                throw new InvalidOperationException($"Unsupported encryption algorithm: {encryptedDataStructure.Algorithm}");

            // Get the encryption key
            var encryptionKey = GetEncryptionKey(encryptedDataStructure.KeyId);

            // Convert base64 components back to bytes
            var nonce = Convert.FromBase64String(encryptedDataStructure.Nonce);
            var cipherText = Convert.FromBase64String(encryptedDataStructure.CipherText);
            var tag = Convert.FromBase64String(encryptedDataStructure.Tag);

            // Prepare plaintext buffer
            var plainTextBytes = new byte[cipherText.Length];

            // Perform AES-GCM decryption with authentication
            using var aesGcm = new AesGcm(encryptionKey, TagSize);
            aesGcm.Decrypt(nonce, cipherText, tag, plainTextBytes);

            return Encoding.UTF8.GetString(plainTextBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔐 Error decrypting data");
            throw new InvalidOperationException("Failed to decrypt data", ex);
        }
    }

    public async Task<string> EncryptCreditCardAsync(string cardNumber, string? keyId = null)
    {
        if (string.IsNullOrEmpty(cardNumber))
            throw new ArgumentException("Card number cannot be null or empty", nameof(cardNumber));

        // Validate and sanitize card number
        var sanitizedCardNumber = cardNumber.Replace(" ", "").Replace("-", "");

        if (!IsValidCreditCardNumber(sanitizedCardNumber))
            throw new ArgumentException("Invalid credit card number format", nameof(cardNumber));

        _logger.LogInformation("🔐 Encrypting credit card data (last 4: ****{LastFour})",
            sanitizedCardNumber.Substring(Math.Max(0, sanitizedCardNumber.Length - 4)));

        return await EncryptAsync(sanitizedCardNumber, keyId);
    }

    public async Task<string> DecryptCreditCardAsync(string encryptedCardNumber, string? keyId = null)
    {
        if (string.IsNullOrEmpty(encryptedCardNumber))
            throw new ArgumentException("Encrypted card number cannot be null or empty", nameof(encryptedCardNumber));

        _logger.LogInformation("🔐 Decrypting credit card data");

        return await DecryptAsync(encryptedCardNumber, keyId);
    }

    public string GenerateSecurityFingerprint(string sensitiveData)
    {
        if (string.IsNullOrEmpty(sensitiveData))
            throw new ArgumentException("Sensitive data cannot be null or empty", nameof(sensitiveData));

        try
        {
            using var sha256 = SHA256.Create();
            var saltBytes = Encoding.UTF8.GetBytes($"PAYMENT_FINGERPRINT_{_currentKeyId}");
            var dataBytes = Encoding.UTF8.GetBytes(sensitiveData);
            var combined = saltBytes.Concat(dataBytes).ToArray();
            var hashBytes = sha256.ComputeHash(combined);

            return Convert.ToBase64String(hashBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔐 Error generating security fingerprint");
            throw new InvalidOperationException("Failed to generate security fingerprint", ex);
        }
    }

    public async Task<bool> ValidateDataIntegrityAsync(string encryptedData, string? keyId = null)
    {
        try
        {
            await DecryptAsync(encryptedData, keyId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GetCurrentKeyId()
    {
        return _currentKeyId;
    }

    public async Task<string> RotateEncryptionKeyAsync()
    {
        try
        {
            var newKeyId = $"key_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}";
            var newKey = new byte[KeySize];
            RandomNumberGenerator.Fill(newKey);

            _encryptionKeys[newKeyId] = newKey;

            _logger.LogInformation("🔐 New encryption key generated: {KeyId}", newKeyId);

            // In a production system, you would persist this key securely
            // and update the configuration to use the new key as current

            return newKeyId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔐 Error rotating encryption key");
            throw new InvalidOperationException("Failed to rotate encryption key", ex);
        }
    }

    public async Task<string> ReEncryptWithNewKeyAsync(string encryptedData, string oldKeyId, string newKeyId)
    {
        try
        {
            // Decrypt with old key
            var plainText = await DecryptAsync(encryptedData, oldKeyId);

            // Encrypt with new key
            var reEncryptedData = await EncryptAsync(plainText, newKeyId);

            _logger.LogInformation("🔐 Data re-encrypted from key {OldKeyId} to {NewKeyId}", oldKeyId, newKeyId);

            return reEncryptedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔐 Error re-encrypting data from {OldKeyId} to {NewKeyId}", oldKeyId, newKeyId);
            throw new InvalidOperationException("Failed to re-encrypt data with new key", ex);
        }
    }

    private string InitializeEncryptionKeys()
    {
        try
        {
            // Get master key from configuration or generate new one
            var masterKeyBase64 = _configuration["Encryption:MasterKey"];

            if (string.IsNullOrEmpty(masterKeyBase64))
            {
                // Generate new master key for development
                var newMasterKey = new byte[KeySize];
                RandomNumberGenerator.Fill(newMasterKey);
                masterKeyBase64 = Convert.ToBase64String(newMasterKey);

                _logger.LogWarning("🔐 No master key found in configuration. Generated new key for development use only!");
                _logger.LogWarning("🔐 In production, ensure master key is properly configured and secured!");
            }

            var currentKeyId = _configuration["Encryption:CurrentKeyId"] ?? $"key_default_{DateTimeOffset.UtcNow:yyyyMMdd}";
            var masterKey = Convert.FromBase64String(masterKeyBase64);

            _encryptionKeys[currentKeyId] = masterKey;

            return currentKeyId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔐 Error initializing encryption keys");
            throw new InvalidOperationException("Failed to initialize encryption keys", ex);
        }
    }

    private byte[] GetEncryptionKey(string keyId)
    {
        if (!_encryptionKeys.TryGetValue(keyId, out var key))
        {
            throw new InvalidOperationException($"Encryption key not found: {keyId}");
        }

        return key;
    }

    private static bool IsValidCreditCardNumber(string cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 13 || cardNumber.Length > 19)
            return false;

        // Check if all characters are digits
        if (!cardNumber.All(char.IsDigit))
            return false;

        // Luhn algorithm validation
        return IsValidLuhn(cardNumber);
    }

    private static bool IsValidLuhn(string cardNumber)
    {
        var sum = 0;
        var alternate = false;

        for (var i = cardNumber.Length - 1; i >= 0; i--)
        {
            var n = int.Parse(cardNumber[i].ToString());

            if (alternate)
            {
                n *= 2;
                if (n > 9)
                    n = (n % 10) + 1;
            }

            sum += n;
            alternate = !alternate;
        }

        return (sum % 10) == 0;
    }

    /// <summary>
    /// Internal structure for encrypted data with metadata
    /// </summary>
    private class EncryptedData
    {
        public string KeyId { get; set; } = string.Empty;
        public string Nonce { get; set; } = string.Empty;
        public string CipherText { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public string Algorithm { get; set; } = string.Empty;
    }
}