using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Models.Auth;

namespace InsightLearn.WebAssembly.Services;

public interface IUserService
{
    Task<ApiResponse<UserInfo>> GetUserProfileAsync();
    Task<ApiResponse<UserInfo>> UpdateUserProfileAsync(UserInfo profile);
    Task<ApiResponse> ChangePasswordAsync(string currentPassword, string newPassword);
    Task<ApiResponse> UploadProfileImageAsync(Stream imageStream, string filename);
}
