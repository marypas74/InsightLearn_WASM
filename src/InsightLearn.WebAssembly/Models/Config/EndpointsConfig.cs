namespace InsightLearn.WebAssembly.Models.Config;

/// <summary>
/// Configurazione centralizzata di TUTTI gli endpoint API.
/// NESSUN endpoint deve essere hardcoded nel codice dei servizi.
/// </summary>
public class EndpointsConfig
{
    public AuthEndpoints Auth { get; set; } = new();
    public CoursesEndpoints Courses { get; set; } = new();
    public CategoriesEndpoints Categories { get; set; } = new();
    public EnrollmentsEndpoints Enrollments { get; set; } = new();
    public UsersEndpoints Users { get; set; } = new();
    public DashboardEndpoints Dashboard { get; set; } = new();
    public ReviewsEndpoints Reviews { get; set; } = new();
    public PaymentsEndpoints Payments { get; set; } = new();
    public ChatEndpoints Chat { get; set; } = new();
}

public class AuthEndpoints
{
    public string Login { get; set; } = "api/auth/login";
    public string Register { get; set; } = "api/auth/register";
    public string CompleteRegistration { get; set; } = "api/auth/complete-registration";
    public string Refresh { get; set; } = "api/auth/refresh";
    public string Me { get; set; } = "api/auth/me";
    public string OAuthCallback { get; set; } = "api/auth/oauth-callback";
}

public class CoursesEndpoints
{
    public string GetAll { get; set; } = "api/courses";
    public string GetById { get; set; } = "api/courses/{0}";
    public string Create { get; set; } = "api/courses";
    public string Update { get; set; } = "api/courses/{0}";
    public string Delete { get; set; } = "api/courses/{0}";
    public string Search { get; set; } = "api/courses/search";
    public string GetByCategory { get; set; } = "api/courses/category/{0}";
}

public class CategoriesEndpoints
{
    public string GetAll { get; set; } = "api/categories";
    public string GetById { get; set; } = "api/categories/{0}";
    public string Create { get; set; } = "api/categories";
    public string Update { get; set; } = "api/categories/{0}";
    public string Delete { get; set; } = "api/categories/{0}";
}

public class EnrollmentsEndpoints
{
    public string GetAll { get; set; } = "api/enrollments";
    public string GetById { get; set; } = "api/enrollments/{0}";
    public string Create { get; set; } = "api/enrollments";
    public string GetByCourse { get; set; } = "api/enrollments/course/{0}";
    public string GetByUser { get; set; } = "api/enrollments/user/{0}";
}

public class UsersEndpoints
{
    public string GetAll { get; set; } = "api/users";
    public string GetById { get; set; } = "api/users/{0}";
    public string Update { get; set; } = "api/users/{0}";
    public string Delete { get; set; } = "api/users/{0}";
    public string GetProfile { get; set; } = "api/users/profile";
}

public class DashboardEndpoints
{
    public string GetStats { get; set; } = "api/dashboard/stats";
    public string GetRecentActivity { get; set; } = "api/dashboard/recent-activity";
}

public class ReviewsEndpoints
{
    public string GetAll { get; set; } = "api/reviews";
    public string GetById { get; set; } = "api/reviews/{0}";
    public string Create { get; set; } = "api/reviews";
    public string GetByCourse { get; set; } = "api/reviews/course/{0}";
}

public class PaymentsEndpoints
{
    public string CreateCheckout { get; set; } = "api/payments/create-checkout";
    public string GetTransactions { get; set; } = "api/payments/transactions";
    public string GetTransactionById { get; set; } = "api/payments/transactions/{0}";
}

public class ChatEndpoints
{
    public string SendMessage { get; set; } = "api/chat/message";
    public string GetHistory { get; set; } = "api/chat/history";
}
