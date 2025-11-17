using InsightLearn.WebAssembly.Models.Auth;

namespace InsightLearn.WebAssembly.Services.Auth;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> CompleteRegistrationAsync(CompleteRegistrationRequest request);
    Task<bool> LogoutAsync();
    Task<AuthResponse> RefreshTokenAsync();
    Task<UserInfo?> GetCurrentUserAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<bool> IsInRoleAsync(string role);
    Task<bool> HandleOAuthCallbackAsync(string code, string state);

    // Password Reset (added for /forgot-password and /reset-password pages)
    Task<bool> ForgotPasswordAsync(string email);
    Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
}
