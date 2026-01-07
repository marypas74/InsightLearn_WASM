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
            _logger.LogDebug("Attempting login for {Email} using endpoint: {Endpoint}",
                request.Email, _endpoints.Auth.Login);

            var response = await _authHttpClient.PostAsync<AuthResponse>(_endpoints.Auth.Login, request);

            if (response?.Success == true && !string.IsNullOrEmpty(response.Token))
            {
                _logger.LogDebug("Saving JWT token to localStorage (length: {TokenLength} characters)", response.Token.Length);
                await _tokenService.SetTokenAsync(response.Token);

                if (!string.IsNullOrEmpty(response.RefreshToken))
                {
                    _logger.LogDebug("Saving refresh token to localStorage");
                    await _tokenService.SetRefreshTokenAsync(response.RefreshToken);
                }

                _logger.LogDebug("Notifying authentication state changed");
                _authStateProvider.NotifyAuthenticationStateChanged();

                _logger.LogInformation("Login successful for {Email}", request.Email);
            }
            else if (response != null)
            {
                _logger.LogWarning("Login failed for {Email}: {Message}",
                    request.Email,
                    response.Errors?.FirstOrDefault() ?? response.Message ?? "Unknown error");
            }

            return response ?? new AuthResponse
            {
                Success = false,
                Message = "No response from server",
                Errors = new List<string> { "Unable to connect to the authentication server. Please try again later." }
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during login for {Email} - Status: {StatusCode}: {ErrorMessage}",
                request.Email, ex.StatusCode, ex.Message);

            return new AuthResponse
            {
                Success = false,
                Message = "Unable to connect to the authentication server",
                Errors = new List<string> { "Connection error. Please check your network and try again." }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during login for {Email}: {ErrorMessage}",
                request.Email, ex.Message);

            return new AuthResponse
            {
                Success = false,
                Message = "An unexpected error occurred",
                Errors = new List<string> { "Something went wrong. Please try again later." }
            };
        }
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            _logger.LogDebug("Attempting registration for {Email}", request.Email);

            var response = await _authHttpClient.PostAsync<AuthResponse>(_endpoints.Auth.Register, request);

            if (response?.Success == true && !string.IsNullOrEmpty(response.Token))
            {
                _logger.LogDebug("Saving tokens for newly registered user {Email}", request.Email);
                await _tokenService.SetTokenAsync(response.Token);
                if (!string.IsNullOrEmpty(response.RefreshToken))
                {
                    await _tokenService.SetRefreshTokenAsync(response.RefreshToken);
                }

                _authStateProvider.NotifyAuthenticationStateChanged();
                _logger.LogInformation("Registration successful for {Email}", request.Email);
            }
            else if (response != null)
            {
                _logger.LogWarning("Registration failed for {Email}: {Message}",
                    request.Email,
                    response.Errors?.FirstOrDefault() ?? response.Message ?? "Unknown error");
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
            _logger.LogError(ex, "Error during registration for {Email}: {ErrorMessage}",
                request.Email, ex.Message);
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
            _logger.LogDebug("Completing user registration");

            var token = await _tokenService.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Registration completion failed: Not authenticated");
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
            _logger.LogWarning("Registration completion failed - Status: {StatusCode}: {Error}",
                response.StatusCode, errorContent);

            return new AuthResponse
            {
                Success = false,
                Message = "Failed to complete registration",
                Errors = new List<string> { errorContent }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing registration: {ErrorMessage}", ex.Message);
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
            _logger.LogInformation("User logout successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    public async Task<AuthResponse> RefreshTokenAsync()
    {
        try
        {
            _logger.LogDebug("Attempting to refresh authentication token");

            var refreshToken = await _tokenService.GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("Token refresh failed: No refresh token available");
                return new AuthResponse
                {
                    Success = false,
                    Message = "No refresh token available"
                };
            }

            var response = await _authHttpClient.PostAsync<AuthResponse>(_endpoints.Auth.Refresh, new { RefreshToken = refreshToken });

            if (response?.Success == true && !string.IsNullOrEmpty(response.Token))
            {
                _logger.LogDebug("Saving new tokens after refresh");
                await _tokenService.SetTokenAsync(response.Token);
                if (!string.IsNullOrEmpty(response.RefreshToken))
                {
                    await _tokenService.SetRefreshTokenAsync(response.RefreshToken);
                }

                _authStateProvider.NotifyAuthenticationStateChanged();
                _logger.LogInformation("Token refresh successful");
            }
            else
            {
                _logger.LogWarning("Token refresh failed: {Message}",
                    response?.Message ?? "Unknown error");
            }

            return response ?? new AuthResponse { Success = false, Message = "Token refresh failed" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token: {ErrorMessage}", ex.Message);
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
            _logger.LogDebug("Fetching current user information");

            var token = await _tokenService.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Cannot fetch current user: No authentication token available");
                return null;
            }

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var userInfo = await _httpClient.GetFromJsonAsync<UserInfo>(_endpoints.Auth.Me);

            if (userInfo != null)
            {
                _logger.LogInformation("Retrieved current user information for {Email}", userInfo.Email);
            }
            else
            {
                _logger.LogWarning("Failed to retrieve current user information");
            }

            return userInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user: {ErrorMessage}", ex.Message);
            return null;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        _logger.LogDebug("Checking if user is authenticated");
        var isValid = await _tokenService.IsTokenValidAsync();
        _logger.LogDebug("User authentication status: {IsAuthenticated}", isValid);
        return isValid;
    }

    public async Task<bool> IsInRoleAsync(string role)
    {
        try
        {
            _logger.LogDebug("Checking if user has role: {Role}", role);
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var isInRole = authState.User.IsInRole(role);
            _logger.LogDebug("User role check for {Role}: {IsInRole}", role, isInRole);
            return isInRole;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user role {Role}: {ErrorMessage}", role, ex.Message);
            return false;
        }
    }

    public async Task<bool> HandleOAuthCallbackAsync(string code, string state)
    {
        try
        {
            _logger.LogDebug("Handling OAuth callback (code length: {CodeLength}, state length: {StateLength})",
                code?.Length ?? 0, state?.Length ?? 0);

            var response = await _authHttpClient.PostAsync<AuthResponse>(
                $"{_endpoints.Auth.OAuthCallback}?code={Uri.EscapeDataString(code)}&state={Uri.EscapeDataString(state)}",
                null);

            if (response?.Success == true && !string.IsNullOrEmpty(response.Token))
            {
                _logger.LogDebug("Saving OAuth tokens");
                await _tokenService.SetTokenAsync(response.Token);
                if (!string.IsNullOrEmpty(response.RefreshToken))
                {
                    await _tokenService.SetRefreshTokenAsync(response.RefreshToken);
                }

                _authStateProvider.NotifyAuthenticationStateChanged();
                _logger.LogInformation("OAuth authentication successful");
                return true;
            }

            _logger.LogWarning("OAuth callback failed: {Message}", response?.Message ?? "Unknown error");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OAuth callback error: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        try
        {
            _logger.LogDebug("Requesting password reset for {Email}", email);

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
            _logger.LogError(ex, "Failed to send password reset email: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
    {
        try
        {
            _logger.LogDebug("Resetting password for {Email}", email);

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
            _logger.LogError(ex, "Failed to reset password: {ErrorMessage}", ex.Message);
            return false;
        }
    }
}
