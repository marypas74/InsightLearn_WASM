using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.Extensions.Configuration;

namespace InsightLearn.WebAssembly.Services.Auth;

public class TokenService : ITokenService
{
    private readonly ILocalStorageService _localStorage;
    private readonly IConfiguration _configuration;
    private readonly string _tokenKey;
    private readonly string _refreshTokenKey;

    public TokenService(ILocalStorageService localStorage, IConfiguration configuration)
    {
        _localStorage = localStorage;
        _configuration = configuration;
        _tokenKey = configuration["Authentication:TokenKey"] ?? "InsightLearn.AuthToken";
        _refreshTokenKey = configuration["Authentication:RefreshTokenKey"] ?? "InsightLearn.RefreshToken";
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _localStorage.GetItemAsStringAsync(_tokenKey);
    }

    public async Task SetTokenAsync(string token)
    {
        await _localStorage.SetItemAsStringAsync(_tokenKey, token);
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        return await _localStorage.GetItemAsStringAsync(_refreshTokenKey);
    }

    public async Task SetRefreshTokenAsync(string refreshToken)
    {
        await _localStorage.SetItemAsStringAsync(_refreshTokenKey, refreshToken);
    }

    public async Task ClearTokensAsync()
    {
        await _localStorage.RemoveItemAsync(_tokenKey);
        await _localStorage.RemoveItemAsync(_refreshTokenKey);
    }

    public async Task<bool> IsTokenValidAsync()
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            // Check if token is expired
            if (jsonToken.ValidTo < DateTime.UtcNow)
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ClaimsPrincipal> GetClaimsPrincipalFromTokenAsync()
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            var claims = jsonToken.Claims.ToList();
            var identity = new ClaimsIdentity(claims, "jwt");
            return new ClaimsPrincipal(identity);
        }
        catch
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }
    }
}
