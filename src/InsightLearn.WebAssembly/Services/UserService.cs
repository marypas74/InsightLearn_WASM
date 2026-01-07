using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Models.Auth;
using InsightLearn.WebAssembly.Models.Config;
using InsightLearn.WebAssembly.Services.Http;
using Microsoft.Extensions.Logging;

namespace InsightLearn.WebAssembly.Services;

public class UserService : IUserService
{
    private readonly IApiClient _apiClient;
    private readonly EndpointsConfig _endpoints;
    private readonly ILogger<UserService> _logger;

    public UserService(IApiClient apiClient, EndpointsConfig endpoints, ILogger<UserService> logger)
    {
        _apiClient = apiClient;
        _endpoints = endpoints;
        _logger = logger;
    }

    public async Task<ApiResponse<UserInfo>> GetUserProfileAsync()
    {
        _logger.LogDebug("Fetching user profile");
        var response = await _apiClient.GetAsync<UserInfo>(_endpoints.Users.GetProfile);

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("User profile retrieved: {Email}", response.Data.Email);
        }
        else
        {
            _logger.LogWarning("Failed to retrieve user profile: {ErrorMessage}",
                response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<UserInfo>> UpdateUserProfileAsync(UserInfo profile)
    {
        _logger.LogInformation("Updating user profile: {Email}", profile.Email);
        var response = await _apiClient.PutAsync<UserInfo>(_endpoints.Users.GetProfile, profile);

        if (response.Success)
        {
            _logger.LogInformation("User profile updated successfully: {Email}", profile.Email);
        }
        else
        {
            _logger.LogError("Failed to update user profile {Email}: {ErrorMessage}",
                profile.Email, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        // SECURITY: Never log password values
        _logger.LogInformation("User password change requested");
        var response = await _apiClient.PostAsync("users/change-password", new
        {
            CurrentPassword = currentPassword,
            NewPassword = newPassword
        });

        if (response.Success)
        {
            _logger.LogInformation("User password changed successfully");
        }
        else
        {
            _logger.LogWarning("Password change failed: {ErrorMessage}",
                response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse> UploadProfileImageAsync(Stream imageStream, string filename)
    {
        _logger.LogInformation("Uploading profile image: {Filename}", filename);

        // Implement multipart form data upload
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(imageStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent, "file", filename);

        var response = await _apiClient.PostAsync("users/profile-image", content);

        if (response.Success)
        {
            _logger.LogInformation("Profile image uploaded successfully: {Filename}", filename);
        }
        else
        {
            _logger.LogError("Failed to upload profile image {Filename}: {ErrorMessage}",
                filename, response.Message ?? "Unknown error");
        }

        return response;
    }
}
