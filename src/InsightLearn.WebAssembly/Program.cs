using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using InsightLearn.WebAssembly;
using InsightLearn.WebAssembly.Services;
using InsightLearn.WebAssembly.Services.Auth;
using InsightLearn.WebAssembly.Services.Http;
using InsightLearn.WebAssembly.Models.Config;
using Blazored.LocalStorage;
using Blazored.Toast;
using Serilog;
using Serilog.Core;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure Serilog for client-side logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.WithProperty("Application", "InsightLearn.WASM")
    .WriteTo.BrowserConsole()
    .CreateLogger();

builder.Logging.AddSerilog(Log.Logger);

// Add Blazored services
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredToast();

// Configure HttpClient with base address from appsettings
var apiTimeout = int.Parse(builder.Configuration["ApiSettings:Timeout"] ?? "30");

// CRITICAL: Use HostEnvironment.BaseAddress as HttpClient base
// This makes all requests relative to current origin (e.g., http://192.168.49.2:31090/)
// Nginx proxy inside WASM pod will intercept /api/* requests and forward to backend
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
    Timeout = TimeSpan.FromSeconds(apiTimeout)
});

// Register Endpoint Configuration Service - loads from database via API with fallback to appsettings.json
builder.Services.AddScoped<IEndpointConfigurationService, EndpointConfigurationService>();

// Configure EndpointsConfig as Scoped to allow async loading from database
// EndpointConfigurationService.LoadEndpointsAsync() tries database first, then falls back to appsettings.json
// Scoped lifetime avoids deadlock and allows proper async/await pattern
builder.Services.AddScoped<EndpointsConfig>(sp =>
{
    // Get appsettings.json config as synchronous fallback
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("EndpointsConfig");

    // Load directly from appsettings.json as initial config
    // EndpointConfigurationService can be used by services that need dynamic loading
    var config = configuration.GetSection("Endpoints").Get<EndpointsConfig>() ?? new EndpointsConfig();

    logger.LogInformation("[ENDPOINTS] Loaded endpoints from appsettings.json (synchronous fallback)");
    logger.LogInformation("[ENDPOINTS] Use IEndpointConfigurationService.LoadEndpointsAsync() for database-driven endpoints");
    return config;
});

// HTTP Services
builder.Services.AddScoped<IApiClient, ApiClient>();
builder.Services.AddScoped<IAuthHttpClient, AuthHttpClient>();

// Authentication & Authorization
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();
builder.Services.AddScoped<JwtAuthenticationStateProvider>(sp =>
    (JwtAuthenticationStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());

// Business Services
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IChatService, ChatService>();

// SaaS Subscription Services
builder.Services.AddScoped<InsightLearn.WebAssembly.Services.Subscription.ISubscriptionService, InsightLearn.WebAssembly.Services.Subscription.SubscriptionService>();
builder.Services.AddScoped<InsightLearn.WebAssembly.Services.Payout.IPayoutService, InsightLearn.WebAssembly.Services.Payout.PayoutService>();

// Admin Services
builder.Services.AddScoped<InsightLearn.WebAssembly.Services.Admin.IAdminDashboardService, InsightLearn.WebAssembly.Services.Admin.AdminDashboardService>();
builder.Services.AddScoped<InsightLearn.WebAssembly.Services.Admin.IEnhancedDashboardService, InsightLearn.WebAssembly.Services.Admin.EnhancedDashboardService>();
builder.Services.AddScoped<InsightLearn.WebAssembly.Services.Admin.IUserManagementService, InsightLearn.WebAssembly.Services.Admin.UserManagementService>();
builder.Services.AddScoped<InsightLearn.WebAssembly.Services.Admin.ICourseManagementService, InsightLearn.WebAssembly.Services.Admin.CourseManagementService>();
builder.Services.AddScoped<InsightLearn.WebAssembly.Services.Admin.IPrometheusMetricsService, InsightLearn.WebAssembly.Services.Admin.PrometheusMetricsService>();

Log.Information("InsightLearn WebAssembly application starting...");
Log.Information("Base Address: {BaseAddress}", builder.HostEnvironment.BaseAddress);

await builder.Build().RunAsync();
