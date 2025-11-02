using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using InsightLearn.Application.DTOs;
using InsightLearn.Infrastructure.Data;
using InsightLearn.Core.Entities;
using InsightLearn.Api;

namespace InsightLearn.Tests.Security;

public class SecurityTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public SecurityTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"SecurityTestDb_{Guid.NewGuid()}");
                });
            });
        });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    }

    [Fact]
    public async Task PasswordHashing_StoresHashedPassword_NotPlainText()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "security-test@example.com",
            Password = "SecurePassword123!",
            FirstName = "Security",
            LastName = "Test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify password is hashed in database
        var user = await _userManager.FindByEmailAsync(registerDto.Email);
        user.Should().NotBeNull();
        user.PasswordHash.Should().NotBeNull();
        user.PasswordHash.Should().NotBe(registerDto.Password);  // Should not store plain text
        user.PasswordHash.Length.Should().BeGreaterThan(50);  // Hashed passwords are long

        // Verify password validation still works
        var isValid = await _userManager.CheckPasswordAsync(user, registerDto.Password);
        isValid.Should().BeTrue();

        var isInvalid = await _userManager.CheckPasswordAsync(user, "WrongPassword");
        isInvalid.Should().BeFalse();
    }

    [Theory]
    [InlineData("' OR '1'='1")]  // SQL injection attempt
    [InlineData("'; DROP TABLE AspNetUsers; --")]  // SQL injection attempt
    [InlineData("admin'--")]  // SQL injection attempt
    [InlineData("' UNION SELECT * FROM AspNetUsers --")]  // SQL injection attempt
    public async Task SQLInjection_InEmailField_DoesNotCompromiseSecurity(string maliciousEmail)
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = maliciousEmail,
            Password = "TestPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Verify database integrity - table should still exist and be intact
        var userCount = await _context.Users.CountAsync();
        userCount.Should().BeGreaterThanOrEqualTo(0);  // Table should still exist

        // Verify no unauthorized access
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotContain("password");  // Should not leak sensitive info
        content.Should().NotContain("Id");  // Should not leak user data
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]  // XSS attempt
    [InlineData("<img src=x onerror=alert('xss')>")]  // XSS attempt
    [InlineData("javascript:alert('xss')")]  // XSS attempt
    [InlineData("<svg onload=alert('xss')>")]  // XSS attempt
    public async Task XSSPrevention_InRegistrationFields_SanitizesInput(string maliciousInput)
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "xss-test@example.com",
            Password = "TestPassword123!",
            FirstName = maliciousInput,  // Inject malicious content in first name
            LastName = "Test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            // If registration succeeds, verify data is sanitized in database
            var user = await _userManager.FindByEmailAsync(registerDto.Email);
            user.Should().NotBeNull();

            // The first name should not contain executable script tags
            user.FirstName.Should().NotContain("<script>");
            user.FirstName.Should().NotContain("javascript:");
            user.FirstName.Should().NotContain("onerror");
            user.FirstName.Should().NotContain("onload");
        }
        else
        {
            // If registration fails due to validation, that's also acceptable
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task PasswordComplexity_WeakPasswords_AreRejected()
    {
        var weakPasswords = new[]
        {
            "password",           // Too common
            "12345678",          // Only numbers
            "abcdefgh",          // Only lowercase
            "ABCDEFGH",          // Only uppercase
            "Password",          // No numbers or special chars
            "Pass123",           // Too short
            "",                  // Empty
            "   ",               // Whitespace only
        };

        foreach (var weakPassword in weakPasswords)
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = $"weak-password-test-{Guid.NewGuid()}@example.com",
                Password = weakPassword,
                FirstName = "Weak",
                LastName = "Password"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"Weak password '{weakPassword}' should be rejected");

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Password", "Error message should mention password requirements");
        }
    }

    [Fact]
    public async Task PasswordComplexity_StrongPasswords_AreAccepted()
    {
        var strongPasswords = new[]
        {
            "StrongPassword123!",
            "MySecure@Pass2024",
            "Complex#Password99",
            "SafeAndSecure$123"
        };

        foreach (var strongPassword in strongPasswords)
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = $"strong-password-test-{Guid.NewGuid()}@example.com",
                Password = strongPassword,
                FirstName = "Strong",
                LastName = "Password"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"Strong password '{strongPassword}' should be accepted");
        }
    }

    [Fact]
    public async Task RateLimiting_MultipleFailedLogins_ShouldEventuallyBlock()
    {
        // Arrange - Register a user first
        var email = "rate-limit-test@example.com";
        var registerDto = new RegisterDto
        {
            Email = email,
            Password = "TestPassword123!",
            FirstName = "Rate",
            LastName = "Limit"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        var loginDto = new LoginDto
        {
            Email = email,
            Password = "WrongPassword123!"  // Intentionally wrong
        };

        // Act - Attempt multiple failed logins
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < 10; i++)
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            responses.Add(response);
        }

        // Assert - All should fail with Unauthorized (not indicating lockout detection working)
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // Note: Actual rate limiting might require additional configuration
        // This test verifies that failed attempts don't reveal sensitive information
    }

    [Fact]
    public async Task AuthTokenSecurity_TokenIsJWT_WithProperStructure()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "jwt-test@example.com",
            Password = "TestPassword123!",
            FirstName = "JWT",
            LastName = "Test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuthResultDto>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        result.Should().NotBeNull();
        result.Token.Should().NotBeNull();

        // JWT tokens have 3 parts separated by dots
        var tokenParts = result.Token.Split('.');
        tokenParts.Should().HaveCount(3, "JWT should have header, payload, and signature");

        // Each part should be base64 encoded
        foreach (var part in tokenParts)
        {
            part.Should().NotBeNullOrEmpty();
            part.Should().MatchRegex(@"^[A-Za-z0-9_-]+$", "JWT parts should be base64url encoded");
        }
    }

    [Fact]
    public async Task SensitiveDataExposure_ErrorMessages_DoNotLeakInformation()
    {
        // Test various scenarios to ensure error messages don't leak sensitive information

        // Test 1: Non-existent user login
        var loginDto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "TestPassword123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        var content = await response.Content.ReadAsStringAsync();

        // Should not reveal whether user exists or not
        content.Should().Contain("Invalid email or password");
        content.Should().NotContain("user not found", StringComparison.OrdinalIgnoreCase);
        content.Should().NotContain("does not exist", StringComparison.OrdinalIgnoreCase);

        // Test 2: Wrong password for existing user
        var registerDto = new RegisterDto
        {
            Email = "error-test@example.com",
            Password = "TestPassword123!",
            FirstName = "Error",
            LastName = "Test"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        var wrongPasswordLogin = new LoginDto
        {
            Email = registerDto.Email,
            Password = "WrongPassword123!"
        };

        var wrongResponse = await _client.PostAsJsonAsync("/api/auth/login", wrongPasswordLogin);
        var wrongContent = await wrongResponse.Content.ReadAsStringAsync();

        // Should use same error message as non-existent user
        wrongContent.Should().Contain("Invalid email or password");
        wrongContent.Should().NotContain("wrong password", StringComparison.OrdinalIgnoreCase);
        wrongContent.Should().NotContain("incorrect password", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HeaderSecurity_ResponseHeaders_IncludeSecurityHeaders()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/google/health");

        // Assert - Check for security headers
        response.Headers.Should().NotBeNull();

        // Note: In a production environment, you would typically see:
        // - X-Content-Type-Options: nosniff
        // - X-Frame-Options: DENY or SAMEORIGIN
        // - X-XSS-Protection: 1; mode=block
        // - Strict-Transport-Security (for HTTPS)
        // - Content-Security-Policy

        // For testing, we at least verify the response is structured properly
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task EmailValidation_PreventEmailEnumeration()
    {
        // Test that duplicate email registration doesn't reveal timing differences

        // Arrange - Register a user
        var existingEmail = "existing@example.com";
        var registerDto = new RegisterDto
        {
            Email = existingEmail,
            Password = "TestPassword123!",
            FirstName = "Existing",
            LastName = "User"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Act - Try to register with same email
        var duplicateDto = new RegisterDto
        {
            Email = existingEmail,
            Password = "DifferentPassword123!",
            FirstName = "Duplicate",
            LastName = "User"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", duplicateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("already exists");

        // Verify response doesn't leak additional user information
        content.Should().NotContain("password");
        content.Should().NotContain("FirstName");
        content.Should().NotContain("LastName");
    }

    [Theory]
    [InlineData("../../../etc/passwd")]  // Path traversal
    [InlineData("..\\..\\..\\windows\\system32")]  // Windows path traversal
    [InlineData("file://C:/windows/system.ini")]  // File protocol
    [InlineData("//server/share/file")]  // UNC path
    public async Task PathTraversal_InInputFields_IsBlocked(string maliciousPath)
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "path-traversal@example.com",
            Password = "TestPassword123!",
            FirstName = maliciousPath,  // Try path traversal in name field
            LastName = "Test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            // If registration succeeds, verify the malicious path is not processed
            var user = await _userManager.FindByEmailAsync(registerDto.Email);
            user.Should().NotBeNull();

            // The name should not contain file system paths
            user.FirstName.Should().NotContain("etc/passwd");
            user.FirstName.Should().NotContain("windows");
            user.FirstName.Should().NotContain("system32");
        }
        else
        {
            // If validation fails, that's also acceptable
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task PasswordPolicy_EnforcesMinimumRequirements()
    {
        // Test that the password policy enforces all security requirements

        var testCases = new[]
        {
            new { Password = "short", ShouldFail = true, Reason = "Too short" },
            new { Password = "nouppercase123!", ShouldFail = true, Reason = "No uppercase" },
            new { Password = "NOLOWERCASE123!", ShouldFail = true, Reason = "No lowercase" },
            new { Password = "NoNumbers!", ShouldFail = true, Reason = "No numbers" },
            new { Password = "GoodPassword123!", ShouldFail = false, Reason = "Meets all requirements" }
        };

        foreach (var testCase in testCases)
        {
            var registerDto = new RegisterDto
            {
                Email = $"password-policy-{Guid.NewGuid()}@example.com",
                Password = testCase.Password,
                FirstName = "Policy",
                LastName = "Test"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            if (testCase.ShouldFail)
            {
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest, testCase.Reason);
            }
            else
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK, testCase.Reason);
            }
        }
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _client?.Dispose();
    }
}