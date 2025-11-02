using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using InsightLearn.Core.Entities;

namespace InsightLearn.Infrastructure.Data;

/// <summary>
/// Database configuration extensions specifically optimized for Google OAuth authentication.
/// Provides performance optimizations, security constraints, and monitoring capabilities
/// for OAuth-based authentication flows.
/// </summary>
public static class OAuthDbExtensions
{
    /// <summary>
    /// Configures OAuth-specific database optimizations for Google authentication.
    /// </summary>
    public static void ConfigureOAuthOptimizations(this ModelBuilder builder)
    {
        ConfigureUserEntityForOAuth(builder);
        ConfigureIdentityTablesForOAuth(builder);
        ConfigureUserSessionsForOAuth(builder);
        ConfigureLoginTrackingForOAuth(builder);
        ConfigureSecurityEventsForOAuth(builder);
    }

    /// <summary>
    /// Configures the User entity with OAuth-specific optimizations and constraints.
    /// </summary>
    private static void ConfigureUserEntityForOAuth(ModelBuilder builder)
    {
        builder.Entity<User>(entity =>
        {
            // OAuth-specific field configurations
            entity.Property(u => u.GoogleId)
                .HasMaxLength(100)
                .HasComment("Google OAuth unique identifier");

            entity.Property(u => u.GooglePictureUrl)
                .HasMaxLength(500)
                .HasComment("Google profile picture URL");

            entity.Property(u => u.GoogleLocale)
                .HasMaxLength(10)
                .HasComment("Google account locale setting");

            entity.Property(u => u.GoogleGivenName)
                .HasMaxLength(100)
                .HasComment("Google profile given name");

            entity.Property(u => u.GoogleFamilyName)
                .HasMaxLength(100)
                .HasComment("Google profile family name");

            entity.Property(u => u.GoogleTokenExpiry)
                .HasComment("Google OAuth token expiration timestamp");

            // Critical OAuth indexes for fast lookups
            entity.HasIndex(u => u.GoogleId)
                .HasDatabaseName("IX_Users_GoogleId_Unique")
                .IsUnique()
                .HasFilter("[GoogleId] IS NOT NULL");

            entity.HasIndex(u => new { u.Email, u.IsGoogleUser })
                .HasDatabaseName("IX_Users_Email_GoogleUser")
                .HasFilter("[IsGoogleUser] = 1");

            entity.HasIndex(u => new { u.IsGoogleUser, u.EmailConfirmed, u.LockoutEnabled })
                .HasDatabaseName("IX_Users_GoogleAuth_Status")
                .HasFilter("[IsGoogleUser] = 1");

            // Check constraints for data integrity
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Users_GoogleId_NotEmpty",
                "[GoogleId] IS NULL OR LEN([GoogleId]) > 0"));

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Users_GoogleUser_Consistency",
                "([IsGoogleUser] = 0 AND [GoogleId] IS NULL) OR ([IsGoogleUser] = 1 AND [GoogleId] IS NOT NULL)"));
        });
    }

    /// <summary>
    /// Configures Identity tables with OAuth-specific optimizations.
    /// </summary>
    private static void ConfigureIdentityTablesForOAuth(ModelBuilder builder)
    {
        // AspNetUserLogins optimization for Google OAuth
        builder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            // Performance index for Google provider lookups
            entity.HasIndex(ul => new { ul.LoginProvider, ul.ProviderKey, ul.UserId })
                .HasDatabaseName("IX_UserLogins_Google_Provider")
                .HasFilter("[LoginProvider] = 'Google'");

            // Add comments for OAuth fields
            entity.Property(ul => ul.LoginProvider)
                .HasComment("OAuth provider name (e.g., 'Google')");

            entity.Property(ul => ul.ProviderKey)
                .HasComment("OAuth provider unique user identifier");

            entity.Property(ul => ul.ProviderDisplayName)
                .HasComment("OAuth provider display name");
        });

        // AspNetUserTokens optimization for OAuth tokens
        builder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            // Index for token lookups and cleanup
            entity.HasIndex(ut => new { ut.UserId, ut.LoginProvider, ut.Name })
                .HasDatabaseName("IX_UserTokens_OAuth_Lookup");

            // Performance index for token expiration cleanup
            entity.Property(ut => ut.Value)
                .HasComment("OAuth token value or expiration timestamp");
        });
    }

    /// <summary>
    /// Configures UserSession entity for OAuth session management.
    /// </summary>
    private static void ConfigureUserSessionsForOAuth(ModelBuilder builder)
    {
        builder.Entity<UserSession>(entity =>
        {
            // Critical indexes for OAuth session performance
            entity.HasIndex(s => new { s.UserId, s.IsActive, s.LastActivityAt })
                .HasDatabaseName("IX_UserSessions_OAuth_Active")
                .HasFilter("[IsActive] = 1")
                .HasAnnotation("SqlServer:Include", new[] { "SessionId", "JwtTokenId", "StartedAt" });

            entity.HasIndex(s => s.JwtTokenId)
                .HasDatabaseName("IX_UserSessions_JwtToken")
                .IsUnique()
                .HasFilter("[JwtTokenId] IS NOT NULL");

            entity.HasIndex(s => new { s.IpAddress, s.StartedAt })
                .HasDatabaseName("IX_UserSessions_IP_Security"); // Removed temporal filter for SQL Server compatibility

            // Session cleanup index for expired sessions (simplified filter)
            // Using CAST for SQL Server bit column compatibility
            entity.HasIndex(s => new { s.IsActive, s.LastActivityAt })
                .HasDatabaseName("IX_UserSessions_Cleanup")
                .HasFilter("[IsActive] = CAST(1 AS bit)");

            // Enhanced field configurations for OAuth
            entity.Property(s => s.JwtTokenId)
                .HasMaxLength(100)
                .HasComment("JWT token identifier for OAuth session tracking");

            entity.Property(s => s.JwtToken)
                .HasMaxLength(2000)
                .HasComment("Full JWT token for OAuth authentication");

            entity.Property(s => s.EndReason)
                .HasMaxLength(50)
                .HasComment("Session termination reason (Logout, Timeout, TokenExpiry, Forced)");

            // Check constraint for session state consistency
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_UserSessions_ActiveState",
                "([IsActive] = 1 AND [EndedAt] IS NULL) OR ([IsActive] = 0 AND [EndedAt] IS NOT NULL)"));

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_UserSessions_JWT_Consistency",
                "([JwtTokenId] IS NULL AND [JwtToken] IS NULL) OR ([JwtTokenId] IS NOT NULL AND [JwtToken] IS NOT NULL)"));
        });
    }

    /// <summary>
    /// Configures LoginAttempt entity for OAuth security monitoring.
    /// </summary>
    private static void ConfigureLoginTrackingForOAuth(ModelBuilder builder)
    {
        builder.Entity<LoginAttempt>(entity =>
        {
            // Critical security indexes for OAuth monitoring
            entity.HasIndex(la => new { la.Email, la.IsSuccess, la.AttemptedAt })
                .HasDatabaseName("IX_LoginAttempts_OAuth_Email"); // Removed temporal filter for SQL Server compatibility

            entity.HasIndex(la => new { la.IpAddress, la.IsSuccess, la.AttemptedAt })
                .HasDatabaseName("IX_LoginAttempts_OAuth_IP"); // Removed temporal filter for SQL Server compatibility

            entity.HasIndex(la => new { la.LoginMethod, la.IsSuccess, la.AttemptedAt })
                .HasDatabaseName("IX_LoginAttempts_OAuth_Method");

            // OAuth-specific failure tracking
            entity.HasIndex(la => new { la.IsSuccess, la.FailureReason, la.AttemptedAt })
                .HasDatabaseName("IX_LoginAttempts_OAuth_Failures")
                .HasFilter("[IsSuccess] = 0"); // Removed temporal filter for SQL Server compatibility

            // Enhanced field configurations
            entity.Property(la => la.LoginMethod)
                .HasMaxLength(50)
                .HasComment("Authentication method (Password, Google, JWT)");

            entity.Property(la => la.FailureReason)
                .HasMaxLength(200)
                .HasComment("OAuth failure reason for debugging");
        });
    }

    /// <summary>
    /// Configures SecurityEvent entity for OAuth security monitoring.
    /// </summary>
    private static void ConfigureSecurityEventsForOAuth(ModelBuilder builder)
    {
        builder.Entity<SecurityEvent>(entity =>
        {
            // OAuth security monitoring indexes
            entity.HasIndex(se => new { se.EventType, se.Severity, se.DetectedAt })
                .HasDatabaseName("IX_SecurityEvents_OAuth_Type");

            entity.HasIndex(se => new { se.UserId, se.EventType, se.DetectedAt })
                .HasDatabaseName("IX_SecurityEvents_OAuth_User")
                .HasFilter("[UserId] IS NOT NULL");

            // Separate indices for High and Critical severity events (SQL Server doesn't support OR in index filters)
            entity.HasIndex(se => new { se.IpAddress, se.Severity, se.DetectedAt })
                .HasDatabaseName("IX_SecurityEvents_OAuth_IP_High")
                .HasFilter("[Severity] = 'High'"); // High severity events only

            entity.HasIndex(se => new { se.IpAddress, se.Severity, se.DetectedAt })
                .HasDatabaseName("IX_SecurityEvents_OAuth_IP_Critical")
                .HasFilter("[Severity] = 'Critical'"); // Critical severity events only

            // OAuth-specific event tracking
            entity.Property(se => se.EventType)
                .HasMaxLength(100)
                .HasComment("Security event type (OAuth_Login, OAuth_Failure, Token_Refresh, etc.)");

            entity.Property(se => se.EventDetails)
                .HasComment("OAuth-specific event details and context");
        });
    }

    /// <summary>
    /// Creates database views for OAuth analytics and monitoring.
    /// </summary>
    public static void CreateOAuthViews(this ModelBuilder builder)
    {
        // View for OAuth login analytics
        builder.Entity<object>().ToView("vw_OAuth_LoginAnalytics", "dbo")
            .HasNoKey()
            .ToSqlQuery(@"
                SELECT
                    CAST(la.AttemptedAt AS DATE) as LoginDate,
                    la.LoginMethod,
                    COUNT(*) as TotalAttempts,
                    SUM(CASE WHEN la.IsSuccess = 1 THEN 1 ELSE 0 END) as SuccessfulLogins,
                    SUM(CASE WHEN la.IsSuccess = 0 THEN 1 ELSE 0 END) as FailedLogins,
                    CAST(SUM(CASE WHEN la.IsSuccess = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS DECIMAL(5,2)) as SuccessRate
                FROM LoginAttempts la
                WHERE la.AttemptedAt >= DATEADD(day, -30, GETUTCDATE())
                GROUP BY CAST(la.AttemptedAt AS DATE), la.LoginMethod
            ");

        // View for OAuth session monitoring
        builder.Entity<object>().ToView("vw_OAuth_ActiveSessions", "dbo")
            .HasNoKey()
            .ToSqlQuery(@"
                SELECT
                    us.UserId,
                    u.Email,
                    u.IsGoogleUser,
                    us.SessionId,
                    us.StartedAt,
                    us.LastActivityAt,
                    us.IpAddress,
                    us.DeviceType,
                    us.Platform,
                    DATEDIFF(minute, us.LastActivityAt, GETUTCDATE()) as MinutesInactive,
                    CASE WHEN us.JwtTokenId IS NOT NULL THEN 'JWT' ELSE 'Session' END as AuthType
                FROM UserSessions us
                INNER JOIN Users u ON us.UserId = u.Id
                WHERE us.IsActive = 1
                    AND us.LastActivityAt >= DATEADD(hour, -24, GETUTCDATE())
            ");
    }

    /// <summary>
    /// Creates stored procedures for OAuth maintenance and analytics.
    /// </summary>
    public static void CreateOAuthStoredProcedures(this ModelBuilder builder)
    {
        // Note: These would typically be created via migrations or separate SQL scripts
        // This method serves as documentation for the required stored procedures

        /*
        CREATE PROCEDURE sp_OAuth_CleanupExpiredSessions
        AS
        BEGIN
            -- Cleanup expired OAuth sessions older than 24 hours
            UPDATE UserSessions
            SET IsActive = 0,
                EndedAt = GETUTCDATE(),
                EndReason = 'Timeout'
            WHERE IsActive = 1
                AND LastActivityAt < DATEADD(hour, -24, GETUTCDATE());
        END

        CREATE PROCEDURE sp_OAuth_GetUserByGoogleId
            @GoogleId NVARCHAR(100)
        AS
        BEGIN
            SELECT u.*, ul.ProviderKey, ul.ProviderDisplayName
            FROM Users u
            LEFT JOIN UserLogins ul ON u.Id = ul.UserId AND ul.LoginProvider = 'Google'
            WHERE u.GoogleId = @GoogleId;
        END

        CREATE PROCEDURE sp_OAuth_SecurityReport
            @DaysBack INT = 7
        AS
        BEGIN
            -- OAuth security monitoring report
            SELECT
                'Failed Logins' as MetricType,
                COUNT(*) as Count,
                STRING_AGG(DISTINCT IpAddress, ', ') as TopIPs
            FROM LoginAttempts
            WHERE IsSuccess = 0
                AND AttemptedAt >= DATEADD(day, -@DaysBack, GETUTCDATE())
                AND LoginMethod LIKE '%Google%'

            UNION ALL

            SELECT
                'Suspicious IPs' as MetricType,
                COUNT(DISTINCT IpAddress) as Count,
                STRING_AGG(DISTINCT IpAddress, ', ') as TopIPs
            FROM LoginAttempts
            WHERE IsSuccess = 0
                AND AttemptedAt >= DATEADD(day, -@DaysBack, GETUTCDATE())
            GROUP BY IpAddress
            HAVING COUNT(*) > 10;
        END
        */
    }
}