using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsightLearn.Core.DTOs.User;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Services
{
    public class UserAdminService : IUserAdminService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly ILogger<UserAdminService> _logger;

        // Valid role names
        private static readonly string[] ValidRoles = { "Administrator", "Instructor", "Student", "Moderator" };

        public UserAdminService(
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IEnrollmentRepository enrollmentRepository,
            ICourseRepository courseRepository,
            IPaymentRepository paymentRepository,
            IReviewRepository reviewRepository,
            ILogger<UserAdminService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _enrollmentRepository = enrollmentRepository;
            _courseRepository = courseRepository;
            _paymentRepository = paymentRepository;
            _reviewRepository = reviewRepository;
            _logger = logger;
        }

        public async Task<UserListDto> GetAllUsersAsync(int page = 1, int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("[UserAdmin] Getting all users - Page: {Page}, PageSize: {PageSize}", page, pageSize);

                var totalCount = await _userManager.Users.CountAsync();
                var users = await _userManager.Users
                    .OrderByDescending(u => u.DateJoined)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var userDtos = new List<UserDto>();
                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userDtos.Add(await MapToUserDto(user, roles.ToList()));
                }

                return new UserListDto
                {
                    Users = userDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserAdmin] Error getting all users");
                return new UserListDto();
            }
        }

        public async Task<UserDetailDto?> GetUserByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("[UserAdmin] Getting user by ID: {UserId}", id);

                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    _logger.LogWarning("[UserAdmin] User not found: {UserId}", id);
                    return null;
                }

                return await MapToUserDetailDto(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserAdmin] Error getting user by ID: {UserId}", id);
                return null;
            }
        }

        public async Task<UserDetailDto?> UpdateUserAsync(Guid id, UpdateUserDto dto)
        {
            try
            {
                _logger.LogInformation("[UserAdmin] Updating user: {UserId}", id);

                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    _logger.LogWarning("[UserAdmin] User not found: {UserId}", id);
                    return null;
                }

                // Update basic information
                if (!string.IsNullOrEmpty(dto.FirstName))
                    user.FirstName = dto.FirstName;

                if (!string.IsNullOrEmpty(dto.LastName))
                    user.LastName = dto.LastName;

                if (dto.PhoneNumber != null)
                    user.PhoneNumber = dto.PhoneNumber;

                if (dto.Bio != null)
                    user.Bio = dto.Bio;

                // Update instructor status
                if (dto.IsInstructor.HasValue)
                    user.IsInstructor = dto.IsInstructor.Value;

                // Update verification status
                if (dto.IsVerified.HasValue)
                    user.IsVerified = dto.IsVerified.Value;

                // Update address information
                if (dto.StreetAddress != null)
                    user.StreetAddress = dto.StreetAddress;

                if (dto.City != null)
                    user.City = dto.City;

                if (dto.StateProvince != null)
                    user.StateProvince = dto.StateProvince;

                if (dto.PostalCode != null)
                    user.PostalCode = dto.PostalCode;

                if (dto.Country != null)
                    user.Country = dto.Country;

                // Update profile information
                if (dto.DateOfBirth.HasValue)
                    user.DateOfBirth = dto.DateOfBirth;

                if (dto.Gender != null)
                    user.Gender = dto.Gender;

                if (dto.UserType != null)
                    user.UserType = dto.UserType;

                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("[UserAdmin] Failed to update user {UserId}: {Errors}",
                        id, string.Join(", ", result.Errors.Select(e => e.Description)));
                    return null;
                }

                _logger.LogInformation("[UserAdmin] User updated successfully: {UserId}", id);
                return await MapToUserDetailDto(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserAdmin] Error updating user: {UserId}", id);
                return null;
            }
        }

        public async Task<bool> DeleteUserAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("[UserAdmin] Attempting to delete user: {UserId}", id);

                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    _logger.LogWarning("[UserAdmin] User not found: {UserId}", id);
                    return false;
                }

                // Check for active enrollments
                var activeEnrollments = await _enrollmentRepository.GetActiveEnrollmentsAsync(id);
                if (activeEnrollments.Any())
                {
                    _logger.LogWarning("[UserAdmin] Cannot delete user {UserId} with {Count} active enrollments",
                        id, activeEnrollments.Count());
                    return false;
                }

                // Check if user is instructor with published courses
                if (user.IsInstructor)
                {
                    var courses = await _courseRepository.GetByInstructorIdAsync(id);
                    var publishedCourses = courses.Where(c => c.Status == CourseStatus.Published).ToList();
                    if (publishedCourses.Any())
                    {
                        _logger.LogWarning("[UserAdmin] Cannot delete instructor {UserId} with {Count} published courses",
                            id, publishedCourses.Count);
                        return false;
                    }
                }

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("[UserAdmin] User deleted successfully: {UserId}", id);
                    return true;
                }

                _logger.LogWarning("[UserAdmin] Failed to delete user {UserId}: {Errors}",
                    id, string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserAdmin] Error deleting user: {UserId}", id);
                return false;
            }
        }

        public async Task<bool> SuspendUserAsync(Guid id, string reason)
        {
            try
            {
                _logger.LogInformation("[UserAdmin] Suspending user: {UserId}, Reason: {Reason}", id, reason);

                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    _logger.LogWarning("[UserAdmin] User not found: {UserId}", id);
                    return false;
                }

                // Set lockout end to 100 years from now (effectively permanent)
                var lockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
                var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);

                if (result.Succeeded)
                {
                    _logger.LogInformation("[UserAdmin] User suspended successfully: {UserId}", id);
                    return true;
                }

                _logger.LogWarning("[UserAdmin] Failed to suspend user {UserId}: {Errors}",
                    id, string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserAdmin] Error suspending user: {UserId}", id);
                return false;
            }
        }

        public async Task<bool> ActivateUserAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("[UserAdmin] Activating user: {UserId}", id);

                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    _logger.LogWarning("[UserAdmin] User not found: {UserId}", id);
                    return false;
                }

                // Clear lockout
                var result = await _userManager.SetLockoutEndDateAsync(user, null);

                if (result.Succeeded)
                {
                    _logger.LogInformation("[UserAdmin] User activated successfully: {UserId}", id);
                    return true;
                }

                _logger.LogWarning("[UserAdmin] Failed to activate user {UserId}: {Errors}",
                    id, string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserAdmin] Error activating user: {UserId}", id);
                return false;
            }
        }

        public async Task<bool> AssignRoleAsync(Guid userId, string roleName)
        {
            try
            {
                _logger.LogInformation("[UserAdmin] Assigning role {RoleName} to user {UserId}", roleName, userId);

                // Validate role name
                if (!ValidRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("[UserAdmin] Invalid role name: {RoleName}. Valid roles: {ValidRoles}",
                        roleName, string.Join(", ", ValidRoles));
                    return false;
                }

                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    _logger.LogWarning("[UserAdmin] User not found: {UserId}", userId);
                    return false;
                }

                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    _logger.LogWarning("[UserAdmin] Role {RoleName} does not exist", roleName);
                    return false;
                }

                // Check if user already has this role
                if (await _userManager.IsInRoleAsync(user, roleName))
                {
                    _logger.LogInformation("[UserAdmin] User {UserId} already has role {RoleName}", userId, roleName);
                    return true;
                }

                // If assigning Instructor role, set IsInstructor flag
                if (roleName.Equals("Instructor", StringComparison.OrdinalIgnoreCase))
                {
                    user.IsInstructor = true;
                    await _userManager.UpdateAsync(user);
                }

                var result = await _userManager.AddToRoleAsync(user, roleName);
                if (result.Succeeded)
                {
                    _logger.LogInformation("[UserAdmin] Assigned role {RoleName} to user {UserId}", roleName, userId);
                    return true;
                }

                _logger.LogWarning("[UserAdmin] Failed to assign role {RoleName} to user {UserId}: {Errors}",
                    roleName, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserAdmin] Error assigning role {RoleName} to user {UserId}", roleName, userId);
                return false;
            }
        }

        public async Task<bool> RemoveRoleAsync(Guid userId, string roleName)
        {
            try
            {
                _logger.LogInformation("[UserAdmin] Removing role {RoleName} from user {UserId}", roleName, userId);

                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    _logger.LogWarning("[UserAdmin] User not found: {UserId}", userId);
                    return false;
                }

                // Prevent removing Administrator role if this is the only admin
                if (roleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
                {
                    var admins = await _userManager.GetUsersInRoleAsync("Administrator");
                    if (admins.Count <= 1)
                    {
                        _logger.LogWarning("[UserAdmin] Cannot remove Administrator role - user {UserId} is the only admin", userId);
                        return false;
                    }
                }

                var result = await _userManager.RemoveFromRoleAsync(user, roleName);
                if (result.Succeeded)
                {
                    _logger.LogInformation("[UserAdmin] Removed role {RoleName} from user {UserId}", roleName, userId);
                    return true;
                }

                _logger.LogWarning("[UserAdmin] Failed to remove role {RoleName} from user {UserId}: {Errors}",
                    roleName, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserAdmin] Error removing role {RoleName} from user {UserId}", roleName, userId);
                return false;
            }
        }

        public async Task<List<string>> GetUserRolesAsync(Guid userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    _logger.LogWarning("[UserAdmin] User not found: {UserId}", userId);
                    return new List<string>();
                }

                var roles = await _userManager.GetRolesAsync(user);
                return roles.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserAdmin] Error getting roles for user: {UserId}", userId);
                return new List<string>();
            }
        }

        public async Task<UserStatisticsDto> GetUserStatisticsAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("[UserAdmin] Getting statistics for user: {UserId}", userId);

                var enrollments = (await _enrollmentRepository.GetByUserIdAsync(userId)).ToList();
                var payments = (await _paymentRepository.GetByUserIdAsync(userId)).ToList();
                var reviews = (await _reviewRepository.GetByUserIdAsync(userId)).ToList();

                var completed = enrollments.Count(e => e.Status == EnrollmentStatus.Completed);
                var inProgress = enrollments.Count(e => e.Status == EnrollmentStatus.Active);

                var stats = new UserStatisticsDto
                {
                    UserId = userId,
                    TotalEnrollments = enrollments.Count,
                    CompletedCourses = completed,
                    InProgressCourses = inProgress,
                    TotalCertificates = completed,
                    TotalMinutesLearned = enrollments.Sum(e => e.TotalWatchedMinutes),
                    TotalSpent = payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount),
                    AverageRatingGiven = reviews.Any() ? reviews.Average(r => r.Rating) : 0,
                    TotalReviewsWritten = reviews.Count,
                    LastActivityDate = enrollments.Any() ? enrollments.Max(e => e.LastAccessedAt) : null
                };

                // Get instructor statistics if applicable
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user != null && user.IsInstructor)
                {
                    var courses = await _courseRepository.GetByInstructorIdAsync(userId);
                    stats.CoursesCreated = courses.Count();

                    var courseIds = courses.Select(c => c.Id).ToList();
                    var studentEnrollments = new List<Enrollment>();
                    foreach (var courseId in courseIds)
                    {
                        var courseEnrollments = await _enrollmentRepository.GetByCourseIdAsync(courseId);
                        studentEnrollments.AddRange(courseEnrollments);
                    }

                    stats.TotalStudents = studentEnrollments.Select(e => e.UserId).Distinct().Count();

                    // Calculate instructor earnings (course creator gets 70% of revenue)
                    var instructorPayments = new List<Payment>();
                    foreach (var courseId in courseIds)
                    {
                        var coursePayments = await _paymentRepository.GetByCourseIdAsync(courseId);
                        instructorPayments.AddRange(coursePayments);
                    }
                    stats.TotalEarnings = instructorPayments
                        .Where(p => p.Status == PaymentStatus.Completed)
                        .Sum(p => p.Amount * 0.7m); // 70% revenue share

                    // Calculate average instructor rating
                    var courseReviews = new List<Review>();
                    foreach (var courseId in courseIds)
                    {
                        var courseReviewsList = await _reviewRepository.GetByCourseIdAsync(courseId);
                        courseReviews.AddRange(courseReviewsList);
                    }
                    stats.AverageInstructorRating = courseReviews.Any() ? courseReviews.Average(r => r.Rating) : 0;
                }

                _logger.LogInformation("[UserAdmin] Statistics generated for user: {UserId}", userId);
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserAdmin] Error getting statistics for user: {UserId}", userId);
                return new UserStatisticsDto { UserId = userId };
            }
        }

        public async Task<UserListDto> SearchUsersAsync(string query, int page = 1, int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("[UserAdmin] Searching users with query: {Query}", query);

                var lowerQuery = query.ToLower();

                var filteredUsers = _userManager.Users
                    .Where(u => u.Email.ToLower().Contains(lowerQuery) ||
                                u.FirstName.ToLower().Contains(lowerQuery) ||
                                u.LastName.ToLower().Contains(lowerQuery) ||
                                (u.FirstName + " " + u.LastName).ToLower().Contains(lowerQuery));

                var totalCount = await filteredUsers.CountAsync();
                var users = await filteredUsers
                    .OrderByDescending(u => u.DateJoined)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var userDtos = new List<UserDto>();
                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userDtos.Add(await MapToUserDto(user, roles.ToList()));
                }

                return new UserListDto
                {
                    Users = userDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserAdmin] Error searching users with query: {Query}", query);
                return new UserListDto();
            }
        }

        // Private helper methods

        private Task<UserDto> MapToUserDto(User user, List<string> roles)
        {
            var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

            var dto = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                ProfilePictureUrl = user.ProfilePictureUrl,
                IsInstructor = user.IsInstructor,
                IsVerified = user.IsVerified,
                EmailConfirmed = user.EmailConfirmed,
                DateJoined = user.DateJoined,
                LastLoginDate = user.LastLoginDate,
                IsLocked = isLocked,
                LockoutEnd = user.LockoutEnd?.UtcDateTime,
                WalletBalance = user.WalletBalance,
                Roles = roles
            };

            return Task.FromResult(dto);
        }

        private async Task<UserDetailDto> MapToUserDetailDto(User user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

            var detailDto = new UserDetailDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Bio = user.Bio,
                IsInstructor = user.IsInstructor,
                IsVerified = user.IsVerified,
                EmailConfirmed = user.EmailConfirmed,
                IsLocked = isLocked,
                LockoutEnd = user.LockoutEnd?.UtcDateTime,
                DateJoined = user.DateJoined,
                LastLoginDate = user.LastLoginDate,
                UpdatedAt = user.UpdatedAt,
                StreetAddress = user.StreetAddress,
                City = user.City,
                StateProvince = user.StateProvince,
                PostalCode = user.PostalCode,
                Country = user.Country,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                IsGoogleUser = user.IsGoogleUser,
                GoogleId = user.GoogleId,
                UserType = user.UserType,
                PreferredPaymentMethod = user.PreferredPaymentMethod,
                WalletBalance = user.WalletBalance,
                Roles = roles.ToList()
            };

            // Get statistics
            detailDto.Statistics = await GetUserStatisticsAsync(user.Id);

            return detailDto;
        }
    }
}
