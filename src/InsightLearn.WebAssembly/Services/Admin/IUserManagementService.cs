using InsightLearn.WebAssembly.Models.Admin;

namespace InsightLearn.WebAssembly.Services.Admin;

public interface IUserManagementService
{
    Task<PagedResult<UserListItem>?> GetUsersAsync(int page, int pageSize, string? search = null);
    Task<UserDetail?> GetUserByIdAsync(Guid id);
    Task<bool> UpdateUserAsync(Guid id, UserUpdateRequest request);
    Task<bool> DeleteUserAsync(Guid id);
}
