using InsightLearn.Application.DTOs;

namespace InsightLearn.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResultDto> RegisterAsync(RegisterDto registerDto);
    Task<AuthResultDto> LoginAsync(LoginDto loginDto);
    Task<AuthResultDto> LogoutAsync(Guid userId, string? ipAddress = null, string? userAgent = null, bool logoutFromAllDevices = false);
    Task<AuthResultDto> LogoutFromAllDevicesAsync(Guid userId, string? ipAddress = null, string? userAgent = null);
    Task<bool> RevokeTokenAsync(Guid userId, string tokenId);
    Task<IEnumerable<UserSessionDto>> GetUserSessionsAsync(Guid userId);
    Task<UserDto?> GetCurrentUserAsync(string userId);
    Task<bool> UpdateProfileAsync(string userId, UpdateProfileDto updateDto);
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);
    Task<bool> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto);
    Task<bool> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
    Task<AuthResultDto> GoogleLoginAsync(GoogleLoginDto googleLoginDto);
    Task<AuthResultDto> RefreshTokenAsync(string userId);
    Task<AuthResultDto> GenerateJwtTokenAsync(InsightLearn.Core.Entities.User user, string? ipAddress = null, string? userAgent = null, string? correlationId = null);
}