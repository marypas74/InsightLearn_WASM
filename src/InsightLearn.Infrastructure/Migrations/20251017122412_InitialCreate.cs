using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InsightLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ValueType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    IsReadOnly = table.Column<bool>(type: "bit", nullable: false),
                    IsEncrypted = table.Column<bool>(type: "bit", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    DefaultValue = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ValidationRules = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IconUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ColorCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ParentCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ErrorLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    RequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ExceptionType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExceptionMessage = table.Column<string>(type: "nvarchar(max)", maxLength: 255, nullable: false),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", maxLength: 255, nullable: true),
                    InnerException = table.Column<string>(type: "nvarchar(max)", maxLength: 255, nullable: true),
                    RequestPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RequestData = table.Column<string>(type: "nvarchar(max)", maxLength: 255, nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "int", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Error"),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Component = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Method = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LineNumber = table.Column<int>(type: "int", nullable: true),
                    LoggedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    AdditionalData = table.Column<string>(type: "nvarchar(max)", maxLength: 255, nullable: true),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Environment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Version = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: true),
                    NotificationSent = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoogleIntegrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApiKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PropertyId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TrackingCode = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Configuration = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleIntegrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LoginMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "EmailPassword", comment: "Authentication method (Password, Google, JWT)"),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "OAuth failure reason for debugging"),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DeviceFingerprint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    GeolocationData = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RiskScore = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BrowserInfo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AuthProvider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProviderUserId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecurityEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Security event type (OAuth_Login, OAuth_Failure, Token_Refresh, etc.)"),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Medium"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EventDetails = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, comment: "OAuth-specific event details and context"),
                    RiskScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0.00m),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeolocationData = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RelatedSessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RelatedLoginAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AutoBlocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    BlockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NotificationSent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sitemaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    LastGenerated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UrlCount = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sitemaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProfileImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Bio = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DateJoined = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastLoginDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsInstructor = table.Column<bool>(type: "bit", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    GoogleId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Google OAuth unique identifier"),
                    GooglePictureUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Google profile picture URL"),
                    IsGoogleUser = table.Column<bool>(type: "bit", nullable: false),
                    GoogleTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Google OAuth token expiration timestamp"),
                    GoogleLocale = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "Google account locale setting"),
                    GoogleGivenName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Google profile given name"),
                    GoogleFamilyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Google profile family name"),
                    WalletBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    RegistrationCompleted = table.Column<bool>(type: "bit", nullable: false),
                    RegistrationCompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StreetAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    City = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    StateProvince = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UserType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PreferredPaymentMethod = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    HasAgreedToTerms = table.Column<bool>(type: "bit", nullable: false),
                    TermsAgreedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HasAgreedToPrivacyPolicy = table.Column<bool>(type: "bit", nullable: false),
                    PrivacyPolicyAgreedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.CheckConstraint("CK_Users_GoogleId_NotEmpty", "[GoogleId] IS NULL OR LEN([GoogleId]) > 0");
                    table.CheckConstraint("CK_Users_GoogleUser_Consistency", "([IsGoogleUser] = 0 AND [GoogleId] IS NULL) OR ([IsGoogleUser] = 1 AND [GoogleId] IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccessLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    RequestPath = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Referer = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    ResponseTimeMs = table.Column<long>(type: "bigint", nullable: true),
                    AccessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AdditionalData = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AdminAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PerformedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminAuditLogs_Users_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApiRequestLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    RequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Method = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    QueryString = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RequestHeaders = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RequestBody = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "int", nullable: false),
                    ResponseHeaders = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ResponseBody = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RequestSize = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    ResponseSize = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Referer = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Exception = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CacheHit = table.Column<bool>(type: "bit", nullable: true),
                    DatabaseQueries = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    DatabaseDurationMs = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    MemoryUsageMB = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CpuUsageMs = table.Column<long>(type: "bigint", nullable: true),
                    ApiVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ClientApp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Feature = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiRequestLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiRequestLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    InstructorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PreviewVideoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstimatedDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Requirements = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WhatYouWillLearn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Language = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HasCertificate = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MetaDescription = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    AverageRating = table.Column<double>(type: "float", nullable: false),
                    ReviewCount = table.Column<int>(type: "int", nullable: false),
                    EnrollmentCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Courses_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Courses_Users_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DatabaseErrorLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ErrorType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SqlCommand = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Parameters = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SqlErrorNumber = table.Column<int>(type: "int", nullable: true),
                    SqlErrorSeverity = table.Column<int>(type: "int", nullable: true),
                    SqlErrorState = table.Column<int>(type: "int", nullable: true),
                    DatabaseName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TableName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ProcedureName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LineNumber = table.Column<int>(type: "int", nullable: true),
                    ExecutionTimeMs = table.Column<long>(type: "bigint", nullable: true),
                    RowsAffected = table.Column<int>(type: "int", nullable: true),
                    ConnectionString = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsolationLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Exception = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StackTrace = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    LoggedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseErrorLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatabaseErrorLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EntityAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PropertyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OldValue = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangeSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AuditedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdditionalContext = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntityAuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", maxLength: 255, nullable: false),
                    Exception = table.Column<string>(type: "nvarchar(max)", maxLength: 255, nullable: true),
                    Logger = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Application = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MachineName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequestPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Properties = table.Column<string>(type: "nvarchar(max)", maxLength: 255, nullable: true),
                    ThreadId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProcessId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LoginMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MethodType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    FirstUsedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SuccessfulLogins = table.Column<int>(type: "int", nullable: false),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false),
                    ProviderUserId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProviderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProviderAccountEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginMethods_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EncryptedCardNumber = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LastFourDigits = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    CardType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EncryptedCardholderName = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExpirationMonth = table.Column<int>(type: "int", nullable: true),
                    ExpirationYear = table.Column<int>(type: "int", nullable: true),
                    EncryptedCvv = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BillingAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BillingCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BillingState = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BillingPostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BillingCountry = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    PayPalEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    EncryptedBankAccountNumber = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BankRoutingNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExternalPaymentMethodId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerificationNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SecurityFingerprint = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RiskScore = table.Column<int>(type: "int", nullable: false),
                    EncryptionKeyId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentMethods_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MetricType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MetricName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Component = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CollectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Threshold = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsAlert = table.Column<bool>(type: "bit", nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceMetrics_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SettingChangeLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettingKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ChangedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettingChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettingChangeLogs_ApplicationSettings_SettingId",
                        column: x => x.SettingId,
                        principalTable: "ApplicationSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SettingChangeLogs_Users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, comment: "OAuth provider name (e.g., 'Google')"),
                    ProviderKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, comment: "OAuth provider unique user identifier"),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "OAuth provider display name"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndReason = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "Session termination reason (Logout, Timeout, TokenExpiry, Forced)"),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DeviceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Browser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LoginAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JwtTokenId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "JWT token identifier for OAuth session tracking"),
                    JwtToken = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true, comment: "Full JWT token for OAuth authentication"),
                    GeolocationData = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ActivityCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    DataTransferred = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    LastPageVisited = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TimeZone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.CheckConstraint("CK_UserSessions_ActiveState", "([IsActive] = 1 AND [EndedAt] IS NULL) OR ([IsActive] = 0 AND [EndedAt] IS NOT NULL)");
                    table.CheckConstraint("CK_UserSessions_JWT_Consistency", "([JwtTokenId] IS NULL AND [JwtToken] IS NULL) OR ([JwtTokenId] IS NOT NULL AND [JwtToken] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "OAuth token value or expiration timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValidationErrorLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValidationSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FieldValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ValidationRule = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ModelType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RequestPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RequestData = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ValidatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsClientSideValidated = table.Column<bool>(type: "bit", nullable: false),
                    ValidationCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationErrorLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationErrorLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Coupons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinimumAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaximumDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    UsageLimit = table.Column<int>(type: "int", nullable: true),
                    UsedCount = table.Column<int>(type: "int", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InstructorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coupons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Coupons_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Coupons_Users_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    HelpfulVotes = table.Column<int>(type: "int", nullable: false),
                    UnhelpfulVotes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sections_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeoAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SeoScore = table.Column<int>(type: "int", nullable: false),
                    Issues = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Suggestions = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Recommendations = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AuditDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TechnicalData = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeoAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeoAudits_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SeoSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MetaTitle = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    MetaDescription = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    MetaKeywords = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CanonicalUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OgTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OgDescription = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    OgImage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OgType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "website"),
                    TwitterTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TwitterDescription = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TwitterImage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TwitterCard = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StructuredData = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NoIndex = table.Column<bool>(type: "bit", nullable: false),
                    NoFollow = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    ChangeFrequency = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RobotsDirective = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CustomCode = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeoSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeoSettings_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeoSettings_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeoSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethodAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentMethodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Metadata = table.Column<string>(type: "nvarchar(max)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethodAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentMethodAuditLogs_PaymentMethods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalTable: "PaymentMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentMethodAuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CouponId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PaymentGatewayId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BillingAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PaymentMethodId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Coupons_CouponId",
                        column: x => x.CouponId,
                        principalTable: "Coupons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Payments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_PaymentMethods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalTable: "PaymentMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReviewVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsHelpful = table.Column<bool>(type: "bit", nullable: false),
                    VotedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewVotes_Reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "Reviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReviewVotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    IsFree = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    VideoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VideoThumbnailUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContentText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttachmentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    VideoQuality = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    VideoFileSize = table.Column<long>(type: "bigint", nullable: true),
                    VideoFormat = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lessons_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Discussions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    UpVotes = table.Column<int>(type: "int", nullable: false),
                    DownVotes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discussions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Discussions_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Discussions_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Discussions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAccessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrentLessonIndex = table.Column<int>(type: "int", nullable: false),
                    CurrentLessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HasCertificate = table.Column<bool>(type: "bit", nullable: false),
                    CompletedLessons = table.Column<int>(type: "int", nullable: false),
                    TotalWatchedMinutes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Enrollments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_Lessons_CurrentLessonId",
                        column: x => x.CurrentLessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Enrollments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LessonProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    WatchedMinutes = table.Column<int>(type: "int", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonProgress_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonProgress_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    TimeStamp = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notes_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiscussionComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiscussionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentCommentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsBestAnswer = table.Column<bool>(type: "bit", nullable: false),
                    IsInstructorReply = table.Column<bool>(type: "bit", nullable: false),
                    UpVotes = table.Column<int>(type: "int", nullable: false),
                    DownVotes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscussionComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscussionComments_DiscussionComments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "DiscussionComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiscussionComments_Discussions_DiscussionId",
                        column: x => x.DiscussionId,
                        principalTable: "Discussions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscussionComments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DiscussionVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiscussionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsUpVote = table.Column<bool>(type: "bit", nullable: false),
                    VotedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscussionVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscussionVotes_Discussions_DiscussionId",
                        column: x => x.DiscussionId,
                        principalTable: "Discussions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscussionVotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Certificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CertificateNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokeReason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TemplateUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PdfUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SkillsAcquired = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CourseHours = table.Column<int>(type: "int", nullable: false),
                    CourseRating = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Certificates_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Certificates_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Certificates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DiscussionCommentVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsUpVote = table.Column<bool>(type: "bit", nullable: false),
                    VotedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscussionCommentVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscussionCommentVotes_DiscussionComments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "DiscussionComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscussionCommentVotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "ColorCode", "CreatedAt", "Description", "IconUrl", "IsActive", "IsFeatured", "Name", "OrderIndex", "ParentCategoryId", "Slug", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "#007bff", new DateTime(2025, 10, 17, 12, 24, 11, 764, DateTimeKind.Utc).AddTicks(6039), null, "fas fa-code", true, false, "Development", 1, null, "development", null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "#28a745", new DateTime(2025, 10, 17, 12, 24, 11, 764, DateTimeKind.Utc).AddTicks(6071), null, "fas fa-briefcase", true, false, "Business", 2, null, "business", null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "#17a2b8", new DateTime(2025, 10, 17, 12, 24, 11, 764, DateTimeKind.Utc).AddTicks(6076), null, "fas fa-laptop", true, false, "IT & Software", 3, null, "it-software", null },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "#e83e8c", new DateTime(2025, 10, 17, 12, 24, 11, 764, DateTimeKind.Utc).AddTicks(6083), null, "fas fa-paint-brush", true, false, "Design", 4, null, "design", null },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "#fd7e14", new DateTime(2025, 10, 17, 12, 24, 11, 764, DateTimeKind.Utc).AddTicks(6089), null, "fas fa-bullhorn", true, false, "Marketing", 5, null, "marketing", null },
                    { new Guid("66666666-6666-6666-6666-666666666666"), "#6f42c1", new DateTime(2025, 10, 17, 12, 24, 11, 764, DateTimeKind.Utc).AddTicks(6094), null, "fas fa-camera", true, false, "Photography & Video", 6, null, "photography-video", null },
                    { new Guid("77777777-7777-7777-7777-777777777777"), "#20c997", new DateTime(2025, 10, 17, 12, 24, 11, 764, DateTimeKind.Utc).AddTicks(6098), null, "fas fa-music", true, false, "Music", 7, null, "music", null },
                    { new Guid("88888888-8888-8888-8888-888888888888"), "#dc3545", new DateTime(2025, 10, 17, 12, 24, 11, 764, DateTimeKind.Utc).AddTicks(6103), null, "fas fa-dumbbell", true, false, "Health & Fitness", 8, null, "health-fitness", null }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), null, "Administrator", "ADMINISTRATOR" },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), null, "Instructor", "INSTRUCTOR" },
                    { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), null, "Student", "STUDENT" },
                    { new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), null, "Moderator", "MODERATOR" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessLogs_AccessedAt",
                table: "AccessLogs",
                column: "AccessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AccessLogs_IpAddress",
                table: "AccessLogs",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_AccessLogs_UserId",
                table: "AccessLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessLogs_UserId_AccessedAt",
                table: "AccessLogs",
                columns: new[] { "UserId", "AccessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_Action",
                table: "AdminAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_AdminUserId",
                table: "AdminAuditLogs",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_AdminUserId_PerformedAt",
                table: "AdminAuditLogs",
                columns: new[] { "AdminUserId", "PerformedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_EntityType",
                table: "AdminAuditLogs",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_EntityType_EntityId",
                table: "AdminAuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_PerformedAt",
                table: "AdminAuditLogs",
                column: "PerformedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApiRequestLogs_CorrelationId",
                table: "ApiRequestLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiRequestLogs_DurationMs",
                table: "ApiRequestLogs",
                column: "DurationMs");

            migrationBuilder.CreateIndex(
                name: "IX_ApiRequestLogs_Feature",
                table: "ApiRequestLogs",
                column: "Feature");

            migrationBuilder.CreateIndex(
                name: "IX_ApiRequestLogs_Path_RequestedAt",
                table: "ApiRequestLogs",
                columns: new[] { "Path", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiRequestLogs_RequestedAt",
                table: "ApiRequestLogs",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApiRequestLogs_RequestId",
                table: "ApiRequestLogs",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiRequestLogs_ResponseStatusCode",
                table: "ApiRequestLogs",
                column: "ResponseStatusCode");

            migrationBuilder.CreateIndex(
                name: "IX_ApiRequestLogs_UserId_RequestedAt",
                table: "ApiRequestLogs",
                columns: new[] { "UserId", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_IsActive",
                table: "Categories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_OrderIndex",
                table: "Categories",
                column: "OrderIndex");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentCategoryId",
                table: "Categories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_CertificateNumber",
                table: "Certificates",
                column: "CertificateNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_CourseId",
                table: "Certificates",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_EnrollmentId",
                table: "Certificates",
                column: "EnrollmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_IssuedAt",
                table: "Certificates",
                column: "IssuedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_UserId",
                table: "Certificates",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_Code",
                table: "Coupons",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_CourseId",
                table: "Coupons",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_InstructorId",
                table: "Coupons",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_ValidFrom",
                table: "Coupons",
                column: "ValidFrom");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_ValidUntil",
                table: "Coupons",
                column: "ValidUntil");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CategoryStatusActive",
                table: "Courses",
                columns: new[] { "CategoryId", "Status", "IsActive" },
                filter: "[IsActive] = 1 AND [Status] = 2");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CreatedAt",
                table: "Courses",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_InstructorStatus",
                table: "Courses",
                columns: new[] { "InstructorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Slug",
                table: "Courses",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Status",
                table: "Courses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Title",
                table: "Courses",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseErrorLogs_UserId",
                table: "DatabaseErrorLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionComments_CreatedAt",
                table: "DiscussionComments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionComments_DiscussionId",
                table: "DiscussionComments",
                column: "DiscussionId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionComments_ParentCommentId",
                table: "DiscussionComments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionComments_UserId",
                table: "DiscussionComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionCommentVotes_CommentId",
                table: "DiscussionCommentVotes",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionCommentVotes_User_Comment",
                table: "DiscussionCommentVotes",
                columns: new[] { "UserId", "CommentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Discussions_CourseId",
                table: "Discussions",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Discussions_CourseTypeDate",
                table: "Discussions",
                columns: new[] { "CourseId", "Type", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Discussions_CreatedAt",
                table: "Discussions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Discussions_LessonId",
                table: "Discussions",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_Discussions_UserId",
                table: "Discussions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionVotes_DiscussionId",
                table: "DiscussionVotes",
                column: "DiscussionId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionVotes_User_Discussion",
                table: "DiscussionVotes",
                columns: new[] { "UserId", "DiscussionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_CourseId",
                table: "Enrollments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_CurrentLessonId",
                table: "Enrollments",
                column: "CurrentLessonId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_EnrolledAt",
                table: "Enrollments",
                column: "EnrolledAt");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_Status",
                table: "Enrollments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_UserId_CourseId",
                table: "Enrollments",
                columns: new[] { "UserId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_UserStatus",
                table: "Enrollments",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityAuditLogs_UserId",
                table: "EntityAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_ExceptionType_LoggedAt",
                table: "ErrorLogs",
                columns: new[] { "ExceptionType", "LoggedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_IpAddress_LoggedAt",
                table: "ErrorLogs",
                columns: new[] { "IpAddress", "LoggedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_IsResolved",
                table: "ErrorLogs",
                column: "IsResolved");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_Severity_LoggedAt",
                table: "ErrorLogs",
                columns: new[] { "Severity", "LoggedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_UserId_LoggedAt",
                table: "ErrorLogs",
                columns: new[] { "UserId", "LoggedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GoogleIntegrations_ServiceName",
                table: "GoogleIntegrations",
                column: "ServiceName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgress_CompletedAt",
                table: "LessonProgress",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgress_IsCompleted",
                table: "LessonProgress",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgress_LessonId",
                table: "LessonProgress",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgress_UserCompleted",
                table: "LessonProgress",
                columns: new[] { "UserId", "IsCompleted" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgress_UserId_LessonId",
                table: "LessonProgress",
                columns: new[] { "UserId", "LessonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_IsActive",
                table: "Lessons",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_OrderIndex",
                table: "Lessons",
                column: "OrderIndex");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_SectionId_OrderIndex",
                table: "Lessons",
                columns: new[] { "SectionId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_Type",
                table: "Lessons",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_Application",
                table: "LogEntries",
                column: "Application");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_Application_Timestamp",
                table: "LogEntries",
                columns: new[] { "Application", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_Level",
                table: "LogEntries",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_Level_Timestamp",
                table: "LogEntries",
                columns: new[] { "Level", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_Logger",
                table: "LogEntries",
                column: "Logger");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_Timestamp",
                table: "LogEntries",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_UserId",
                table: "LogEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_UserId_Timestamp",
                table: "LogEntries",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_CorrelationId",
                table: "LoginAttempts",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_Email_AttemptedAt",
                table: "LoginAttempts",
                columns: new[] { "Email", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_IpDate",
                table: "LoginAttempts",
                columns: new[] { "IpAddress", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_IsSuccess_AttemptedAt",
                table: "LoginAttempts",
                columns: new[] { "IsSuccess", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_LoginMethod",
                table: "LoginAttempts",
                column: "LoginMethod");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_OAuth_Email",
                table: "LoginAttempts",
                columns: new[] { "Email", "IsSuccess", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_OAuth_Failures",
                table: "LoginAttempts",
                columns: new[] { "IsSuccess", "FailureReason", "AttemptedAt" },
                filter: "[IsSuccess] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_OAuth_IP",
                table: "LoginAttempts",
                columns: new[] { "IpAddress", "IsSuccess", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_OAuth_Method",
                table: "LoginAttempts",
                columns: new[] { "LoginMethod", "IsSuccess", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_RiskScore",
                table: "LoginAttempts",
                column: "RiskScore");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_UserId_AttemptedAt",
                table: "LoginAttempts",
                columns: new[] { "UserId", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginMethods_UserId",
                table: "LoginMethods",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_CreatedAt",
                table: "Notes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_LessonId",
                table: "Notes",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_User_Lesson",
                table: "Notes",
                columns: new[] { "UserId", "LessonId" });

            migrationBuilder.CreateIndex(
                name: "IX_Notes_UserId",
                table: "Notes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethodAuditLogs_Action",
                table: "PaymentMethodAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethodAuditLogs_CreatedAt",
                table: "PaymentMethodAuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethodAuditLogs_PaymentMethodId",
                table: "PaymentMethodAuditLogs",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethodAuditLogs_PaymentMethodId_CreatedAt",
                table: "PaymentMethodAuditLogs",
                columns: new[] { "PaymentMethodId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethodAuditLogs_UserId",
                table: "PaymentMethodAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethodAuditLogs_UserId_CreatedAt",
                table: "PaymentMethodAuditLogs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_CreatedAt",
                table: "PaymentMethods",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_IsActive",
                table: "PaymentMethods",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_SecurityFingerprint",
                table: "PaymentMethods",
                column: "SecurityFingerprint");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_Type",
                table: "PaymentMethods",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_UserId",
                table: "PaymentMethods",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_UserId_IsActive_Type",
                table: "PaymentMethods",
                columns: new[] { "UserId", "IsActive", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_UserId_IsDefault_Unique",
                table: "PaymentMethods",
                columns: new[] { "UserId", "IsDefault" },
                unique: true,
                filter: "[IsDefault] = 1 AND [IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CouponId",
                table: "Payments",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CourseId",
                table: "Payments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CreatedAt",
                table: "Payments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentMethodId",
                table: "Payments",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TransactionId",
                table: "Payments",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UserId",
                table: "Payments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UserStatusDate",
                table: "Payments",
                columns: new[] { "UserId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_UserId",
                table: "PerformanceMetrics",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CourseRating",
                table: "Reviews",
                columns: new[] { "CourseId", "Rating" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CreatedAt",
                table: "Reviews",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_Rating",
                table: "Reviews",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId_CourseId",
                table: "Reviews",
                columns: new[] { "UserId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewVotes_ReviewId",
                table: "ReviewVotes",
                column: "ReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewVotes_User_Review",
                table: "ReviewVotes",
                columns: new[] { "UserId", "ReviewId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewVotes_VotedAt",
                table: "ReviewVotes",
                column: "VotedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Sections_CourseId_OrderIndex",
                table: "Sections",
                columns: new[] { "CourseId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_Sections_IsActive",
                table: "Sections",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Sections_OrderIndex",
                table: "Sections",
                column: "OrderIndex");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_CorrelationId",
                table: "SecurityEvents",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_EventType_DetectedAt",
                table: "SecurityEvents",
                columns: new[] { "EventType", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_IpAddress_DetectedAt",
                table: "SecurityEvents",
                columns: new[] { "IpAddress", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_IsResolved",
                table: "SecurityEvents",
                column: "IsResolved");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_OAuth_IP_Critical",
                table: "SecurityEvents",
                columns: new[] { "IpAddress", "Severity", "DetectedAt" },
                filter: "[Severity] = 'Critical'");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_OAuth_Type",
                table: "SecurityEvents",
                columns: new[] { "EventType", "Severity", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_OAuth_User",
                table: "SecurityEvents",
                columns: new[] { "UserId", "EventType", "DetectedAt" },
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_RiskScore",
                table: "SecurityEvents",
                column: "RiskScore");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_Severity_DetectedAt",
                table: "SecurityEvents",
                columns: new[] { "Severity", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_UserId_DetectedAt",
                table: "SecurityEvents",
                columns: new[] { "UserId", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SeoAudits_CourseId",
                table: "SeoAudits",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_SeoAudits_CreatedAt",
                table: "SeoAudits",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SeoAudits_Url",
                table: "SeoAudits",
                column: "Url");

            migrationBuilder.CreateIndex(
                name: "IX_SeoSettings_CategoryId",
                table: "SeoSettings",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SeoSettings_CourseId",
                table: "SeoSettings",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_SeoSettings_PageUrl",
                table: "SeoSettings",
                column: "PageUrl",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeoSettings_UserId",
                table: "SeoSettings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SettingChangeLogs_ChangedByUserId",
                table: "SettingChangeLogs",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SettingChangeLogs_SettingId",
                table: "SettingChangeLogs",
                column: "SettingId");

            migrationBuilder.CreateIndex(
                name: "IX_Sitemaps_LastGenerated",
                table: "Sitemaps",
                column: "LastGenerated");

            migrationBuilder.CreateIndex(
                name: "IX_Sitemaps_Name",
                table: "Sitemaps",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_Google_Provider",
                table: "UserLogins",
                columns: new[] { "LoginProvider", "ProviderKey", "UserId" },
                filter: "[LoginProvider] = 'Google'");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_ProviderKey",
                table: "UserLogins",
                columns: new[] { "LoginProvider", "ProviderKey" },
                filter: "[LoginProvider] = 'Google'");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DateJoined",
                table: "Users",
                column: "DateJoined");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email_GoogleUser",
                table: "Users",
                columns: new[] { "Email", "IsGoogleUser" },
                filter: "[IsGoogleUser] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Users_GoogleAuth_Status",
                table: "Users",
                columns: new[] { "IsGoogleUser", "EmailConfirmed", "LockoutEnabled" },
                filter: "[IsGoogleUser] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Users_GoogleId_Unique",
                table: "Users",
                column: "GoogleId",
                unique: true,
                filter: "[GoogleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_GoogleUserEmail",
                table: "Users",
                columns: new[] { "IsGoogleUser", "Email" },
                filter: "[IsGoogleUser] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsGoogleUser",
                table: "Users",
                column: "IsGoogleUser");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsInstructor",
                table: "Users",
                column: "IsInstructor");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsVerified",
                table: "Users",
                column: "IsVerified");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_Cleanup",
                table: "UserSessions",
                columns: new[] { "IsActive", "LastActivityAt" },
                filter: "[IsActive] = CAST(1 AS bit)");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_DeviceType",
                table: "UserSessions",
                column: "DeviceType");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_IP_Security",
                table: "UserSessions",
                columns: new[] { "IpAddress", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_IpAddress",
                table: "UserSessions",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_IsActive",
                table: "UserSessions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_JwtToken",
                table: "UserSessions",
                column: "JwtTokenId",
                unique: true,
                filter: "[JwtTokenId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_LastActivityAt",
                table: "UserSessions",
                column: "LastActivityAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_OAuth_Active",
                table: "UserSessions",
                columns: new[] { "UserId", "IsActive", "LastActivityAt" },
                filter: "[IsActive] = 1")
                .Annotation("SqlServer:Include", new[] { "SessionId", "JwtTokenId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_SessionId",
                table: "UserSessions",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId_StartedAt",
                table: "UserSessions",
                columns: new[] { "UserId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserTokens_OAuth_Lookup",
                table: "UserTokens",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_ValidationErrorLogs_UserId",
                table: "ValidationErrorLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessLogs");

            migrationBuilder.DropTable(
                name: "AdminAuditLogs");

            migrationBuilder.DropTable(
                name: "ApiRequestLogs");

            migrationBuilder.DropTable(
                name: "Certificates");

            migrationBuilder.DropTable(
                name: "DatabaseErrorLogs");

            migrationBuilder.DropTable(
                name: "DiscussionCommentVotes");

            migrationBuilder.DropTable(
                name: "DiscussionVotes");

            migrationBuilder.DropTable(
                name: "EntityAuditLogs");

            migrationBuilder.DropTable(
                name: "ErrorLogs");

            migrationBuilder.DropTable(
                name: "GoogleIntegrations");

            migrationBuilder.DropTable(
                name: "LessonProgress");

            migrationBuilder.DropTable(
                name: "LogEntries");

            migrationBuilder.DropTable(
                name: "LoginAttempts");

            migrationBuilder.DropTable(
                name: "LoginMethods");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropTable(
                name: "PaymentMethodAuditLogs");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "PerformanceMetrics");

            migrationBuilder.DropTable(
                name: "ReviewVotes");

            migrationBuilder.DropTable(
                name: "RoleClaims");

            migrationBuilder.DropTable(
                name: "SecurityEvents");

            migrationBuilder.DropTable(
                name: "SeoAudits");

            migrationBuilder.DropTable(
                name: "SeoSettings");

            migrationBuilder.DropTable(
                name: "SettingChangeLogs");

            migrationBuilder.DropTable(
                name: "Sitemaps");

            migrationBuilder.DropTable(
                name: "UserClaims");

            migrationBuilder.DropTable(
                name: "UserLogins");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "ValidationErrorLogs");

            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "DiscussionComments");

            migrationBuilder.DropTable(
                name: "Coupons");

            migrationBuilder.DropTable(
                name: "PaymentMethods");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "ApplicationSettings");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Discussions");

            migrationBuilder.DropTable(
                name: "Lessons");

            migrationBuilder.DropTable(
                name: "Sections");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
