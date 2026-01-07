using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InsightLearn.WebAssembly.Services.Auth;

public class TokenService : ITokenService
{
    private readonly ILocalStorageService _localStorage;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenService> _logger;
    private readonly string _tokenKey;
    private readonly string _refreshTokenKey;

    public TokenService(
        ILocalStorageService localStorage,
        IConfiguration configuration,
        ILogger<TokenService> logger)
    {
        _localStorage = localStorage;
        _configuration = configuration;
        _logger = logger;
        _tokenKey = configuration["Authentication:TokenKey"] ?? "InsightLearn.AuthToken";
        _refreshTokenKey = configuration["Authentication:RefreshTokenKey"] ?? "InsightLearn.RefreshToken";

        _logger.LogDebug("TokenService initialized with tokenKey: {TokenKey}, refreshTokenKey: {RefreshTokenKey}",
            _tokenKey, _refreshTokenKey);
    }

    public async Task<string?> GetTokenAsync()
    {
        var token = await _localStorage.GetItemAsStringAsync(_tokenKey);
        _logger.LogDebug("Retrieved JWT token from localStorage (exists: {TokenExists})", !string.IsNullOrEmpty(token));
        return token;
    }

    public async Task SetTokenAsync(string token)
    {
        _logger.LogDebug("Storing JWT token in localStorage (length: {TokenLength} characters)", token?.Length ?? 0);
        await _localStorage.SetItemAsStringAsync(_tokenKey, token);
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        var refreshToken = await _localStorage.GetItemAsStringAsync(_refreshTokenKey);
        _logger.LogDebug("Retrieved refresh token from localStorage (exists: {RefreshTokenExists})", !string.IsNullOrEmpty(refreshToken));
        return refreshToken;
    }

    public async Task SetRefreshTokenAsync(string refreshToken)
    {
        _logger.LogDebug("Storing refresh token in localStorage (length: {TokenLength} characters)", refreshToken?.Length ?? 0);
        await _localStorage.SetItemAsStringAsync(_refreshTokenKey, refreshToken);
    }

    public async Task ClearTokensAsync()
    {
        _logger.LogDebug("Clearing authentication tokens from localStorage");
        await _localStorage.RemoveItemAsync(_tokenKey);
        await _localStorage.RemoveItemAsync(_refreshTokenKey);
    }

    public async Task<bool> IsTokenValidAsync()
    {
        _logger.LogDebug("Validating JWT token");

        var token = await GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Token validation failed: no token found in localStorage");
            return false;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            // Check if token is expired
            if (jsonToken.ValidTo < DateTime.UtcNow)
            {
                _logger.LogWarning("Token validation failed: token expired at {ExpirationTime}", jsonToken.ValidTo);
                return false;
            }

            _logger.LogDebug("Token validation successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed with exception: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    public async Task<ClaimsPrincipal> GetClaimsPrincipalFromTokenAsync()
    {
        _logger.LogDebug("Extracting claims principal from JWT token");

        var token = await GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Cannot extract claims: no token found in localStorage");
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            var claims = jsonToken.Claims.ToList();
            var identity = new ClaimsIdentity(claims, "jwt");

            _logger.LogDebug("Successfully extracted {ClaimCount} claims from token", claims.Count);
            return new ClaimsPrincipal(identity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract claims from token: {ErrorMessage}", ex.Message);
            return new ClaimsPrincipal(new ClaimsIdentity());
        }
    }
}
