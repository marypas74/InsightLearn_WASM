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

            // Map JWT standard claims to .NET ClaimTypes for proper role detection
            // JWT uses "role" but .NET expects ClaimTypes.Role for IsInRole() to work
            var mappedClaims = new List<Claim>();
            foreach (var claim in claims)
            {
                // Map role claim to ClaimTypes.Role
                if (claim.Type == "role" || claim.Type == "roles")
                {
                    mappedClaims.Add(new Claim(ClaimTypes.Role, claim.Value));
                    _logger.LogDebug("🔑 Mapped role claim: {Role}", claim.Value);
                }
                // Map name claim to ClaimTypes.Name
                else if (claim.Type == "name" || claim.Type == "unique_name")
                {
                    mappedClaims.Add(new Claim(ClaimTypes.Name, claim.Value));
                }
                // Map email claim to ClaimTypes.Email
                else if (claim.Type == "email")
                {
                    mappedClaims.Add(new Claim(ClaimTypes.Email, claim.Value));
                }
                // Keep original claim as well
                mappedClaims.Add(claim);
            }

            var identity = new ClaimsIdentity(mappedClaims, "jwt");
            var user = new ClaimsPrincipal(identity);

            var email = mappedClaims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email")?.Value ?? "unknown";
            var role = mappedClaims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "Student";
            _logger.LogInformation("✅ User authenticated: {Email} with role: {Role}", email, role);

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
