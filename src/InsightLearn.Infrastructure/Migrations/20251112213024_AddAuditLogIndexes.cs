using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsImplemented",
                table: "SystemEndpoints",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionId",
                table: "Enrollments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserSubscriptionId",
                table: "Enrollments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSubscriptionOnly",
                table: "Courses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UserRoles = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Referer = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseEngagements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EngagementType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidationScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CountsForPayout = table.Column<bool>(type: "bit", nullable: false),
                    MetaData = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeviceFingerprint = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseEngagements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseEngagements_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseEngagements_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseEngagements_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InstructorConnectAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstructorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StripeAccountId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OnboardingStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PayoutsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ChargesEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    DefaultPayoutMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OnboardingUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OnboardingCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalPaidOut = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastPayoutAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Requirements = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    VerificationStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisabledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DisabledReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstructorConnectAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstructorConnectAccounts_Users_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InstructorPayouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstructorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    TotalEngagementMinutes = table.Column<long>(type: "bigint", nullable: false),
                    PlatformTotalEngagementMinutes = table.Column<long>(type: "bigint", nullable: false),
                    TotalPlatformRevenue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EngagementPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PlatformCommissionRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PayoutAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StripeTransferId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UniqueStudentCount = table.Column<int>(type: "int", nullable: false),
                    ActiveCoursesCount = table.Column<int>(type: "int", nullable: false),
                    CourseEngagementBreakdown = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstructorPayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstructorPayouts_Users_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PriceMonthly = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PriceYearly = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Features = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MaxDevices = table.Column<int>(type: "int", nullable: true),
                    MaxVideoQuality = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AllowOfflineDownload = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    StripeProductId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    StripePriceMonthlyId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    StripePriceYearlyId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BillingInterval = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StripeSubscriptionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    StripeCustomerId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CurrentPeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AutoRenew = table.Column<bool>(type: "bit", nullable: false),
                    TrialEndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CancellationFeedback = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionRevenues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "EUR"),
                    StripeInvoiceId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    StripePaymentIntentId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    BillingPeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BillingPeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RefundReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CardLast4 = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    CardBrand = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InvoiceUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionRevenues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionRevenues_UserSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "UserSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(2325));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(2371));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(2377));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(2384));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(2391));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(2399));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(2404));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(2409));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3034) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3048) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3049) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3050) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3052) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3054) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3055) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3056) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3060) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3109) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3111) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3113) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3115) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3116) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3117) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3119) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 23,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3120) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 24,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3122) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 30,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3123) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 31,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3124) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 32,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3125) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 33,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3136) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 34,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3138) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 40,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3139) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 41,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3140) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 42,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3141) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 43,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3142) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 44,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3143) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 50,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3144) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3146) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 60,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3147) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 61,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3148) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 62,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3149) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 63,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3151) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 70,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3152) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 71,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3153) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 72,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3155) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 80,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3156) });

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 81,
                columns: new[] { "IsImplemented", "LastModified" },
                values: new object[] { false, new DateTime(2025, 11, 12, 21, 30, 23, 331, DateTimeKind.Utc).AddTicks(3157) });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_SubscriptionId",
                table: "Enrollments",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_UserSubscriptionId",
                table: "Enrollments",
                column: "UserSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action_Timestamp",
                table: "AuditLogs",
                columns: new[] { "Action", "Timestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityId",
                table: "AuditLogs",
                column: "EntityId",
                filter: "[EntityId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_RequestId",
                table: "AuditLogs",
                column: "RequestId",
                filter: "[RequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_Timestamp",
                table: "AuditLogs",
                columns: new[] { "UserId", "Timestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_CourseEngagements_CountsForPayout_StartedAt",
                table: "CourseEngagements",
                columns: new[] { "CountsForPayout", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseEngagements_CourseId",
                table: "CourseEngagements",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEngagements_LessonId",
                table: "CourseEngagements",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEngagements_UserId_CourseId_StartedAt",
                table: "CourseEngagements",
                columns: new[] { "UserId", "CourseId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InstructorConnectAccounts_InstructorId",
                table: "InstructorConnectAccounts",
                column: "InstructorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstructorConnectAccounts_OnboardingStatus_PayoutsEnabled",
                table: "InstructorConnectAccounts",
                columns: new[] { "OnboardingStatus", "PayoutsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_InstructorConnectAccounts_StripeAccountId",
                table: "InstructorConnectAccounts",
                column: "StripeAccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstructorPayouts_InstructorId_Month_Year",
                table: "InstructorPayouts",
                columns: new[] { "InstructorId", "Month", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstructorPayouts_Status_Month_Year",
                table: "InstructorPayouts",
                columns: new[] { "Status", "Month", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_InstructorPayouts_StripeTransferId",
                table: "InstructorPayouts",
                column: "StripeTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_IsActive_PriceMonthly",
                table: "SubscriptionPlans",
                columns: new[] { "IsActive", "PriceMonthly" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Name",
                table: "SubscriptionPlans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_StripeProductId",
                table: "SubscriptionPlans",
                column: "StripeProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionRevenues_BillingPeriodStart_BillingPeriodEnd",
                table: "SubscriptionRevenues",
                columns: new[] { "BillingPeriodStart", "BillingPeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionRevenues_Status_PaidAt",
                table: "SubscriptionRevenues",
                columns: new[] { "Status", "PaidAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionRevenues_StripeInvoiceId",
                table: "SubscriptionRevenues",
                column: "StripeInvoiceId",
                unique: true,
                filter: "[StripeInvoiceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionRevenues_SubscriptionId",
                table: "SubscriptionRevenues",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_PlanId",
                table: "UserSubscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_Status_CurrentPeriodEnd",
                table: "UserSubscriptions",
                columns: new[] { "Status", "CurrentPeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_StripeSubscriptionId",
                table: "UserSubscriptions",
                column: "StripeSubscriptionId",
                unique: true,
                filter: "[StripeSubscriptionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId",
                table: "UserSubscriptions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_UserSubscriptions_SubscriptionId",
                table: "Enrollments",
                column: "SubscriptionId",
                principalTable: "UserSubscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_UserSubscriptions_UserSubscriptionId",
                table: "Enrollments",
                column: "UserSubscriptionId",
                principalTable: "UserSubscriptions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_UserSubscriptions_SubscriptionId",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_UserSubscriptions_UserSubscriptionId",
                table: "Enrollments");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CourseEngagements");

            migrationBuilder.DropTable(
                name: "InstructorConnectAccounts");

            migrationBuilder.DropTable(
                name: "InstructorPayouts");

            migrationBuilder.DropTable(
                name: "SubscriptionRevenues");

            migrationBuilder.DropTable(
                name: "UserSubscriptions");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_SubscriptionId",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_UserSubscriptionId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "IsImplemented",
                table: "SystemEndpoints");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "UserSubscriptionId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "IsSubscriptionOnly",
                table: "Courses");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(2778));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(2808));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(2814));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(2820));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(2826));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(2831));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(2837));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(2841));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3052));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 2,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3060));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 3,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3061));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 4,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3063));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 5,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3064));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 6,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3065));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 10,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3067));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 11,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3068));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 12,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3069));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 13,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3071));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 14,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3072));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 15,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3073));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 16,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3074));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 20,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3075));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 21,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3076));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 22,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3077));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 23,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3078));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 24,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3080));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 30,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3081));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 31,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3082));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 32,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3083));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 33,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3093));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 34,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3112));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 40,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3113));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 41,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3114));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 42,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3115));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 43,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3117));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 44,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3118));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 50,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3119));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 51,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3120));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 60,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3121));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 61,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3122));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 62,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3123));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 63,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3125));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 70,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3126));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 71,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3128));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 72,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3130));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 80,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3131));

            migrationBuilder.UpdateData(
                table: "SystemEndpoints",
                keyColumn: "Id",
                keyValue: 81,
                column: "LastModified",
                value: new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3132));
        }
    }
}
