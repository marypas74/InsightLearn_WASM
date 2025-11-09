using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace InsightLearn.WebAssembly.Services.Auth;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ITokenService _tokenService;
    private readonly JwtSecurityTokenHandler _jwtHandler;
    private readonly ILogger<JwtAuthenticationStateProvider> _logger;

    public JwtAuthenticationStateProvider(ITokenService tokenService, ILogger<JwtAuthenticationStateProvider> logger)
    {
        _tokenService = tokenService;
        _jwtHandler = new JwtSecurityTokenHandler();
        _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _tokenService.GetTokenAsync();

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogDebug("🔍 No token found in localStorage - user is not authenticated");
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        try
        {
            _logger.LogDebug("🔍 Token found in localStorage, validating...");
            var jwtToken = _jwtHandler.ReadJwtToken(token);

            // Check if token is expired
            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                _logger.LogWarning("⚠️ Token expired at {ExpiryTime}, clearing tokens", jwtToken.ValidTo);
                await _tokenService.ClearTokensAsync();
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var claims = jwtToken.Claims.ToList();
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            var email = claims.FirstOrDefault(c => c.Type == "email" || c.Type == ClaimTypes.Email)?.Value ?? "unknown";
            _logger.LogInformation("✅ User authenticated: {Email}", email);

            return new AuthenticationState(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error parsing JWT token, clearing tokens");
            await _tokenService.ClearTokensAsync();
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public void NotifyAuthenticationStateChanged()
    {
        _logger.LogInformation("🔔 Authentication state changed - notifying subscribers");
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
