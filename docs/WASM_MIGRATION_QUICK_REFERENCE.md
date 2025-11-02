# Blazor WASM Migration Quick Reference

**Quick lookup guide for migrating remaining pages and components**

## File Locations Reference

### Old Blazor Server Project
```
/home/mpasqui/kubernetes/Insightlearn/src/InsightLearn.Web/
├── Pages/
│   ├── Login.razor ✅ DONE
│   ├── Register.razor ✅ DONE
│   ├── Dashboard.razor ✅ DONE
│   ├── CompleteRegistration.razor ✅ DONE
│   ├── OAuthCallback.razor ✅ DONE
│   ├── SignupComplete.razor ✅ DONE
│   ├── Privacy.razor ✅ DONE
│   ├── Terms.razor ✅ DONE
│   ├── StudentDashboard.razor ❌ TODO
│   ├── InstructorDashboard.razor ❌ TODO
│   ├── Index.razor ❌ TODO
│   ├── Help.razor ❌ TODO
│   ├── CookiePolicy.razor ❌ TODO
│   ├── TestAuth.razor ❌ TODO
│   ├── TestCompleteRegistration.razor ❌ TODO
│   ├── RegisterTest.razor ❌ TODO
│   ├── RegisterComprehensive.razor ❌ TODO
│   └── Admin/
│       ├── AccessLogs.razor ❌ TODO
│       ├── Analytics.razor ❌ TODO
│       ├── AuthDiagnostics.razor ❌ TODO
│       ├── CategoryManagement.razor ❌ TODO
│       ├── ChatbotAnalytics.razor ❌ TODO
│       ├── CourseManagement.razor ❌ TODO
│       ├── CreateCategory.razor ❌ TODO
│       ├── CreateCourse.razor ❌ TODO
│       ├── CreateUser.razor ❌ TODO
│       ├── Dashboard.razor ❌ TODO
│       ├── EditCategory.razor ❌ TODO
│       ├── EditCourse.razor ❌ TODO
│       ├── EditUser.razor ❌ TODO
│       ├── Reports.razor ❌ TODO
│       ├── SeoManagement.razor ❌ TODO
│       ├── Settings.razor ❌ TODO
│       ├── SystemHealth.razor ❌ TODO
│       ├── SystemHealthSimple.razor ❌ TODO
│       ├── UserLockoutManagement.razor ❌ TODO
│       └── UserManagement.razor ❌ TODO
├── Components/
│   ├── RedirectToLogin.razor ✅ DONE
│   ├── ChatbotWidget.razor ✅ DONE
│   ├── CookieConsent.razor ✅ DONE
│   ├── CourseCard.razor ❌ TODO
│   ├── CategoryGrid.razor ❌ TODO
│   ├── HeroSection.razor ❌ TODO
│   ├── GoogleSignInButton.razor ❌ TODO
│   ├── LoginForm.razor ❌ TODO
│   ├── AdminHeaderUserMenu.razor ❌ TODO
│   ├── AdminPageBase.razor ❌ TODO
│   ├── AuthenticationStateHandler.razor ❌ TODO
│   ├── AuthenticationStatus.razor ❌ TODO
│   ├── ErrorBoundary.razor ❌ TODO
│   ├── VideoUploadComponent.razor ❌ TODO
│   └── Dashboard/
│       ├── EnterpriseMetricsChart.razor ❌ TODO
│       └── RealTimeMetricsWidget.razor ❌ TODO
└── Shared/
    ├── MainHeader.razor ❌ TODO
    ├── NavMenu.razor ⚠️ NEEDS UPDATE
    ├── TopBar.razor ❌ TODO
    ├── AdminNavigationHint.razor ❌ TODO
    ├── AdminNotification.razor ❌ TODO
    ├── Custom404Page.razor ❌ TODO
    ├── UserFriendlyErrorMessages.razor ❌ TODO
    └── MainLayout.razor ⚠️ CHECK
```

### New Blazor WASM Project
```
/home/mpasqui/kubernetes/Insightlearn/src/InsightLearn.WebAssembly/
├── Pages/
│   └── [Migrate pages here]
├── Components/
│   └── [Migrate components here]
├── Shared/
│   ├── Constants.cs ❌ CREATE
│   ├── Extensions.cs ❌ CREATE
│   └── Helpers.cs ❌ CREATE
└── Layout/
    └── NavMenu.razor ⚠️ UPDATE
```

## How to View Old Files

```bash
# View any file from old Blazor Server site (commit e2282d2)
cd /home/mpasqui/kubernetes/Insightlearn
git show e2282d2:src/InsightLearn.Web/Pages/FILENAME.razor

# Examples:
git show e2282d2:src/InsightLearn.Web/Pages/Index.razor
git show e2282d2:src/InsightLearn.Web/Pages/StudentDashboard.razor
git show e2282d2:src/InsightLearn.Web/Components/CourseCard.razor
git show e2282d2:src/InsightLearn.Web/Shared/MainHeader.razor
```

## Standard Migration Template

### For Regular Page

```razor
@page "/page-route"
@using InsightLearn.WebAssembly.Services
@inject IApiClient ApiClient
@inject NavigationManager Navigation
@inject IJSRuntime JSRuntime
@* Add @attribute [Authorize] if auth required *@
@* Add @attribute [Authorize(Roles = "Admin")] for admin pages *@

<PageTitle>Page Title - InsightLearn</PageTitle>

<!-- Copy HTML from old page, keeping Bootstrap/CSS -->
<div class="container">
    @if (isLoading)
    {
        <div class="text-center py-5">
            <div class="spinner-border" role="status"></div>
            <p>Loading...</p>
        </div>
    }
    else if (!string.IsNullOrEmpty(errorMessage))
    {
        <div class="alert alert-danger">
            <i class="fas fa-exclamation-triangle me-2"></i>
            @errorMessage
        </div>
    }
    else
    {
        <!-- Actual content -->
    }
</div>

@code {
    // State variables
    private bool isLoading = false;
    private string errorMessage = "";
    private DataModel data = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            isLoading = true;
            errorMessage = "";
            StateHasChanged();

            // Replace DbContext queries with API calls
            data = await ApiClient.GetAsync<DataModel>("/api/endpoint") ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            errorMessage = "Failed to load data. Please try again.";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    // Event handlers
    private async Task HandleSubmitAsync()
    {
        try
        {
            var response = await ApiClient.PostAsync("/api/endpoint", data);
            if (response != null)
            {
                // Success
                Navigation.NavigateTo("/success-page");
            }
        }
        catch (Exception ex)
        {
            errorMessage = "Operation failed. Please try again.";
        }
    }

    // DTOs (if needed, or move to /Models/)
    public class DataModel
    {
        public string Property { get; set; } = "";
    }
}
```

### For Admin Page

```razor
@page "/admin/page-name"
@using InsightLearn.WebAssembly.Services
@inject IApiClient ApiClient
@inject NavigationManager Navigation
@attribute [Authorize(Roles = "Admin")]

<PageTitle>Admin - Page Name</PageTitle>

<div class="container-fluid py-4">
    <div class="row mb-4">
        <div class="col">
            <h1><i class="fas fa-icon me-2"></i>Page Name</h1>
            <nav aria-label="breadcrumb">
                <ol class="breadcrumb">
                    <li class="breadcrumb-item"><a href="/admin/dashboard">Admin</a></li>
                    <li class="breadcrumb-item active">Page Name</li>
                </ol>
            </nav>
        </div>
        <div class="col-auto">
            <button class="btn btn-primary" @onclick="HandleCreateAsync">
                <i class="fas fa-plus me-2"></i>Create New
            </button>
        </div>
    </div>

    <!-- Content with loading/error states -->
</div>

@code {
    // Admin-specific code
    private List<ItemDto> items = new();
    private bool isLoading = false;
    private string errorMessage = "";

    protected override async Task OnInitializedAsync()
    {
        await LoadItemsAsync();
    }

    private async Task LoadItemsAsync()
    {
        try
        {
            isLoading = true;
            items = await ApiClient.GetAsync<List<ItemDto>>("/api/admin/items") ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            errorMessage = "Failed to load items.";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }
}
```

### For Component

```razor
@* No @page directive for components *@
@using InsightLearn.WebAssembly.Services

<div class="component-container">
    <!-- Component UI -->
</div>

@code {
    [Parameter]
    public DataModel Data { get; set; } = new();

    [Parameter]
    public EventCallback<DataModel> OnClick { get; set; }

    [Inject]
    public IApiClient? ApiClient { get; set; }

    private async Task HandleClickAsync()
    {
        await OnClick.InvokeAsync(Data);
    }
}
```

## Common Replacements

### Remove These (Server-Only)
```csharp
// ❌ REMOVE
@inject ApplicationDbContext DbContext
@inject UserManager<User> UserManager
@inject SignInManager<User> SignInManager
@inject IHttpContextAccessor HttpContextAccessor
@using Microsoft.EntityFrameworkCore

// Database queries
var users = await DbContext.Users.ToListAsync();
var user = await UserManager.GetUserAsync(authState.User);
```

### Replace With (WASM)
```csharp
// ✅ ADD
@inject IApiClient ApiClient
@inject IAuthService AuthService

// API calls
var users = await ApiClient.GetAsync<List<UserDto>>("/api/users");
var user = await ApiClient.GetAsync<UserDto>("/api/users/profile");
```

## API Endpoint Patterns

### Common Patterns
```
GET    /api/{resource}                    - List all
GET    /api/{resource}/{id}                - Get by ID
POST   /api/{resource}                     - Create
PUT    /api/{resource}/{id}                - Update
DELETE /api/{resource}/{id}                - Delete

GET    /api/{resource}/search?query=...    - Search
GET    /api/{resource}?page=1&size=10      - Pagination
```

### Specific Endpoints
```
# Auth
POST   /api/auth/login
POST   /api/auth/register
POST   /api/auth/complete-registration
POST   /api/auth/oauth/google
POST   /api/auth/refresh-token

# Users
GET    /api/users/profile
PUT    /api/users/profile
POST   /api/users/upload-avatar

# Courses
GET    /api/courses
GET    /api/courses/{id}
GET    /api/courses/featured
GET    /api/courses/by-category/{categoryId}
POST   /api/courses
PUT    /api/courses/{id}
DELETE /api/courses/{id}
POST   /api/courses/{id}/enroll

# Dashboard
GET    /api/dashboard/student
GET    /api/dashboard/instructor
GET    /api/dashboard/admin

# Admin
GET    /api/admin/users
POST   /api/admin/users
PUT    /api/admin/users/{id}
DELETE /api/admin/users/{id}
GET    /api/admin/analytics
GET    /api/admin/system-health
GET    /api/admin/access-logs

# Categories
GET    /api/categories
POST   /api/categories
PUT    /api/categories/{id}
DELETE /api/categories/{id}

# Videos
POST   /api/videos/upload
GET    /api/videos/{id}/status

# Certificates
GET    /api/certificates
GET    /api/certificates/{id}/download
```

## Common DTOs

```csharp
// User
public class UserDto
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string[] Roles { get; set; }
}

// Course
public class CourseDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string InstructorName { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public string ThumbnailUrl { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public string CategoryName { get; set; }
    public bool IsPublished { get; set; }
}

// Category
public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string IconClass { get; set; }
    public int CourseCount { get; set; }
}

// Dashboard
public class StudentDashboardDto
{
    public List<EnrolledCourseDto> EnrolledCourses { get; set; }
    public int TotalCompletedLessons { get; set; }
    public List<CertificateDto> Certificates { get; set; }
}

public class InstructorDashboardDto
{
    public List<InstructorCourseDto> Courses { get; set; }
    public int TotalEnrollments { get; set; }
    public double AverageRating { get; set; }
    public decimal TotalRevenue { get; set; }
}

// Generic Response
public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
}

public class ApiResponse<T> : ApiResponse
{
    public T Data { get; set; }
}
```

## Testing Checklist for Each Page

After migration, verify:
- [ ] Page loads without errors
- [ ] Loading state displays correctly
- [ ] Error state displays correctly
- [ ] Data loads from API
- [ ] Forms submit correctly
- [ ] Navigation works
- [ ] Authorization works (if applicable)
- [ ] Styling matches old site
- [ ] Mobile responsive
- [ ] Console has no errors

## Priority Order

1. **CRITICAL (Do First):**
   - Index.razor (homepage)
   - CourseCard.razor component
   - CategoryGrid.razor component
   - HeroSection.razor component
   - StudentDashboard.razor
   - InstructorDashboard.razor
   - Constants.cs, Extensions.cs, Helpers.cs
   - NavMenu.razor (update)

2. **HIGH (Do Second):**
   - Help.razor
   - CookiePolicy.razor
   - MainHeader.razor
   - TopBar.razor
   - Custom404Page.razor

3. **MEDIUM (Do Third):**
   - All Admin pages (20 files)
   - Remaining components

4. **LOW (Do Last):**
   - Test pages (TestAuth, etc.)

## Command Shortcuts

```bash
# Navigate to project
cd /home/mpasqui/kubernetes/Insightlearn/src/InsightLearn.WebAssembly

# View old file
git show e2282d2:src/InsightLearn.Web/Pages/FILENAME.razor

# Check current branch
git branch

# List WASM pages
ls Pages/

# List WASM components
ls Components/

# Check build (when ready)
cd /home/mpasqui/kubernetes/Insightlearn/src/InsightLearn.WebAssembly
dotnet build
```

## Quick Tips

1. **Copy HTML exactly** - Blazor WASM supports same Bootstrap/HTML as Server
2. **Remove server dependencies** - No DbContext, UserManager, etc.
3. **Add loading states** - All API calls need loading indicators
4. **Add error handling** - Try-catch around all API calls
5. **Test authorization** - Use `@attribute [Authorize]` or `@attribute [Authorize(Roles = "Admin")]`
6. **Keep styling** - CSS classes work the same
7. **Use StateHasChanged()** - After async operations
8. **Console.WriteLine** - For debugging (shows in browser console)

## File Size Reference

- Simple page (Privacy): ~50 lines
- Medium page (Login): ~200 lines
- Complex page (CompleteRegistration): ~400 lines
- Admin page: ~300-500 lines
- Simple component: ~50-100 lines
- Complex component: ~200-300 lines

## Estimated Time Per File

- Simple page: 15-30 minutes
- Medium page: 30-60 minutes
- Complex page: 60-90 minutes
- Admin page: 45-90 minutes
- Simple component: 20-30 minutes
- Complex component: 30-60 minutes

Total for 52 files: **25-35 hours**
MVP (20 files): **14-19 hours**
