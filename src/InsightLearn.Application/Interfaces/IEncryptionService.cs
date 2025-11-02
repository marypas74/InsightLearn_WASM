namespace InsightLearn.Application.Interfaces;

/// <summary>
/// Service for secure encryption and decryption of sensitive payment data
/// Uses AES-256-GCM encryption with proper key management for PCI DSS compliance
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts sensitive data using AES-256-GCM encryption
    /// </summary>
    /// <param name="plainText">The data to encrypt</param>
    /// <param name="keyId">Optional key ID for key rotation support</param>
    /// <returns>Encrypted data as base64 string</returns>
    Task<string> EncryptAsync(string plainText, string? keyId = null);

    /// <summary>
    /// Decrypts data that was encrypted with AES-256-GCM
    /// </summary>
    /// <param name="encryptedData">The encrypted data as base64 string</param>
    /// <param name="keyId">Optional key ID for key rotation support</param>
    /// <returns>Decrypted plain text</returns>
    Task<string> DecryptAsync(string encryptedData, string? keyId = null);

    /// <summary>
    /// Encrypts credit card number with additional security measures
    /// </summary>
    /// <param name="cardNumber">Credit card number to encrypt</param>
    /// <param name="keyId">Optional key ID for key rotation</param>
    /// <returns>Encrypted card number</returns>
    Task<string> EncryptCreditCardAsync(string cardNumber, string? keyId = null);

    /// <summary>
    /// Decrypts credit card number
    /// </summary>
    /// <param name="encryptedCardNumber">Encrypted card number</param>
    /// <param name="keyId">Optional key ID for key rotation</param>
    /// <returns>Decrypted card number</returns>
    Task<string> DecryptCreditCardAsync(string encryptedCardNumber, string? keyId = null);

    /// <summary>
    /// Generates a security fingerprint for fraud detection
    /// </summary>
    /// <param name="sensitiveData">Data to generate fingerprint from</param>
    /// <returns>Security fingerprint hash</returns>
    string GenerateSecurityFingerprint(string sensitiveData);

    /// <summary>
    /// Validates if encrypted data can be decrypted (integrity check)
    /// </summary>
    /// <param name="encryptedData">Encrypted data to validate</param>
    /// <param name="keyId">Optional key ID</param>
    /// <returns>True if data integrity is valid</returns>
    Task<bool> ValidateDataIntegrityAsync(string encryptedData, string? keyId = null);

    /// <summary>
    /// Gets the current active encryption key ID
    /// </summary>
    /// <returns>Current key ID</returns>
    string GetCurrentKeyId();

    /// <summary>
    /// Rotates encryption keys (creates new key and marks old as deprecated)
    /// </summary>
    /// <returns>New key ID</returns>
    Task<string> RotateEncryptionKeyAsync();

    /// <summary>
    /// Re-encrypts data with a new key (for key rotation)
    /// </summary>
    /// <param name="encryptedData">Data encrypted with old key</param>
    /// <param name="oldKeyId">Old key ID</param>
    /// <param name="newKeyId">New key ID</param>
    /// <returns>Data encrypted with new key</returns>
    Task<string> ReEncryptWithNewKeyAsync(string encryptedData, string oldKeyId, string newKeyId);
}