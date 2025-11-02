using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace InsightLearn.Infrastructure.Data;

public static class EfCoreExtensions
{
    public static IServiceCollection AddEfCoreOptimizations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<InsightLearnDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                // Enable connection resiliency
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                
                // Configure command timeout
                sqlOptions.CommandTimeout(60);
                
                // Use query splitting to avoid Cartesian explosion
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
            
            // Configure query tracking behavior for better performance
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
            
            // Enable lazy loading proxies if needed
            // options.UseLazyLoadingProxies();
            
            // Configure logging and diagnostics for development
            #if DEBUG
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
            options.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
            #endif
            
            // Configure warnings
            options.ConfigureWarnings(warnings =>
            {
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.RowLimitingOperationWithoutOrderByWarning);
                // Optionally ignore other warnings that are expected in your application
            });
        });

        return services;
    }
    
    public static void ApplyOptimalIndexes(this ModelBuilder builder)
    {
        // ====== GOOGLE OAUTH CRITICAL INDEXES ======

        // Users - Google OAuth lookup performance (CRITICAL for OAuth login)
        builder.Entity<InsightLearn.Core.Entities.User>()
            .HasIndex(u => u.GoogleId)
            .HasDatabaseName("IX_Users_GoogleId")
            .HasFilter("[GoogleId] IS NOT NULL")
            .IsUnique();

        builder.Entity<InsightLearn.Core.Entities.User>()
            .HasIndex(u => new { u.IsGoogleUser, u.Email })
            .HasDatabaseName("IX_Users_GoogleUserEmail")
            .HasFilter("[IsGoogleUser] = 1");

        // AspNetUserLogins - External provider lookups (CRITICAL for OAuth)
        // Note: Primary composite key already exists (LoginProvider, ProviderKey)
        // Additional index for performance on Google provider lookups
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>()
            .HasIndex("LoginProvider", "ProviderKey")
            .HasDatabaseName("IX_UserLogins_ProviderKey")
            .HasFilter("[LoginProvider] = 'Google'");

        // User Sessions - OAuth session management (CRITICAL for performance)
        builder.Entity<InsightLearn.Core.Entities.UserSession>()
            .HasIndex(s => new { s.UserId, s.IsActive, s.LastActivityAt })
            .HasDatabaseName("IX_UserSessions_UserActiveActivity")
            .HasFilter("[IsActive] = 1");

        builder.Entity<InsightLearn.Core.Entities.UserSession>()
            .HasIndex(s => s.SessionId)
            .HasDatabaseName("IX_UserSessions_SessionId")
            .IsUnique();

        builder.Entity<InsightLearn.Core.Entities.UserSession>()
            .HasIndex(s => s.JwtTokenId)
            .HasDatabaseName("IX_UserSessions_JwtTokenId")
            .HasFilter("[JwtTokenId] IS NOT NULL");

        // Login Attempts - Security monitoring for OAuth
        builder.Entity<InsightLearn.Core.Entities.LoginAttempt>()
            .HasIndex(la => new { la.Email, la.IsSuccess, la.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_EmailSuccessDate");

        builder.Entity<InsightLearn.Core.Entities.LoginAttempt>()
            .HasIndex(la => new { la.IpAddress, la.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_IpDate");

        // ====== EXISTING APPLICATION INDEXES ======

        // Courses - Most common query patterns
        builder.Entity<InsightLearn.Core.Entities.Course>()
            .HasIndex(c => new { c.CategoryId, c.Status, c.IsActive })
            .HasDatabaseName("IX_Courses_CategoryStatusActive")
            .HasFilter("[IsActive] = 1 AND [Status] = 2");

        builder.Entity<InsightLearn.Core.Entities.Course>()
            .HasIndex(c => new { c.InstructorId, c.Status })
            .HasDatabaseName("IX_Courses_InstructorStatus");

        // Enrollments - User progress tracking
        builder.Entity<InsightLearn.Core.Entities.Enrollment>()
            .HasIndex(e => new { e.UserId, e.Status })
            .HasDatabaseName("IX_Enrollments_UserStatus");

        // Lesson Progress - User learning analytics
        builder.Entity<InsightLearn.Core.Entities.LessonProgress>()
            .HasIndex(lp => new { lp.UserId, lp.IsCompleted })
            .HasDatabaseName("IX_LessonProgress_UserCompleted");

        // Reviews - Course ratings and feedback
        builder.Entity<InsightLearn.Core.Entities.Review>()
            .HasIndex(r => new { r.CourseId, r.Rating })
            .HasDatabaseName("IX_Reviews_CourseRating");

        // Discussions - Course Q&A
        builder.Entity<InsightLearn.Core.Entities.Discussion>()
            .HasIndex(d => new { d.CourseId, d.Type, d.CreatedAt })
            .HasDatabaseName("IX_Discussions_CourseTypeDate");

        // Payments - Financial tracking
        builder.Entity<InsightLearn.Core.Entities.Payment>()
            .HasIndex(p => new { p.UserId, p.Status, p.CreatedAt })
            .HasDatabaseName("IX_Payments_UserStatusDate");
    }
    
    public static void ConfigureGlobalQueryFilters(this ModelBuilder builder)
    {
        // Apply global query filters for soft delete
        builder.Entity<InsightLearn.Core.Entities.Course>()
            .HasQueryFilter(c => c.IsActive);
            
        builder.Entity<InsightLearn.Core.Entities.Section>()
            .HasQueryFilter(s => s.IsActive);
            
        builder.Entity<InsightLearn.Core.Entities.Lesson>()
            .HasQueryFilter(l => l.IsActive);
            
        builder.Entity<InsightLearn.Core.Entities.Category>()
            .HasQueryFilter(c => c.IsActive);
            
        builder.Entity<InsightLearn.Core.Entities.DiscussionComment>()
            .HasQueryFilter(dc => dc.IsActive);
    }
    
    public static void ConfigurePerformanceOptimizations(this ModelBuilder builder)
    {
        // Note: Split queries are now configured at the DbContext level in OnConfiguring method
        
        // Configure decimal precision globally
        foreach (var property in builder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }
        
        // Configure string lengths for common properties
        foreach (var property in builder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(string) && p.GetMaxLength() == null))
        {
            // Set default max length for string properties without explicit length
            if (property.Name.EndsWith("Url"))
                property.SetMaxLength(500);
            else if (property.Name.Contains("Description"))
                property.SetMaxLength(2000);
            else if (property.Name.Contains("Content"))
                property.SetMaxLength(4000);
            else
                property.SetMaxLength(255);
        }
    }
}