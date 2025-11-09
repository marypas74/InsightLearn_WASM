using InsightLearn.WebAssembly.Models.Admin;
using InsightLearn.WebAssembly.Services.Http;

namespace InsightLearn.WebAssembly.Services.Admin;

public class UserManagementService : IUserManagementService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(IApiClient apiClient, ILogger<UserManagementService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<PagedResult<UserListItem>?> GetUsersAsync(int page, int pageSize, string? search = null)
    {
        try
        {
            _logger.LogInformation("Fetching users (page: {Page}, pageSize: {PageSize}, search: {Search})",
                page, pageSize, search ?? "none");

            var url = $"/api/admin/users?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(search))
            {
                url += $"&search={Uri.EscapeDataString(search)}";
            }

            var response = await _apiClient.GetAsync<PagedResult<UserListItem>>(url);

            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("Users fetched successfully: {Count} items", response.Data.Items.Count);
                return response.Data;
            }

            _logger.LogWarning("Failed to fetch users: {Message}", response.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users");
            return null;
        }
    }

    public async Task<UserDetail?> GetUserByIdAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Fetching user {UserId}", id);
            var response = await _apiClient.GetAsync<UserDetail>($"/api/admin/users/{id}");

            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("User fetched successfully: {Email}", response.Data.Email);
                return response.Data;
            }

            _logger.LogWarning("Failed to fetch user {UserId}: {Message}", id, response.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user {UserId}", id);
            return null;
        }
    }

    public async Task<bool> UpdateUserAsync(Guid id, UserUpdateRequest request)
    {
        try
        {
            _logger.LogInformation("Updating user {UserId}", id);
            var response = await _apiClient.PutAsync($"/api/admin/users/{id}", request);

            if (response.Success)
            {
                _logger.LogInformation("User {UserId} updated successfully", id);
                return true;
            }

            _logger.LogWarning("Failed to update user {UserId}: {Message}", id, response.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return false;
        }
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting user {UserId}", id);
            var response = await _apiClient.DeleteAsync($"/api/admin/users/{id}");

            if (response.Success)
            {
                _logger.LogInformation("User {UserId} deleted successfully", id);
                return true;
            }

            _logger.LogWarning("Failed to delete user {UserId}: {Message}", id, response.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return false;
        }
    }
}
