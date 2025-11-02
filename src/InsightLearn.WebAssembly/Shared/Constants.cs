namespace InsightLearn.WebAssembly.Shared;

/// <summary>
/// Application role constants
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Instructor = "Instructor";
    public const string Student = "Student";
    public const string EnterpriseAdmin = "EnterpriseAdmin";
}

/// <summary>
/// Custom claim type constants
/// </summary>
public static class ClaimTypes
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string Role = "role";
    public const string Name = "name";
    public const string ProfilePictureUrl = "picture";
    public const string TenantId = "tenant_id";
    public const string IsEmailVerified = "email_verified";
}

/// <summary>
/// Route constants for navigation
/// </summary>
public static class Routes
{
    // Public routes
    public const string Home = "/";
    public const string Login = "/login";
    public const string Register = "/register";
    public const string RegisterComprehensive = "/register-comprehensive";
    public const string Privacy = "/privacy";
    public const string Terms = "/terms";
    public const string CookiePolicy = "/cookie-policy";
    public const string Help = "/help";

    // Dashboard routes
    public const string Dashboard = "/dashboard";
    public const string StudentDashboard = "/dashboard/student";
    public const string InstructorDashboard = "/dashboard/instructor";

    // Course routes
    public const string Courses = "/courses";
    public const string CourseDetails = "/courses/{0}";
    public const string Categories = "/categories";
    public const string CategoryDetails = "/categories/{0}";

    // Admin routes
    public const string AdminDashboard = "/admin";
    public const string AdminUsers = "/admin/users";
    public const string AdminCourses = "/admin/courses";
    public const string AdminCategories = "/admin/categories";
    public const string AdminAnalytics = "/admin/analytics";
    public const string AdminReports = "/admin/reports";
    public const string AdminSettings = "/admin/settings";
    public const string AdminHealth = "/admin/health";

    // Auth routes
    public const string CompleteRegistration = "/complete-registration";
    public const string OAuthCallback = "/auth/oauth-callback";
    public const string SignupComplete = "/signup-complete";
}

/// <summary>
/// API endpoint constants
/// </summary>
public static class ApiEndpoints
{
    // Auth endpoints
    public const string Login = "/api/auth/login";
    public const string Register = "/api/auth/register";
    public const string Logout = "/api/auth/logout";
    public const string RefreshToken = "/api/auth/refresh";
    public const string GoogleSignIn = "/api/auth/google-signin";

    // User endpoints
    public const string UserProfile = "/api/users/profile";
    public const string UserUpdate = "/api/users/update";
    public const string CompleteProfile = "/api/users/complete-profile";

    // Course endpoints
    public const string Courses = "/api/courses";
    public const string CourseById = "/api/courses/{0}";
    public const string CourseEnroll = "/api/courses/{0}/enroll";

    // Category endpoints
    public const string Categories = "/api/categories";
    public const string CategoryById = "/api/categories/{0}";

    // Dashboard endpoints
    public const string StudentDashboard = "/api/dashboard/student";
    public const string InstructorDashboard = "/api/dashboard/instructor";
    public const string AdminDashboard = "/api/dashboard/admin";
}

/// <summary>
/// Local storage key constants
/// </summary>
public static class StorageKeys
{
    public const string AuthToken = "authToken";
    public const string RefreshToken = "refreshToken";
    public const string UserInfo = "userInfo";
    public const string Theme = "theme";
    public const string Language = "language";
    public const string CookieConsent = "cookieConsent";
}

/// <summary>
/// Application configuration constants
/// </summary>
public static class AppConstants
{
    public const string AppName = "InsightLearn";
    public const string AppVersion = "1.0.0";
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int TokenExpirationMinutes = 60;
    public const int RefreshTokenExpirationDays = 30;
}

/// <summary>
/// Validation constants
/// </summary>
public static class ValidationConstants
{
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 100;
    public const int MinUsernameLength = 3;
    public const int MaxUsernameLength = 50;
    public const int MaxEmailLength = 255;
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 500;
}

/// <summary>
/// HTTP status code constants for client-side reference
/// </summary>
public static class StatusCodes
{
    public const int Ok = 200;
    public const int Created = 201;
    public const int NoContent = 204;
    public const int BadRequest = 400;
    public const int Unauthorized = 401;
    public const int Forbidden = 403;
    public const int NotFound = 404;
    public const int Conflict = 409;
    public const int UnprocessableEntity = 422;
    public const int InternalServerError = 500;
}

/// <summary>
/// Toast notification types
/// </summary>
public static class ToastTypes
{
    public const string Success = "success";
    public const string Error = "error";
    public const string Warning = "warning";
    public const string Info = "info";
}
