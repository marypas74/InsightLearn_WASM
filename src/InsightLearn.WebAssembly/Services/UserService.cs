using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Models.Auth;
using InsightLearn.WebAssembly.Models.Config;
using InsightLearn.WebAssembly.Services.Http;

namespace InsightLearn.WebAssembly.Services;

public class UserService : IUserService
{
    private readonly IApiClient _apiClient;
    private readonly EndpointsConfig _endpoints;

    public UserService(IApiClient apiClient, EndpointsConfig endpoints)
    {
        _apiClient = apiClient;
        _endpoints = endpoints;
    }

    public async Task<ApiResponse<UserInfo>> GetUserProfileAsync()
    {
        return await _apiClient.GetAsync<UserInfo>(_endpoints.Users.GetProfile);
    }

    public async Task<ApiResponse<UserInfo>> UpdateUserProfileAsync(UserInfo profile)
    {
        return await _apiClient.PutAsync<UserInfo>(_endpoints.Users.GetProfile, profile);
    }

    public async Task<ApiResponse> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        return await _apiClient.PostAsync("users/change-password", new
        {
            CurrentPassword = currentPassword,
            NewPassword = newPassword
        });
    }

    public async Task<ApiResponse> UploadProfileImageAsync(Stream imageStream, string filename)
    {
        // Implement multipart form data upload
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(imageStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent, "file", filename);

        return await _apiClient.PostAsync("users/profile-image", content);
    }
}
