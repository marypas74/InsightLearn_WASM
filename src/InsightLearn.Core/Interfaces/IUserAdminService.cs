using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InsightLearn.Core.DTOs.User;

namespace InsightLearn.Core.Interfaces
{
    /// <summary>
    /// Service interface for user administration operations
    /// </summary>
    public interface IUserAdminService
    {
        // User CRUD Operations

        /// <summary>
        /// Gets all users with pagination
        /// </summary>
        Task<UserListDto> GetAllUsersAsync(int page = 1, int pageSize = 10);

        /// <summary>
        /// Gets a user by their unique identifier
        /// </summary>
        Task<UserDetailDto?> GetUserByIdAsync(Guid id);

        /// <summary>
        /// Updates user information
        /// </summary>
        Task<UserDetailDto?> UpdateUserAsync(Guid id, UpdateUserDto dto);

        /// <summary>
        /// Deletes a user account (soft delete with validation)
        /// </summary>
        Task<bool> DeleteUserAsync(Guid id);

        /// <summary>
        /// Suspends a user account
        /// </summary>
        Task<bool> SuspendUserAsync(Guid id, string reason);

        /// <summary>
        /// Activates a suspended user account
        /// </summary>
        Task<bool> ActivateUserAsync(Guid id);

        // Role Management

        /// <summary>
        /// Assigns a role to a user
        /// </summary>
        Task<bool> AssignRoleAsync(Guid userId, string roleName);

        /// <summary>
        /// Removes a role from a user
        /// </summary>
        Task<bool> RemoveRoleAsync(Guid userId, string roleName);

        /// <summary>
        /// Gets all roles assigned to a user
        /// </summary>
        Task<List<string>> GetUserRolesAsync(Guid userId);

        // Statistics

        /// <summary>
        /// Gets detailed statistics for a user
        /// </summary>
        Task<UserStatisticsDto> GetUserStatisticsAsync(Guid userId);

        // Search

        /// <summary>
        /// Searches users by email, name, or other criteria
        /// </summary>
        Task<UserListDto> SearchUsersAsync(string query, int page = 1, int pageSize = 10);
    }
}
