using InsightLearn.WebAssembly.Models.Config;
using System.Net.Http.Json;
using System.Text.Json;

namespace InsightLearn.WebAssembly.Services;

/// <summary>
/// Service for loading endpoint configuration from backend API (database)
/// with fallback to appsettings.json
/// </summary>
public interface IEndpointConfigurationService
{
    Task<EndpointsConfig> LoadEndpointsAsync();
}

public class EndpointConfigurationService : IEndpointConfigurationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EndpointConfigurationService> _logger;

    public EndpointConfigurationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<EndpointConfigurationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<EndpointsConfig> LoadEndpointsAsync()
    {
        try
        {
            _logger.LogInformation("[ENDPOINTS] Loading endpoints from backend API...");

            // Try to fetch from backend API
            var response = await _httpClient.GetAsync("/api/system/endpoints");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("[ENDPOINTS] Received endpoints from API: {Json}", json);

                var endpointsData = await response.Content.ReadFromJsonAsync<Dictionary<string, Dictionary<string, string>>>();

                if (endpointsData != null)
                {
                    var config = MapToEndpointsConfig(endpointsData);
                    _logger.LogInformation("[ENDPOINTS] Successfully loaded endpoints from database");
                    return config;
                }
            }
            else
            {
                _logger.LogWarning("[ENDPOINTS] API returned status {StatusCode}, falling back to appsettings.json", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ENDPOINTS] Error loading endpoints from API, falling back to appsettings.json");
        }

        // Fallback to appsettings.json
        _logger.LogInformation("[ENDPOINTS] Loading endpoints from appsettings.json (fallback)");
        var fallbackConfig = _configuration.GetSection("Endpoints").Get<EndpointsConfig>() ?? new EndpointsConfig();
        return fallbackConfig;
    }

    private EndpointsConfig MapToEndpointsConfig(Dictionary<string, Dictionary<string, string>> data)
    {
        var config = new EndpointsConfig();

        if (data.TryGetValue("Auth", out var authData))
        {
            config.Auth = new AuthEndpoints
            {
                Login = GetEndpoint(authData, "Login", config.Auth.Login),
                Register = GetEndpoint(authData, "Register", config.Auth.Register),
                CompleteRegistration = GetEndpoint(authData, "CompleteRegistration", config.Auth.CompleteRegistration),
                Refresh = GetEndpoint(authData, "Refresh", config.Auth.Refresh),
                Me = GetEndpoint(authData, "Me", config.Auth.Me),
                OAuthCallback = GetEndpoint(authData, "OAuthCallback", config.Auth.OAuthCallback)
            };
        }

        if (data.TryGetValue("Courses", out var coursesData))
        {
            config.Courses = new CoursesEndpoints
            {
                GetAll = GetEndpoint(coursesData, "GetAll", config.Courses.GetAll),
                GetById = GetEndpoint(coursesData, "GetById", config.Courses.GetById),
                Create = GetEndpoint(coursesData, "Create", config.Courses.Create),
                Update = GetEndpoint(coursesData, "Update", config.Courses.Update),
                Delete = GetEndpoint(coursesData, "Delete", config.Courses.Delete),
                Search = GetEndpoint(coursesData, "Search", config.Courses.Search),
                GetByCategory = GetEndpoint(coursesData, "GetByCategory", config.Courses.GetByCategory)
            };
        }

        if (data.TryGetValue("Categories", out var categoriesData))
        {
            config.Categories = new CategoriesEndpoints
            {
                GetAll = GetEndpoint(categoriesData, "GetAll", config.Categories.GetAll),
                GetById = GetEndpoint(categoriesData, "GetById", config.Categories.GetById),
                Create = GetEndpoint(categoriesData, "Create", config.Categories.Create),
                Update = GetEndpoint(categoriesData, "Update", config.Categories.Update),
                Delete = GetEndpoint(categoriesData, "Delete", config.Categories.Delete)
            };
        }

        if (data.TryGetValue("Enrollments", out var enrollmentsData))
        {
            config.Enrollments = new EnrollmentsEndpoints
            {
                GetAll = GetEndpoint(enrollmentsData, "GetAll", config.Enrollments.GetAll),
                GetById = GetEndpoint(enrollmentsData, "GetById", config.Enrollments.GetById),
                Create = GetEndpoint(enrollmentsData, "Create", config.Enrollments.Create),
                GetByCourse = GetEndpoint(enrollmentsData, "GetByCourse", config.Enrollments.GetByCourse),
                GetByUser = GetEndpoint(enrollmentsData, "GetByUser", config.Enrollments.GetByUser)
            };
        }

        if (data.TryGetValue("Users", out var usersData))
        {
            config.Users = new UsersEndpoints
            {
                GetAll = GetEndpoint(usersData, "GetAll", config.Users.GetAll),
                GetById = GetEndpoint(usersData, "GetById", config.Users.GetById),
                Update = GetEndpoint(usersData, "Update", config.Users.Update),
                Delete = GetEndpoint(usersData, "Delete", config.Users.Delete),
                GetProfile = GetEndpoint(usersData, "GetProfile", config.Users.GetProfile)
            };
        }

        if (data.TryGetValue("Dashboard", out var dashboardData))
        {
            config.Dashboard = new DashboardEndpoints
            {
                GetStats = GetEndpoint(dashboardData, "GetStats", config.Dashboard.GetStats),
                GetRecentActivity = GetEndpoint(dashboardData, "GetRecentActivity", config.Dashboard.GetRecentActivity)
            };
        }

        if (data.TryGetValue("Reviews", out var reviewsData))
        {
            config.Reviews = new ReviewsEndpoints
            {
                GetAll = GetEndpoint(reviewsData, "GetAll", config.Reviews.GetAll),
                GetById = GetEndpoint(reviewsData, "GetById", config.Reviews.GetById),
                Create = GetEndpoint(reviewsData, "Create", config.Reviews.Create),
                GetByCourse = GetEndpoint(reviewsData, "GetByCourse", config.Reviews.GetByCourse)
            };
        }

        if (data.TryGetValue("Payments", out var paymentsData))
        {
            config.Payments = new PaymentsEndpoints
            {
                CreateCheckout = GetEndpoint(paymentsData, "CreateCheckout", config.Payments.CreateCheckout),
                GetTransactions = GetEndpoint(paymentsData, "GetTransactions", config.Payments.GetTransactions),
                GetTransactionById = GetEndpoint(paymentsData, "GetTransactionById", config.Payments.GetTransactionById)
            };
        }

        if (data.TryGetValue("Chat", out var chatData))
        {
            config.Chat = new ChatEndpoints
            {
                SendMessage = GetEndpoint(chatData, "SendMessage", config.Chat.SendMessage),
                GetHistory = GetEndpoint(chatData, "GetHistory", config.Chat.GetHistory)
            };
        }

        return config;
    }

    private string GetEndpoint(Dictionary<string, string> data, string key, string defaultValue)
    {
        if (data.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }
        return defaultValue;
    }
}
