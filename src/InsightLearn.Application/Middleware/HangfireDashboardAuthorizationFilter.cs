using Hangfire.Dashboard;

namespace InsightLearn.Application.Middleware
{
    /// <summary>
    /// Custom authorization filter for Hangfire Dashboard.
    /// Restricts access to Admin users only.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // Allow access only if user is authenticated and has Admin role
            return httpContext.User.Identity?.IsAuthenticated == true
                   && httpContext.User.IsInRole("Admin");
        }
    }
}
