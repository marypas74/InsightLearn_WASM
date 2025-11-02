using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using InsightLearn.Infrastructure.Data;
using InsightLearn.Core.Entities;
using Microsoft.AspNetCore.Identity;
using System.Data;

namespace InsightLearn.Tests.Database;

public class DatabaseConnectionTests : IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;

    public DatabaseConnectionTests()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", optional: false);

        _configuration = builder.Build();
        _connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(_connectionString));

        services.AddIdentity<User, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
    }

    [Fact]
    public async Task CanConnectToDatabase()
    {
        // Arrange & Act
        using var connection = new SqlConnection(_connectionString);

        // Assert
        var openTask = connection.OpenAsync();
        await openTask;

        connection.State.Should().Be(ConnectionState.Open);

        // Verify server information
        var serverVersion = connection.ServerVersion;
        serverVersion.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CanQueryUsersTable()
    {
        // Arrange
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Act
        using var command = new SqlCommand("SELECT COUNT(*) FROM AspNetUsers", connection);
        var count = await command.ExecuteScalarAsync();

        // Assert
        count.Should().NotBeNull();
        ((int)count).Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task CanVerifyUserTableStructure()
    {
        // Arrange
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Act - Check if all required columns exist
        var requiredColumns = new[]
        {
            "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
            "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
            "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnd",
            "LockoutEnabled", "AccessFailedCount", "FirstName", "LastName",
            "DateJoined", "LastLoginDate", "IsInstructor", "IsVerified", "WalletBalance"
        };

        foreach (var column in requiredColumns)
        {
            var sql = @"
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = 'AspNetUsers'
                AND COLUMN_NAME = @ColumnName";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ColumnName", column);
            var exists = (int)await command.ExecuteScalarAsync() > 0;

            // Assert
            exists.Should().BeTrue($"Column {column} should exist in AspNetUsers table");
        }
    }

    [Fact]
    public async Task CanVerifyRolesTableExists()
    {
        // Arrange
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Act
        var sql = @"
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_NAME = 'AspNetRoles'";

        using var command = new SqlCommand(sql, connection);
        var exists = (int)await command.ExecuteScalarAsync() > 0;

        // Assert
        exists.Should().BeTrue("AspNetRoles table should exist");
    }

    [Fact]
    public async Task CanVerifyUserRolesTableExists()
    {
        // Arrange
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Act
        var sql = @"
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_NAME = 'AspNetUserRoles'";

        using var command = new SqlCommand(sql, connection);
        var exists = (int)await command.ExecuteScalarAsync() > 0;

        // Assert
        exists.Should().BeTrue("AspNetUserRoles table should exist");
    }

    [Fact]
    public async Task CanCreateAndRetrieveTestUser()
    {
        // Arrange
        var context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var testUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser@database.test",
            Email = "testuser@database.test",
            FirstName = "Database",
            LastName = "Test",
            EmailConfirmed = true,
            IsVerified = true,
            DateJoined = DateTime.UtcNow
        };

        try
        {
            // Act - Create user
            var createResult = await userManager.CreateAsync(testUser, "TestPassword123!");
            createResult.Should().Be(IdentityResult.Success);

            // Act - Retrieve user
            var retrievedUser = await userManager.FindByEmailAsync(testUser.Email);

            // Assert
            retrievedUser.Should().NotBeNull();
            retrievedUser.Email.Should().Be(testUser.Email);
            retrievedUser.FirstName.Should().Be(testUser.FirstName);
            retrievedUser.LastName.Should().Be(testUser.LastName);
            retrievedUser.EmailConfirmed.Should().BeTrue();
            retrievedUser.IsVerified.Should().BeTrue();

            // Verify password
            var passwordValid = await userManager.CheckPasswordAsync(retrievedUser, "TestPassword123!");
            passwordValid.Should().BeTrue();
        }
        finally
        {
            // Cleanup - Remove test user
            var userToDelete = await userManager.FindByEmailAsync(testUser.Email);
            if (userToDelete != null)
            {
                await userManager.DeleteAsync(userToDelete);
            }
        }
    }

    [Fact]
    public async Task CanVerifyPasswordHashing()
    {
        // Arrange
        var userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "hashingtestuser@database.test",
            Email = "hashingtestuser@database.test",
            FirstName = "Hashing",
            LastName = "Test"
        };

        try
        {
            // Act - Create user with password
            var password = "TestPassword123!";
            var createResult = await userManager.CreateAsync(testUser, password);
            createResult.Should().Be(IdentityResult.Success);

            // Retrieve the created user
            var createdUser = await userManager.FindByEmailAsync(testUser.Email);
            createdUser.Should().NotBeNull();

            // Assert - Password should be hashed, not stored in plain text
            createdUser.PasswordHash.Should().NotBeNullOrEmpty();
            createdUser.PasswordHash.Should().NotBe(password);
            createdUser.PasswordHash.Length.Should().BeGreaterThan(50); // Hashed passwords are long

            // Verify password validation works
            var isValidPassword = await userManager.CheckPasswordAsync(createdUser, password);
            isValidPassword.Should().BeTrue();

            var isInvalidPassword = await userManager.CheckPasswordAsync(createdUser, "WrongPassword");
            isInvalidPassword.Should().BeFalse();
        }
        finally
        {
            // Cleanup
            var userToDelete = await userManager.FindByEmailAsync(testUser.Email);
            if (userToDelete != null)
            {
                await userManager.DeleteAsync(userToDelete);
            }
        }
    }

    [Fact]
    public async Task CanVerifyUserRoleAssignment()
    {
        // Arrange
        var userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        var testUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "roletestuser@database.test",
            Email = "roletestuser@database.test",
            FirstName = "Role",
            LastName = "Test"
        };

        try
        {
            // Ensure Student role exists
            if (!await roleManager.RoleExistsAsync("Student"))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>("Student"));
            }

            // Act - Create user and assign role
            var createResult = await userManager.CreateAsync(testUser, "TestPassword123!");
            createResult.Should().Be(IdentityResult.Success);

            var addRoleResult = await userManager.AddToRoleAsync(testUser, "Student");
            addRoleResult.Should().Be(IdentityResult.Success);

            // Assert - Verify role assignment
            var userRoles = await userManager.GetRolesAsync(testUser);
            userRoles.Should().Contain("Student");

            var isInRole = await userManager.IsInRoleAsync(testUser, "Student");
            isInRole.Should().BeTrue();
        }
        finally
        {
            // Cleanup
            var userToDelete = await userManager.FindByEmailAsync(testUser.Email);
            if (userToDelete != null)
            {
                await userManager.DeleteAsync(userToDelete);
            }
        }
    }

    [Fact]
    public async Task CanVerifyUserSessionsTableExists()
    {
        // Arrange
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Act
        var sql = @"
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_NAME = 'UserSessions'";

        using var command = new SqlCommand(sql, connection);
        var exists = (int)await command.ExecuteScalarAsync() > 0;

        // Assert
        exists.Should().BeTrue("UserSessions table should exist");
    }

    [Fact]
    public async Task CanQueryRegistrationData()
    {
        // Arrange
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Act - Query for recently registered users (last 24 hours)
        var sql = @"
            SELECT
                Id, Email, FirstName, LastName, DateJoined,
                EmailConfirmed, IsVerified, IsInstructor
            FROM AspNetUsers
            WHERE DateJoined >= DATEADD(day, -1, GETUTCDATE())
            ORDER BY DateJoined DESC";

        using var command = new SqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        var users = new List<dynamic>();
        while (await reader.ReadAsync())
        {
            users.Add(new
            {
                Id = reader["Id"],
                Email = reader["Email"],
                FirstName = reader["FirstName"],
                LastName = reader["LastName"],
                DateJoined = reader["DateJoined"],
                EmailConfirmed = reader["EmailConfirmed"],
                IsVerified = reader["IsVerified"],
                IsInstructor = reader["IsInstructor"]
            });
        }

        // Assert - Should be able to retrieve registration data
        users.Should().NotBeNull();
        // Note: users list might be empty if no recent registrations, but query should succeed
    }

    [Fact]
    public async Task CanVerifyDatabasePerformance()
    {
        // Arrange
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Act - Test query performance
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var sql = "SELECT COUNT(*) FROM AspNetUsers";
        using var command = new SqlCommand(sql, connection);
        await command.ExecuteScalarAsync();

        stopwatch.Stop();

        // Assert - Query should complete within reasonable time
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "Database queries should be performant");
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _serviceProvider?.Dispose();
    }
}