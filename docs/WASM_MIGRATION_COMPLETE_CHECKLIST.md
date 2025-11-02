# Complete Blazor WASM Migration Checklist

**Comprehensive file-by-file migration checklist with exact commands and priorities**

## Legend
- ✅ = Completed
- ❌ = Not started
- ⚠️ = Needs update
- 🔥 = Critical priority
- ⭐ = High priority
- 📌 = Medium priority
- 🔹 = Low priority

---

## PHASE 1: CRITICAL MVP COMPONENTS (🔥 Must Do First)

### 1.1 Core Display Components

#### ❌ CourseCard.razor 🔥
**Location:** `/Components/CourseCard.razor`
**Time:** 30-45 minutes
**Command:** `git show e2282d2:src/InsightLearn.Web/Components/CourseCard.razor`
**Purpose:** Display course card with image, title, price, rating
**Props:**
- Course (CourseDto)
- ThumbnailUrl (string)
- InstructorName (string)
- Price (decimal)
- Rating (double)
**Used In:** Homepage, course listings, dashboards
**API:** None (display component)
**Notes:** Essential for any course display

---

#### ❌ CategoryGrid.razor 🔥
**Location:** `/Components/CategoryGrid.razor`
**Time:** 20-30 minutes
**Command:** `git show e2282d2:src/InsightLearn.Web/Components/CategoryGrid.razor`
**Purpose:** Display grid of course categories
**Props:**
- Categories (List<CategoryDto>)
- OnCategoryClick (EventCallback<int>)
**Used In:** Homepage
**API:** None (display component)
**Notes:** Shows category cards with icons

---

#### ❌ HeroSection.razor 🔥
**Location:** `/Components/HeroSection.razor`
**Time:** 30-45 minutes
**Command:** `git show e2282d2:src/InsightLearn.Web/Components/HeroSection.razor`
**Purpose:** Homepage hero banner with CTA buttons
**Props:** None (or dynamic content from API)
**Used In:** Homepage only
**API:** Optional: `GET /api/content/hero`
**Notes:** Main landing banner

---

### 1.2 Critical Pages

#### ❌ Index.razor (Homepage) 🔥
**Location:** `/Pages/Index.razor`
**Time:** 60-90 minutes
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/Index.razor`
**Purpose:** Main landing page
**Route:** `@page "/"`
**APIs Needed:**
- `GET /api/courses/featured` - Get featured courses
- `GET /api/categories` - Get all categories
**Dependencies:** CourseCard, CategoryGrid, HeroSection (CREATE FIRST!)
**Authorization:** None (public)
**Notes:** Critical user entry point

---

#### ❌ StudentDashboard.razor 🔥
**Location:** `/Pages/StudentDashboard.razor`
**Time:** 60-90 minutes
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/StudentDashboard.razor`
**Purpose:** Student-specific dashboard
**Route:** `@page "/student/dashboard"`
**APIs Needed:**
- `GET /api/dashboard/student` - Get student dashboard data
**Authorization:** `@attribute [Authorize(Roles = "Student,Admin")]`
**Shows:**
- Enrolled courses with progress
- Completed lessons count
- Certificates
- Recent activity
**Notes:** Already have DashboardService.GetStudentDashboard()

---

#### ❌ InstructorDashboard.razor 🔥
**Location:** `/Pages/InstructorDashboard.razor`
**Time:** 60-90 minutes
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/InstructorDashboard.razor`
**Purpose:** Instructor-specific dashboard
**Route:** `@page "/instructor/dashboard"`
**APIs Needed:**
- `GET /api/dashboard/instructor` - Get instructor dashboard data
**Authorization:** `@attribute [Authorize(Roles = "Instructor,Admin")]`
**Shows:**
- Created courses
- Total students
- Revenue
- Average rating
- Quick actions
**Notes:** Already have DashboardService.GetInstructorDashboard()

---

### 1.3 Essential Utilities

#### ❌ Constants.cs 🔥
**Location:** `/Shared/Constants.cs`
**Time:** 30 minutes
**Purpose:** Application-wide constants
**Create:** New file
**Contents:**
```csharp
namespace InsightLearn.WebAssembly.Shared
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Instructor = "Instructor";
        public const string Student = "Student";
    }

    public static class ClaimTypes
    {
        public const string UserId = "sub";
        public const string Email = "email";
        public const string Role = "role";
        public const string FirstName = "given_name";
        public const string LastName = "family_name";
    }

    public static class ApiEndpoints
    {
        public const string Auth = "/api/auth";
        public const string Users = "/api/users";
        public const string Courses = "/api/courses";
        public const string Categories = "/api/categories";
        public const string Dashboard = "/api/dashboard";
        public const string Admin = "/api/admin";
    }

    public static class Routes
    {
        public const string Home = "/";
        public const string Login = "/login";
        public const string Register = "/register";
        public const string Dashboard = "/dashboard";
        public const string StudentDashboard = "/student/dashboard";
        public const string InstructorDashboard = "/instructor/dashboard";
        public const string AdminDashboard = "/admin/dashboard";
    }
}
```

---

#### ❌ Extensions.cs 🔥
**Location:** `/Shared/Extensions.cs`
**Time:** 30 minutes
**Purpose:** Extension methods
**Create:** New file
**Contents:**
```csharp
namespace InsightLearn.WebAssembly.Shared
{
    public static class StringExtensions
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }

        public static string ToTitleCase(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLower());
        }
    }

    public static class DateTimeExtensions
    {
        public static string ToRelativeTime(this DateTime dateTime)
        {
            var span = DateTime.UtcNow - dateTime;
            if (span.Days > 365) return $"{span.Days / 365} year{(span.Days / 365 > 1 ? "s" : "")} ago";
            if (span.Days > 30) return $"{span.Days / 30} month{(span.Days / 30 > 1 ? "s" : "")} ago";
            if (span.Days > 0) return $"{span.Days} day{(span.Days > 1 ? "s" : "")} ago";
            if (span.Hours > 0) return $"{span.Hours} hour{(span.Hours > 1 ? "s" : "")} ago";
            if (span.Minutes > 0) return $"{span.Minutes} minute{(span.Minutes > 1 ? "s" : "")} ago";
            return "Just now";
        }

        public static string ToShortDateString(this DateTime dateTime)
        {
            return dateTime.ToString("MMM dd, yyyy");
        }
    }

    public static class DecimalExtensions
    {
        public static string ToCurrency(this decimal value)
        {
            return value.ToString("C");
        }
    }
}
```

---

#### ❌ Helpers.cs 🔥
**Location:** `/Shared/Helpers.cs`
**Time:** 30 minutes
**Purpose:** Utility helper functions
**Create:** New file
**Contents:**
```csharp
namespace InsightLearn.WebAssembly.Shared
{
    public static class ValidationHelpers
    {
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsValidPassword(string password)
        {
            // Min 8 chars, 1 uppercase, 1 lowercase, 1 digit, 1 special char
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8) return false;
            return System.Text.RegularExpressions.Regex.IsMatch(password,
                @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$");
        }
    }

    public static class FormatHelpers
    {
        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public static string FormatDuration(int minutes)
        {
            if (minutes < 60) return $"{minutes}m";
            var hours = minutes / 60;
            var mins = minutes % 60;
            return mins > 0 ? $"{hours}h {mins}m" : $"{hours}h";
        }

        public static string FormatNumber(int number)
        {
            if (number >= 1000000) return $"{number / 1000000.0:0.#}M";
            if (number >= 1000) return $"{number / 1000.0:0.#}K";
            return number.ToString();
        }
    }

    public static class NavigationHelpers
    {
        public static string GetDashboardRoute(string[] roles)
        {
            if (roles.Contains(Roles.Admin)) return Routes.AdminDashboard;
            if (roles.Contains(Roles.Instructor)) return Routes.InstructorDashboard;
            if (roles.Contains(Roles.Student)) return Routes.StudentDashboard;
            return Routes.Dashboard;
        }
    }
}
```

---

### 1.4 Navigation Update

#### ⚠️ NavMenu.razor 🔥 NEEDS MAJOR UPDATE
**Location:** `/Layout/NavMenu.razor`
**Time:** 60 minutes
**Command:** `git show e2282d2:src/InsightLearn.Web/Shared/NavMenu.razor`
**Purpose:** Update with ALL navigation links
**Changes Needed:**
- Add home, courses, help links
- Add authenticated section with dashboard, my courses, certificates
- Add instructor section (role-based)
- Add admin section (role-based)
- Add login/register for unauthenticated
**Notes:** See WASM_MIGRATION_STATUS.md for complete example

---

### 1.5 Info Pages

#### ❌ Help.razor ⭐
**Location:** `/Pages/Help.razor`
**Time:** 30-45 minutes
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/Help.razor`
**Purpose:** Help center with FAQ
**Route:** `@page "/help"`
**APIs:** Optional: `GET /api/help/faq`
**Authorization:** None (public)
**Features:** Multi-language support (EN, IT, ES, FR, DE)
**Notes:** Mostly static content, detect country for language

---

#### ❌ CookiePolicy.razor ⭐
**Location:** `/Pages/CookiePolicy.razor`
**Time:** 20-30 minutes
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/CookiePolicy.razor`
**Purpose:** Cookie policy information
**Route:** `@page "/cookie-policy"`
**APIs:** None
**Authorization:** None (public)
**Notes:** Static legal content

---

## PHASE 2: ADMIN PAGES (📌 Medium Priority - 20 Files)

**Common Pattern for ALL Admin Pages:**
- Location: `/Pages/Admin/`
- Authorization: `@attribute [Authorize(Roles = "Admin")]`
- Base route: `/admin/`
- Include breadcrumb navigation
- Loading, error states
- CRUD operations via API

### Admin Page List

#### ❌ 1. Admin/Dashboard.razor
**Route:** `@page "/admin/dashboard"`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/Admin/Dashboard.razor`
**APIs:** `GET /api/dashboard/admin`
**Shows:** Platform overview, stats, charts
**Time:** 60-90 minutes

---

#### ❌ 2. Admin/UserManagement.razor
**Route:** `@page "/admin/users"`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/Admin/UserManagement.razor`
**APIs:**
- `GET /api/admin/users` - List users
- `DELETE /api/admin/users/{id}` - Delete user
**Shows:** User list with search, filter, actions
**Time:** 60-90 minutes

---

#### ❌ 3. Admin/CreateUser.razor
**Route:** `@page "/admin/users/create"`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/Admin/CreateUser.razor`
**APIs:** `POST /api/admin/users`
**Shows:** User creation form
**Time:** 45-60 minutes

---

#### ❌ 4. Admin/EditUser.razor
**Route:** `@page "/admin/users/{id}/edit"`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/Admin/EditUser.razor`
**APIs:**
- `GET /api/admin/users/{id}`
- `PUT /api/admin/users/{id}`
**Shows:** User edit form
**Time:** 45-60 minutes

---

#### ❌ 5. Admin/CourseManagement.razor
**Route:** `@page "/admin/courses"`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/Admin/CourseManagement.razor`
**APIs:**
- `GET /api/admin/courses`
- `DELETE /api/admin/courses/{id}`
- `PUT /api/admin/courses/{id}/publish`
**Shows:** All courses with management actions
**Time:** 60-90 minutes

---

#### ❌ 6. Admin/CreateCourse.razor
**Route:** `@page "/admin/courses/create"`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/Admin/CreateCourse.razor`
**APIs:** `POST /api/admin/courses`
**Shows:** Course creation form
**Time:** 60-90 minutes

---

#### ❌ 7. Admin/EditCourse.razor
**Route:** `@page "/admin/courses/{id}/edit"`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/Admin/EditCourse.razor`
**APIs:**
- `GET /api/admin/courses/{id}`
- `PUT /api/admin/courses/{id}`
**Shows:** Course edit form
**Time:** 60-90 minutes

---

#### ❌ 8. Admin/CategoryManagement.razor
**Route:** `@page "/admin/categories"`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/Admin/CategoryManagement.razor`
**APIs:**
- `GET /api/admin/categories`
- `DELETE /api/admin/categories/{id}`
**Shows:** Category list with CRUD
**Time:** 45-60 minutes

---

#### ❌ 9. Admin/CreateCategory.razor
**Route:** `@page "/admin/categories/create"`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/Admin/CreateCategory.razor`
**APIs:** `POST /api/admin/categories`
**Shows:** Category creation form
**Time:** 30-45 minutes

---

#### ❌ 10. Admin/EditCategory.razor
**Route:** `@page "/admin/categories/{id}/edit"`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/Admin/EditCategory.razor`
**APIs:**
- `GET /api/admin/categories/{id}`
- `PUT /api/admin/categories/{id}`
**Shows:** Category edit form
**Time:** 30-45 minutes

---

#### ❌ 11. Admin/Analytics.razor
**Route:** `@page "/admin/analytics"`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/Admin/Analytics.razor`
**APIs:** `GET /api/admin/analytics`
**Shows:** Platform analytics, charts, reports
**Time:** 60-90 minutes

---

#### ❌ 12. Admin/Reports.razor
**Route:** `@page "/admin/reports"`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/Admin/Reports.razor`
**APIs:** `GET /api/admin/reports`
**Shows:** Generate various reports
**Time:** 45-60 minutes

---

#### ❌ 13. Admin/SystemHealth.razor
**Route:** `@page "/admin/system-health"`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/Admin/SystemHealth.razor`
**APIs:** `GET /api/admin/system-health`
**Shows:** System status, metrics, health checks
**Time:** 60-90 minutes

---

#### ❌ 14. Admin/SystemHealthSimple.razor
**Route:** `@page "/admin/system-health-simple"`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/Admin/SystemHealthSimple.razor`
**APIs:** `GET /api/admin/system-health`
**Shows:** Simplified health view
**Time:** 30-45 minutes

---

#### ❌ 15. Admin/AccessLogs.razor
**Route:** `@page "/admin/access-logs"`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/Admin/AccessLogs.razor`
**APIs:** `GET /api/admin/access-logs`
**Shows:** System access logs viewer
**Time:** 45-60 minutes

---

#### ❌ 16. Admin/UserLockoutManagement.razor
**Route:** `@page "/admin/user-lockouts"`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/Admin/UserLockoutManagement.razor`
**APIs:**
- `GET /api/admin/user-lockouts`
- `POST /api/admin/users/{id}/lock`
- `POST /api/admin/users/{id}/unlock`
**Shows:** Manage locked user accounts
**Time:** 45-60 minutes

---

#### ❌ 17. Admin/Settings.razor
**Route:** `@page "/admin/settings"`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/Admin/Settings.razor`
**APIs:**
- `GET /api/admin/settings`
- `PUT /api/admin/settings`
**Shows:** System configuration settings
**Time:** 60-90 minutes

---

#### ❌ 18. Admin/SeoManagement.razor
**Route:** `@page "/admin/seo"`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/Admin/SeoManagement.razor`
**APIs:**
- `GET /api/admin/seo`
- `PUT /api/admin/seo`
**Shows:** SEO settings, meta tags, sitemap
**Time:** 45-60 minutes

---

#### ❌ 19. Admin/ChatbotAnalytics.razor
**Route:** `@page "/admin/chatbot/analytics"`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/Admin/ChatbotAnalytics.razor`
**APIs:** `GET /api/admin/chatbot/analytics`
**Shows:** Chatbot usage statistics
**Time:** 45-60 minutes

---

#### ❌ 20. Admin/AuthDiagnostics.razor
**Route:** `@page "/admin/auth-diagnostics"`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/Admin/AuthDiagnostics.razor`
**APIs:** `GET /api/admin/auth/diagnostics`
**Shows:** Authentication system diagnostics
**Time:** 45-60 minutes

---

## PHASE 3: REMAINING COMPONENTS (📌 Medium Priority)

### Auth Components

#### ❌ GoogleSignInButton.razor
**Location:** `/Components/GoogleSignInButton.razor`
**Command:** `git show e2282d2:src/InsightLearn.Web/Components/GoogleSignInButton.razor`
**Purpose:** Google OAuth sign-in button
**Props:** RedirectUrl (string)
**Time:** 20-30 minutes
**Notes:** Already used in Login.razor

---

#### ❌ LoginForm.razor
**Location:** `/Components/LoginForm.razor`
**Command:** `git show e2282d2:src/InsightLearn.Web/Components/LoginForm.razor`
**Purpose:** Reusable login form component
**Props:** OnSubmit (EventCallback)
**Time:** 30-45 minutes
**Notes:** May be redundant with Login.razor page

---

#### ❌ AuthenticationStateHandler.razor
**Location:** `/Components/AuthenticationStateHandler.razor`
**Command:** `git show e2282d2:src/InsightLearn.Web/Components/AuthenticationStateHandler.razor`
**Purpose:** Listen to auth state changes
**Props:** None
**Time:** 30-45 minutes
**Notes:** Triggers UI updates on auth changes

---

#### ❌ AuthenticationStatus.razor
**Location:** `/Components/AuthenticationStatus.razor`
**Command:** `git show e2282d2:src/InsightLearn.Web/Components/AuthenticationStatus.razor`
**Purpose:** Display current auth status
**Props:** None
**Time:** 20-30 minutes
**Notes:** Shows user info, login/logout buttons

---

### Admin Components

#### ❌ AdminHeaderUserMenu.razor
**Location:** `/Components/AdminHeaderUserMenu.razor`
**Command:** `git show e2282d2:src/InsightLearn.Web/Components/AdminHeaderUserMenu.razor`
**Purpose:** Admin user dropdown menu
**Props:** None
**Time:** 30-45 minutes
**Notes:** User profile, settings, logout

---

#### ❌ AdminPageBase.razor
**Location:** `/Components/AdminPageBase.razor`
**Command:** `git show e2282d2:src/InsightLearn.Web/Components/AdminPageBase.razor`
**Purpose:** Base component for admin pages
**Time:** 30-45 minutes
**Notes:** May be a base class, not a component. Check implementation.

---

#### ❌ ErrorBoundary.razor
**Location:** `/Components/ErrorBoundary.razor`
**Command:** `git show e2282d2:src/InsightLearn.Web/Components/ErrorBoundary.razor`
**Purpose:** Catch and display component errors
**Props:** ChildContent
**Time:** 20-30 minutes
**Notes:** Blazor error handling

---

### Upload Components

#### ❌ VideoUploadComponent.razor
**Location:** `/Components/VideoUploadComponent.razor`
**Command:** `git show e2282d2:src/InsightLearn.Web/Components/VideoUploadComponent.razor`
**Purpose:** Upload videos with progress
**Props:** OnUploadComplete (EventCallback)
**APIs:** `POST /api/videos/upload`
**Time:** 60-90 minutes
**Notes:** File upload, progress tracking

---

### Dashboard Components

#### ❌ Dashboard/EnterpriseMetricsChart.razor
**Location:** `/Components/Dashboard/EnterpriseMetricsChart.razor`
**Command:** `git show e2282d2:src/InsightLearn.Web/Components/Dashboard/EnterpriseMetricsChart.razor`
**Purpose:** Display enterprise metrics chart
**Props:** Data (MetricsData)
**Time:** 45-60 minutes
**Notes:** May use Chart.js or similar

---

#### ❌ Dashboard/RealTimeMetricsWidget.razor
**Location:** `/Components/Dashboard/RealTimeMetricsWidget.razor`
**Command:** `git show e2282d2:src/InsightLearn.Web/Components/Dashboard/RealTimeMetricsWidget.razor`
**Purpose:** Real-time metrics widget
**Props:** None
**Time:** 45-60 minutes
**Notes:** May need polling or WebSocket

---

## PHASE 4: SHARED/LAYOUT COMPONENTS (⭐ High Priority)

### ❌ MainHeader.razor ⭐
**Location:** `/Shared/MainHeader.razor`
**Command:** `git show e2282d2:src/InsightLearn.Web/Shared/MainHeader.razor`
**Purpose:** Site-wide main header
**Time:** 45-60 minutes
**Notes:** Logo, navigation, user menu

---

### ❌ TopBar.razor ⭐
**Location:** `/Shared/TopBar.razor`
**Command:** `git show e2282d2:src/InsightLearn.Web/Shared/TopBar.razor`
**Purpose:** Top bar component
**Time:** 30-45 minutes
**Notes:** Announcements, notifications

---

### ❌ AdminNavigationHint.razor
**Location:** `/Shared/AdminNavigationHint.razor`
**Command:** `git show e2282d2:src/InsightLearn.Web/Shared/AdminNavigationHint.razor`
**Purpose:** Admin navigation helper
**Time:** 20-30 minutes
**Notes:** Breadcrumbs or hints for admin

---

### ❌ AdminNotification.razor
**Location:** `/Shared/AdminNotification.razor`
**Command:** `git show e2282d2:src/InsightLearn.Web/Shared/AdminNotification.razor`
**Purpose:** Admin notification display
**Time:** 20-30 minutes
**Notes:** Toast or alert for admin actions

---

### ❌ Custom404Page.razor ⭐
**Location:** `/Shared/Custom404Page.razor`
**Command:** `git show e2282d2:src/InsightLearn.Web/Shared/Custom404Page.razor`
**Purpose:** 404 error page
**Time:** 20-30 minutes
**Notes:** Page not found with navigation

---

### ❌ UserFriendlyErrorMessages.razor
**Location:** `/Shared/UserFriendlyErrorMessages.razor`
**Command:** `git show e2282d2:src/InsightLearn.Web/Shared/UserFriendlyErrorMessages.razor`
**Purpose:** Display user-friendly error messages
**Props:** ErrorCode (string), Message (string)
**Time:** 20-30 minutes
**Notes:** Error display component

---

## PHASE 5: TEST/DEBUG PAGES (🔹 Low Priority)

### ❌ TestAuth.razor
**Location:** `/Pages/TestAuth.razor`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/TestAuth.razor`
**Route:** `@page "/test-auth"`
**Purpose:** Test authentication functionality
**Time:** 30-45 minutes
**Notes:** Development/debugging tool

---

### ❌ TestCompleteRegistration.razor
**Location:** `/Pages/TestCompleteRegistration.razor`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/TestCompleteRegistration.razor`
**Route:** `@page "/test-complete-registration"`
**Purpose:** Test registration completion
**Time:** 30-45 minutes
**Notes:** Development/debugging tool

---

### ❌ RegisterTest.razor
**Location:** `/Pages/RegisterTest.razor`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/RegisterTest.razor`
**Route:** `@page "/register-test"`
**Purpose:** Test registration variants
**Time:** 30-45 minutes
**Notes:** Development/debugging tool

---

### ❌ RegisterComprehensive.razor
**Location:** `/Pages/RegisterComprehensive.razor`
**Command:** `git show e2282d2:src/InsightLearn.Web/Pages/RegisterComprehensive.razor`
**Route:** `@page "/register-comprehensive"`
**Purpose:** Full registration form with all fields
**Time:** 45-60 minutes
**Notes:** Extended registration testing

---

## PROGRESS TRACKING

### Overall Progress
- **Total Files:** 52+
- **Completed:** 11 (21%)
- **Remaining:** 41+ (79%)

### By Category
- **Pages:** 8/37 (22%)
- **Components:** 3/15+ (20%)
- **Shared/Layout:** 0/10 (0%)
- **Utilities:** 0/3 (0%)

### By Priority
- **🔥 Critical (MVP):** 0/11 (0%)
- **⭐ High Priority:** 0/10 (0%)
- **📌 Medium Priority:** 0/28 (0%)
- **🔹 Low Priority:** 0/4 (0%)

### Time Estimates
- **Critical MVP:** 6-9 hours (11 files)
- **High Priority:** 4-6 hours (10 files)
- **Medium Priority:** 12-18 hours (28 files)
- **Low Priority:** 2-3 hours (4 files)
- **TOTAL:** 24-36 hours

### Recommended Order
1. **Day 1 (6-9 hours):** Complete all CRITICAL MVP files (Phase 1)
2. **Day 2 (4-6 hours):** Complete all HIGH priority shared components (Phase 4)
3. **Day 3-4 (12-18 hours):** Complete MEDIUM priority admin pages (Phase 2-3)
4. **Day 5 (2-3 hours):** Complete LOW priority test pages + final review

---

## Quick Commands Reference

```bash
# Navigate to WASM project
cd /home/mpasqui/kubernetes/Insightlearn/src/InsightLearn.WebAssembly

# View old file before migration
git show e2282d2:src/InsightLearn.Web/Pages/FILENAME.razor
git show e2282d2:src/InsightLearn.Web/Components/FILENAME.razor
git show e2282d2:src/InsightLearn.Web/Shared/FILENAME.razor

# Create directories if needed
mkdir -p Pages/Admin
mkdir -p Components/Dashboard
mkdir -p Shared

# Check what's done
ls Pages/
ls Components/
ls Shared/

# Test build (when ready)
dotnet build
```

## Completion Checklist

After all migrations:
- [ ] All 37 pages migrated
- [ ] All 15+ components migrated
- [ ] All 10 shared/layout components migrated
- [ ] All 3 utility files created
- [ ] NavMenu fully updated
- [ ] All API endpoints documented
- [ ] Build succeeds with no errors
- [ ] No console errors when running
- [ ] Authorization works on all protected pages
- [ ] All forms validate and submit correctly
- [ ] All data loads from APIs
- [ ] Loading states work everywhere
- [ ] Error handling works everywhere
- [ ] Mobile responsive throughout
- [ ] Styling matches old site
- [ ] Navigation works site-wide

---

**End of Checklist - Ready to Begin Migration!**
