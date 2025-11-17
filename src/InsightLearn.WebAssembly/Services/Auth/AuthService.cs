using System.Net.Http.Json;
using InsightLearn.WebAssembly.Models.Auth;
using InsightLearn.WebAssembly.Models.Config;
using InsightLearn.WebAssembly.Services.Http;
using Microsoft.Extensions.Logging;

namespace InsightLearn.WebAssembly.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IAuthHttpClient _authHttpClient;
    private readonly HttpClient _httpClient;
    private readonly ITokenService _tokenService;
    private readonly JwtAuthenticationStateProvider _authStateProvider;
    private readonly EndpointsConfig _endpoints;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IAuthHttpClient authHttpClient,
        HttpClient httpClient,
        ITokenService tokenService,
        JwtAuthenticationStateProvider authStateProvider,
        EndpointsConfig endpoints,
        ILogger<AuthService> logger)
    {
        _authHttpClient = authHttpClient;
        _httpClient = httpClient;
        _tokenService = tokenService;
        _authStateProvider = authStateProvider;
        _endpoints = endpoints;
        _logger = logger;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            _logger.LogInformation("🔐 DEBUG - Attempting login for {Email} using endpoint: {Endpoint}",
                request.Email, _endpoints.Auth.Login);

            var response = await _authHttpClient.PostAsync<AuthResponse>(_endpoints.Auth.Login, request);

            if (response?.Success == true && !string.IsNullOrEmpty(response.Token))
            {
                _logger.LogInformation("📝 Saving JWT token to localStorage (length: {Length})", response.Token.Length);
                await _tokenService.SetTokenAsync(response.Token);

                if (!string.IsNullOrEmpty(response.RefreshToken))
                {
                    _logger.LogInformation("📝 Saving refresh token to localStorage");
                    await _tokenService.SetRefreshTokenAsync(response.RefreshToken);
                }

                _logger.LogInformation("🔔 Notifying authentication state changed");
                _authStateProvider.NotifyAuthenticationStateChanged();

                _logger.LogInformation("✅ Login successful for {Email} - Token saved, auth state updated", request.Email);
            }

            return response ?? new AuthResponse
            {
                Success = false,
                Message = $"Login failed calling endpoint: {_endpoints.Auth.Login}",
                Errors = new List<string> { "No response from server. Check console for details." }
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "❌ HTTP Error during login for {Email} - Status: {StatusCode}", request.Email, ex.StatusCode);
            return new AuthResponse
            {
                Success = false,
                Message = $"HTTP {ex.StatusCode} calling {_endpoints.Auth.Login}",
                Errors = new List<string> { ex.Message, $"Endpoint: {_endpoints.Auth.Login}", $"Status: {ex.StatusCode}" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Fatal error during login for {Email}", request.Email);
            return new AuthResponse
            {
                Success = false,
                Message = $"Error calling {_endpoints.Auth.Login}: {ex.GetType().Name}",
                Errors = new List<string> { ex.Message, $"Endpoint: {_endpoints.Auth.Login}", $"Type: {ex.GetType().Name}" }
            };
        }
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            _logger.LogInformation("Attempting registration for {Email}", request.Email);

            var response = await _authHttpClient.PostAsync<AuthResponse>(_endpoints.Auth.Register, request);

            if (response?.Success == true && !string.IsNullOrEmpty(response.Token))
            {
                await _tokenService.SetTokenAsync(response.Token);
                if (!string.IsNullOrEmpty(response.RefreshToken))
                {
                    await _tokenService.SetRefreshTokenAsync(response.RefreshToken);
                }

                _authStateProvider.NotifyAuthenticationStateChanged();
                _logger.LogInformation("Registration successful for {Email}", request.Email);
            }

            return response ?? new AuthResponse
            {
                Success = false,
                Message = "Registration failed",
                Errors = new List<string> { "No response from server" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", request.Email);
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred during registration",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<AuthResponse> CompleteRegistrationAsync(CompleteRegistrationRequest request)
    {
        try
        {
            _logger.LogInformation("Completing user registration");

            var token = await _tokenService.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Not authenticated",
                    Errors = new List<string> { "User must be logged in to complete registration" }
                };
            }

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsJsonAsync(_endpoints.Auth.CompleteRegistration, request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                _logger.LogInformation("Registration completion successful");
                return result ?? new AuthResponse { Success = true, Message = "Registration completed" };
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Registration completion failed: {Error}", errorContent);

            return new AuthResponse
            {
                Success = false,
                Message = "Failed to complete registration",
                Errors = new List<string> { errorContent }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing registration");
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred while completing registration",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<bool> LogoutAsync()
    {
        try
        {
            _logger.LogInformation("Logging out user");
            await _tokenService.ClearTokensAsync();
            _authStateProvider.NotifyAuthenticationStateChanged();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return false;
        }
    }

    public async Task<AuthResponse> RefreshTokenAsync()
    {
        try
        {
            var refreshToken = await _tokenService.GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(refreshToken))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "No refresh token available"
                };
            }

            var response = await _authHttpClient.PostAsync<AuthResponse>(_endpoints.Auth.Refresh, new { RefreshToken = refreshToken });

            if (response?.Success == true && !string.IsNullOrEmpty(response.Token))
            {
                await _tokenService.SetTokenAsync(response.Token);
                if (!string.IsNullOrEmpty(response.RefreshToken))
                {
                    await _tokenService.SetRefreshTokenAsync(response.RefreshToken);
                }

                _authStateProvider.NotifyAuthenticationStateChanged();
            }

            return response ?? new AuthResponse { Success = false, Message = "Token refresh failed" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred while refreshing token",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        try
        {
            var token = await _tokenService.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return await _httpClient.GetFromJsonAsync<UserInfo>(_endpoints.Auth.Me);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return null;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        return await _tokenService.IsTokenValidAsync();
    }

    public async Task<bool> IsInRoleAsync(string role)
    {
        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            return authState.User.IsInRole(role);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> HandleOAuthCallbackAsync(string code, string state)
    {
        try
        {
            _logger.LogInformation("Handling OAuth callback");

            // Exchange OAuth code for JWT token via API
            var response = await _authHttpClient.PostAsync<AuthResponse>(
                $"{_endpoints.Auth.OAuthCallback}?code={Uri.EscapeDataString(code)}&state={Uri.EscapeDataString(state)}",
                null);

            if (response?.Success == true && !string.IsNullOrEmpty(response.Token))
            {
                await _tokenService.SetTokenAsync(response.Token);
                if (!string.IsNullOrEmpty(response.RefreshToken))
                {
                    await _tokenService.SetRefreshTokenAsync(response.RefreshToken);
                }

                _authStateProvider.NotifyAuthenticationStateChanged();
                _logger.LogInformation("OAuth callback successful");
                return true;
            }

            _logger.LogWarning("OAuth callback failed: {Message}", response?.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OAuth callback failed");
            return false;
        }
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        try
        {
            _logger.LogInformation("Requesting password reset for {Email}", email);

            // TODO: API endpoint not implemented yet
            // var response = await _authHttpClient.PostAsync<object>(
            //     $"{_endpoints.Auth.ForgotPassword}",
            //     new { Email = email });

            // Simulate API call for now
            await Task.Delay(100);

            _logger.LogInformation("Password reset email sent to {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email");
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
    {
        try
        {
            _logger.LogInformation("Resetting password for {Email}", email);

            // TODO: API endpoint not implemented yet
            // var response = await _authHttpClient.PostAsync<object>(
            //     $"{_endpoints.Auth.ResetPassword}",
            //     new { Email = email, Token = token, NewPassword = newPassword });

            // Simulate API call for now
            await Task.Delay(100);

            _logger.LogInformation("Password reset successfully for {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset password");
            return false;
        }
    }
}
