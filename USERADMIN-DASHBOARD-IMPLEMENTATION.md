# User Admin Service and Dashboard Public Service Implementation

## Summary

Successfully implemented comprehensive user administration and dashboard services for InsightLearn LMS platform.

**Date**: 2025-11-10
**Version**: 1.6.5-dev
**Status**: ✅ COMPLETE

---

## Files Created/Modified

### 1. Core Interfaces

#### Created:
- `/src/InsightLearn.Core/Interfaces/IUserAdminService.cs` (13 methods)
- `/src/InsightLearn.Core/Interfaces/IDashboardPublicService.cs` (2 methods)

### 2. Service Implementations

#### Created:
- `/src/InsightLearn.Application/Services/UserAdminService.cs` (613 lines)
- `/src/InsightLearn.Application/Services/DashboardPublicService.cs` (67 lines)

#### Modified:
- `/src/InsightLearn.Application/Services/EnhancedDashboardService.cs` (added missing using statement)

### 3. DTOs Enhanced

#### Modified (Enhanced existing DTOs):
- `/src/InsightLearn.Core/DTOs/User/UserDto.cs` - Added IsVerified, EmailConfirmed, IsLocked, LockoutEnd, AccountStatus
- `/src/InsightLearn.Core/DTOs/User/UserListDto.cs` - Added TotalPages computed property
- `/src/InsightLearn.Core/DTOs/User/UserDetailDto.cs` - Added 20+ new fields (full profile, address, OAuth, preferences)
- `/src/InsightLearn.Core/DTOs/User/UpdateUserDto.cs` - Added Bio, IsVerified, address fields, profile fields
- `/src/InsightLearn.Core/DTOs/User/UserStatisticsDto.cs` - Added instructor stats, formatted time, calculated properties

---

## UserAdminService Methods

### User CRUD Operations

| Method | Purpose | Business Rules |
|--------|---------|----------------|
| `GetAllUsersAsync()` | Paginated user list | Returns UserDto with roles |
| `GetUserByIdAsync()` | Get single user details | Returns full UserDetailDto with statistics |
| `UpdateUserAsync()` | Update user profile | Validates all fields, updates UpdatedAt timestamp |
| `DeleteUserAsync()` | Delete user account | ⚠️ Prevents deletion if active enrollments or published courses exist |
| `SuspendUserAsync()` | Suspend user account | Sets LockoutEnd to 100 years (permanent suspension) |
| `ActivateUserAsync()` | Reactivate suspended user | Clears LockoutEnd |

### Role Management

| Method | Purpose | Business Rules |
|--------|---------|----------------|
| `AssignRoleAsync()` | Assign role to user | Valid roles: Administrator, Instructor, Student, Moderator |
| `RemoveRoleAsync()` | Remove role from user | ⚠️ Prevents removing last Administrator |
| `GetUserRolesAsync()` | Get user's roles | Returns list of role names |

**Role-Instructor Synchronization**: Assigning "Instructor" role automatically sets `User.IsInstructor = true`

### Statistics

| Method | Purpose | Includes |
|--------|---------|----------|
| `GetUserStatisticsAsync()` | Comprehensive user stats | Enrollments, courses completed, learning time, spending, reviews, instructor metrics |

**Instructor Statistics** (if user.IsInstructor):
- Courses created
- Total students
- Total earnings (70% revenue share)
- Average instructor rating

### Search

| Method | Purpose | Searches |
|--------|---------|----------|
| `SearchUsersAsync()` | Find users | Email, FirstName, LastName, full name |

---

## DashboardPublicService Methods

### Public Statistics (Sanitized)

**Method**: `GetPublicStatsAsync()`

**Returns** (anonymous object):
```csharp
{
    TotalUsers,
    TotalInstructors,
    TotalCourses (published only),
    CoursesAvailable,
    PlatformStatus,
    Uptime,
    TotalVideos,
    LastUpdated
}
```

**Security**: Excludes revenue, payments, user details, draft courses

### Admin Statistics (Full Access)

**Method**: `GetAdminStatsAsync()`

**Returns**: `EnhancedDashboardStatsDto` (complete dashboard data)

**Includes**:
- User metrics (total, active, new)
- Instructor metrics
- Course metrics (published, draft, archived)
- Enrollment metrics
- Revenue metrics (total, daily, weekly, monthly)
- Platform health
- Storage metrics
- Real-time metrics

---

## Security Considerations Applied

### 1. User Deletion Safety

```csharp
// Check for active enrollments
var activeEnrollments = await _enrollmentRepository.GetActiveEnrollmentsAsync(id);
if (activeEnrollments.Any())
    return false; // Cannot delete

// Check for published courses (instructors)
if (user.IsInstructor)
{
    var publishedCourses = courses.Where(c => c.Status == CourseStatus.Published);
    if (publishedCourses.Any())
        return false; // Cannot delete
}
```

### 2. Administrator Protection

```csharp
// Prevent removing last admin
if (roleName == "Administrator")
{
    var admins = await _userManager.GetUsersInRoleAsync("Administrator");
    if (admins.Count <= 1)
        return false; // Cannot remove last admin
}
```

### 3. Role Validation

```csharp
private static readonly string[] ValidRoles =
    { "Administrator", "Instructor", "Student", "Moderator" };

// Validate before assignment
if (!ValidRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
    return false;
```

### 4. Dashboard Data Sanitization

```csharp
// Public API: Only aggregate, non-sensitive data
var publicStats = new
{
    TotalUsers = fullStats.TotalUsers,
    TotalCourses = fullStats.PublishedCourses, // Published only
    // NO revenue, NO payments, NO user details
};

// Admin API: Full access with EnhancedDashboardStatsDto
```

### 5. Lockout Management

```csharp
// Suspend (100 years lockout)
var lockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);

// Activate (clear lockout)
await _userManager.SetLockoutEndDateAsync(user, null);
```

---

## Dependencies Injected

### UserAdminService

```csharp
UserManager<User>                 // ASP.NET Core Identity
RoleManager<IdentityRole<Guid>>   // Role management
IEnrollmentRepository             // Enrollment data
ICourseRepository                 // Course data (instructor stats)
IPaymentRepository                // Financial statistics
IReviewRepository                 // Review statistics
ILogger<UserAdminService>         // Logging
```

### DashboardPublicService

```csharp
IEnhancedDashboardService         // Existing dashboard service (wrapped)
ILogger<DashboardPublicService>   // Logging
```

---

## Usage Examples

### Get All Users (Paginated)

```csharp
var result = await _userAdminService.GetAllUsersAsync(page: 1, pageSize: 20);
// result.Users - List<UserDto>
// result.TotalCount - int
// result.TotalPages - int (computed)
```

### Update User

```csharp
var updateDto = new UpdateUserDto
{
    FirstName = "John",
    LastName = "Doe",
    IsInstructor = true,
    Bio = "Experienced educator"
};
var updated = await _userAdminService.UpdateUserAsync(userId, updateDto);
```

### Suspend User

```csharp
var success = await _userAdminService.SuspendUserAsync(
    userId,
    reason: "Violated terms of service"
);
```

### Assign Role

```csharp
// Assign Instructor role (also sets IsInstructor flag)
var success = await _userAdminService.AssignRoleAsync(userId, "Instructor");
```

### Get User Statistics

```csharp
var stats = await _userAdminService.GetUserStatisticsAsync(userId);
// stats.TotalEnrollments
// stats.CompletedCourses
// stats.TotalSpent
// stats.FormattedTimeLearned - e.g., "12h 34m"
// stats.TotalEarnings (if instructor)
```

### Get Public Dashboard Stats

```csharp
var publicStats = await _dashboardPublicService.GetPublicStatsAsync();
// Returns anonymous object with non-sensitive metrics
```

### Get Admin Dashboard Stats

```csharp
var adminStats = await _dashboardPublicService.GetAdminStatsAsync();
// Returns full EnhancedDashboardStatsDto with all metrics
```

---

## Logging

All methods include comprehensive logging:

```csharp
_logger.LogInformation("[UserAdmin] Getting all users - Page: {Page}, PageSize: {PageSize}", page, pageSize);
_logger.LogWarning("[UserAdmin] Cannot delete user {UserId} with active enrollments", id);
_logger.LogError(ex, "[UserAdmin] Error getting user by ID: {UserId}", id);
```

Log prefix: `[UserAdmin]` for UserAdminService, `[DashboardPublic]` for DashboardPublicService

---

## API Endpoint Integration (Next Steps)

These services are ready to be exposed via API endpoints in `Program.cs`:

### User Admin Endpoints

```csharp
// GET /api/admin/users?page=1&pageSize=10
app.MapGet("/api/admin/users", async (IUserAdminService service, int page, int pageSize)
    => await service.GetAllUsersAsync(page, pageSize))
    .RequireAuthorization("AdminOnly");

// GET /api/admin/users/{id}
app.MapGet("/api/admin/users/{id:guid}", async (IUserAdminService service, Guid id)
    => await service.GetUserByIdAsync(id))
    .RequireAuthorization("AdminOnly");

// PUT /api/admin/users/{id}
app.MapPut("/api/admin/users/{id:guid}", async (IUserAdminService service, Guid id, UpdateUserDto dto)
    => await service.UpdateUserAsync(id, dto))
    .RequireAuthorization("AdminOnly");

// DELETE /api/admin/users/{id}
app.MapDelete("/api/admin/users/{id:guid}", async (IUserAdminService service, Guid id)
    => await service.DeleteUserAsync(id))
    .RequireAuthorization("AdminOnly");

// POST /api/admin/users/{id}/suspend
app.MapPost("/api/admin/users/{id:guid}/suspend", async (IUserAdminService service, Guid id, string reason)
    => await service.SuspendUserAsync(id, reason))
    .RequireAuthorization("AdminOnly");

// POST /api/admin/users/{id}/activate
app.MapPost("/api/admin/users/{id:guid}/activate", async (IUserAdminService service, Guid id)
    => await service.ActivateUserAsync(id))
    .RequireAuthorization("AdminOnly");

// POST /api/admin/users/{id}/roles/{roleName}
app.MapPost("/api/admin/users/{id:guid}/roles/{roleName}", async (IUserAdminService service, Guid id, string roleName)
    => await service.AssignRoleAsync(id, roleName))
    .RequireAuthorization("AdminOnly");

// DELETE /api/admin/users/{id}/roles/{roleName}
app.MapDelete("/api/admin/users/{id:guid}/roles/{roleName}", async (IUserAdminService service, Guid id, string roleName)
    => await service.RemoveRoleAsync(id, roleName))
    .RequireAuthorization("AdminOnly");

// GET /api/admin/users/{id}/statistics
app.MapGet("/api/admin/users/{id:guid}/statistics", async (IUserAdminService service, Guid id)
    => await service.GetUserStatisticsAsync(id))
    .RequireAuthorization("AdminOnly");

// GET /api/admin/users/search?query=john&page=1&pageSize=10
app.MapGet("/api/admin/users/search", async (IUserAdminService service, string query, int page, int pageSize)
    => await service.SearchUsersAsync(query, page, pageSize))
    .RequireAuthorization("AdminOnly");
```

### Dashboard Endpoints

```csharp
// GET /api/dashboard/public
app.MapGet("/api/dashboard/public", async (IDashboardPublicService service)
    => await service.GetPublicStatsAsync())
    .AllowAnonymous();

// GET /api/dashboard/admin
app.MapGet("/api/dashboard/admin", async (IDashboardPublicService service)
    => await service.GetAdminStatsAsync())
    .RequireAuthorization("AdminOnly");
```

---

## Build Status

✅ **Core Project**: Build successful (0 errors)
✅ **UserAdminService**: Compiles successfully (2 warnings - async/await not critical)
✅ **DashboardPublicService**: Compiles successfully (0 errors)
✅ **DTOs**: All enhanced DTOs compile successfully

⚠️ **Note**: Application project has pre-existing errors in `EnhancedPaymentService.cs` (unrelated to this implementation)

---

## Testing Checklist

### UserAdminService Tests

- [ ] GetAllUsersAsync - pagination works correctly
- [ ] GetUserByIdAsync - returns full details with statistics
- [ ] UpdateUserAsync - validates and updates all fields
- [ ] DeleteUserAsync - prevents deletion with active enrollments
- [ ] DeleteUserAsync - prevents deletion with published courses
- [ ] SuspendUserAsync - sets lockout correctly
- [ ] ActivateUserAsync - clears lockout
- [ ] AssignRoleAsync - validates role name
- [ ] AssignRoleAsync - sets IsInstructor flag for Instructor role
- [ ] RemoveRoleAsync - prevents removing last Administrator
- [ ] GetUserRolesAsync - returns correct roles
- [ ] GetUserStatisticsAsync - calculates student stats correctly
- [ ] GetUserStatisticsAsync - calculates instructor stats correctly
- [ ] SearchUsersAsync - searches by email, name

### DashboardPublicService Tests

- [ ] GetPublicStatsAsync - excludes sensitive data (revenue, payments)
- [ ] GetPublicStatsAsync - includes only published courses
- [ ] GetAdminStatsAsync - returns full EnhancedDashboardStatsDto

---

## Performance Considerations

### Optimizations Applied

1. **Lazy Loading Statistics**: `UserDetailDto.Statistics` populated separately (avoid N+1 queries)
2. **Pagination**: All list methods support pagination
3. **Eager Role Loading**: Roles loaded with `GetRolesAsync()` (single query per user)
4. **Dashboard Caching**: EnhancedDashboardService already has 5-minute cache

### Potential Bottlenecks

1. **Instructor Statistics**: Multiple repository calls in loop (for each course)
   - **Mitigation**: Consider batch loading or denormalization for large instructors
2. **Search Query**: Uses `ToLower()` which prevents index usage
   - **Mitigation**: Consider full-text search or pre-computed search tokens

---

## Conclusion

Successfully implemented comprehensive user administration and dashboard services with:

✅ 13 user admin methods
✅ 2 dashboard public methods
✅ Complete CRUD operations
✅ Role management with safety checks
✅ User statistics (student + instructor)
✅ Search functionality
✅ Security considerations (deletion protection, role validation, data sanitization)
✅ Comprehensive logging
✅ DTOs enhanced with backward compatibility

**Ready for API endpoint integration in Program.cs**
