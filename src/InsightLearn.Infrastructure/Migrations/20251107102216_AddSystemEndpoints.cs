using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InsightLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemEndpoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatbotContacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    InitialMessage = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MessageCount = table.Column<int>(type: "int", nullable: false),
                    LastInteractionAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsFollowedUp = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatbotContacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatbotMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsUserMessage = table.Column<bool>(type: "bit", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AiModel = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ResponseTimeMs = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatbotMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemEndpoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EndpointKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EndpointPath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemEndpoints", x => x.Id);
                });

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

            migrationBuilder.InsertData(
                table: "SystemEndpoints",
                columns: new[] { "Id", "Category", "Description", "EndpointKey", "EndpointPath", "HttpMethod", "IsActive", "LastModified", "ModifiedBy" },
                values: new object[,]
                {
                    { 1, "Auth", "User login", "Login", "api/auth/login", "POST", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3052), null },
                    { 2, "Auth", "User registration", "Register", "api/auth/register", "POST", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3060), null },
                    { 3, "Auth", "Complete user registration", "CompleteRegistration", "api/auth/complete-registration", "POST", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3061), null },
                    { 4, "Auth", "Refresh JWT token", "Refresh", "api/auth/refresh", "POST", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3063), null },
                    { 5, "Auth", "Get current user", "Me", "api/auth/me", "GET", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3064), null },
                    { 6, "Auth", "OAuth callback", "OAuthCallback", "api/auth/oauth-callback", "POST", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3065), null },
                    { 10, "Courses", "Get all courses", "GetAll", "api/courses", "GET", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3067), null },
                    { 11, "Courses", "Get course by ID", "GetById", "api/courses/{0}", "GET", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3068), null },
                    { 12, "Courses", "Create new course", "Create", "api/courses", "POST", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3069), null },
                    { 13, "Courses", "Update course", "Update", "api/courses/{0}", "PUT", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3071), null },
                    { 14, "Courses", "Delete course", "Delete", "api/courses/{0}", "DELETE", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3072), null },
                    { 15, "Courses", "Search courses", "Search", "api/courses/search", "GET", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3073), null },
                    { 16, "Courses", "Get courses by category", "GetByCategory", "api/courses/category/{0}", "GET", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3074), null },
                    { 20, "Categories", "Get all categories", "GetAll", "api/categories", "GET", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3075), null },
                    { 21, "Categories", "Get category by ID", "GetById", "api/categories/{0}", "GET", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3076), null },
                    { 22, "Categories", "Create new category", "Create", "api/categories", "POST", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3077), null },
                    { 23, "Categories", "Update category", "Update", "api/categories/{0}", "PUT", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3078), null },
                    { 24, "Categories", "Delete category", "Delete", "api/categories/{0}", "DELETE", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3080), null },
                    { 30, "Enrollments", "Get all enrollments", "GetAll", "api/enrollments", "GET", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3081), null },
                    { 31, "Enrollments", "Get enrollment by ID", "GetById", "api/enrollments/{0}", "GET", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3082), null },
                    { 32, "Enrollments", "Create enrollment", "Create", "api/enrollments", "POST", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3083), null },
                    { 33, "Enrollments", "Get enrollments by course", "GetByCourse", "api/enrollments/course/{0}", "GET", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3093), null },
                    { 34, "Enrollments", "Get enrollments by user", "GetByUser", "api/enrollments/user/{0}", "GET", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3112), null },
                    { 40, "Users", "Get all users", "GetAll", "api/users", "GET", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3113), null },
                    { 41, "Users", "Get user by ID", "GetById", "api/users/{0}", "GET", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3114), null },
                    { 42, "Users", "Update user", "Update", "api/users/{0}", "PUT", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3115), null },
                    { 43, "Users", "Delete user", "Delete", "api/users/{0}", "DELETE", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3117), null },
                    { 44, "Users", "Get user profile", "GetProfile", "api/users/profile", "GET", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3118), null },
                    { 50, "Dashboard", "Get dashboard statistics", "GetStats", "api/dashboard/stats", "GET", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3119), null },
                    { 51, "Dashboard", "Get recent activity", "GetRecentActivity", "api/dashboard/recent-activity", "GET", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3120), null },
                    { 60, "Reviews", "Get all reviews", "GetAll", "api/reviews", "GET", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3121), null },
                    { 61, "Reviews", "Get review by ID", "GetById", "api/reviews/{0}", "GET", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3122), null },
                    { 62, "Reviews", "Create review", "Create", "api/reviews", "POST", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3123), null },
                    { 63, "Reviews", "Get reviews by course", "GetByCourse", "api/reviews/course/{0}", "GET", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3125), null },
                    { 70, "Payments", "Create payment checkout", "CreateCheckout", "api/payments/create-checkout", "POST", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3126), null },
                    { 71, "Payments", "Get payment transactions", "GetTransactions", "api/payments/transactions", "GET", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3128), null },
                    { 72, "Payments", "Get transaction by ID", "GetTransactionById", "api/payments/transactions/{0}", "GET", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3130), null },
                    { 80, "Chat", "Send chat message", "SendMessage", "api/chat/message", "POST", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3131), null },
                    { 81, "Chat", "Get chat history", "GetHistory", "api/chat/history", "GET", true, new DateTime(2025, 11, 7, 10, 22, 16, 112, DateTimeKind.Utc).AddTicks(3132), null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatbotContacts");

            migrationBuilder.DropTable(
                name: "ChatbotMessages");

            migrationBuilder.DropTable(
                name: "SystemEndpoints");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 10, 17, 12, 24, 11, 764, DateTimeKind.Utc).AddTicks(6039));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 10, 17, 12, 24, 11, 764, DateTimeKind.Utc).AddTicks(6071));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2025, 10, 17, 12, 24, 11, 764, DateTimeKind.Utc).AddTicks(6076));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAt",
                value: new DateTime(2025, 10, 17, 12, 24, 11, 764, DateTimeKind.Utc).AddTicks(6083));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "CreatedAt",
                value: new DateTime(2025, 10, 17, 12, 24, 11, 764, DateTimeKind.Utc).AddTicks(6089));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "CreatedAt",
                value: new DateTime(2025, 10, 17, 12, 24, 11, 764, DateTimeKind.Utc).AddTicks(6094));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                column: "CreatedAt",
                value: new DateTime(2025, 10, 17, 12, 24, 11, 764, DateTimeKind.Utc).AddTicks(6098));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                column: "CreatedAt",
                value: new DateTime(2025, 10, 17, 12, 24, 11, 764, DateTimeKind.Utc).AddTicks(6103));
        }
    }
}
