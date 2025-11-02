using Bogus;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using InsightLearn.Core.Entities;
using InsightLearn.Infrastructure.Data;
using InsightLearn.Application.DTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace InsightLearn.Tests.Data;

public class TestDataGenerator : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly Faker _faker;

    public TestDataGenerator()
    {
        // Set up configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", optional: false)
            .Build();

        // Set up services
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

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
        _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        // Initialize Faker
        _faker = new Faker();
    }

    public async Task<List<RegisterDto>> GenerateTestRegistrationData(int count = 10)
    {
        var testData = new List<RegisterDto>();

        var registerDtoFaker = new Faker<RegisterDto>()
            .RuleFor(r => r.Email, f => f.Internet.Email())
            .RuleFor(r => r.FirstName, f => f.Name.FirstName())
            .RuleFor(r => r.LastName, f => f.Name.LastName())
            .RuleFor(r => r.Password, f => GenerateSecurePassword())
            .RuleFor(r => r.IsInstructor, f => f.Random.Bool(0.2f)); // 20% chance of being instructor

        testData = registerDtoFaker.Generate(count);

        return testData;
    }

    public async Task<List<ComprehensiveRegisterDto>> GenerateComprehensiveTestData(int count = 5)
    {
        var testData = new List<ComprehensiveRegisterDto>();

        var comprehensiveFaker = new Faker<ComprehensiveRegisterDto>()
            .RuleFor(r => r.Email, f => f.Internet.Email())
            .RuleFor(r => r.FirstName, f => f.Name.FirstName())
            .RuleFor(r => r.LastName, f => f.Name.LastName())
            .RuleFor(r => r.Password, f => GenerateSecurePassword())
            .RuleFor(r => r.ConfirmPassword, (f, r) => r.Password)
            .RuleFor(r => r.DateOfBirth, f => f.Date.Past(50, DateTime.Now.AddYears(-18))) // Adults only
            .RuleFor(r => r.Country, f => f.Address.Country())
            .RuleFor(r => r.PhoneNumber, f => f.Phone.PhoneNumber())
            .RuleFor(r => r.Gender, f => f.PickRandom("Male", "Female", "Other", "Prefer not to say"))
            .RuleFor(r => r.UserType, f => f.PickRandom("Student", "Teacher"))
            .RuleFor(r => r.PreferredPaymentMethod, f => f.PickRandom("CreditCard", "PayPal", "BankTransfer"))
            .RuleFor(r => r.AgreeToTerms, f => true)
            .RuleFor(r => r.AgreeToPrivacyPolicy, f => true);

        testData = comprehensiveFaker.Generate(count);

        return testData;
    }

    public async Task<TestRegistrationResult> CreateTestUsersInDatabase(int studentCount = 5, int instructorCount = 2)
    {
        var result = new TestRegistrationResult();

        try
        {
            // Generate students
            for (int i = 0; i < studentCount; i++)
            {
                var student = GenerateTestUser(false);
                var createResult = await _userManager.CreateAsync(student, "TestPassword123!");

                if (createResult.Succeeded)
                {
                    await _userManager.AddToRoleAsync(student, "Student");
                    result.CreatedUsers.Add(student);
                    result.SuccessfulCreations++;
                }
                else
                {
                    result.FailedCreations++;
                    result.Errors.AddRange(createResult.Errors.Select(e => e.Description));
                }
            }

            // Generate instructors
            for (int i = 0; i < instructorCount; i++)
            {
                var instructor = GenerateTestUser(true);
                var createResult = await _userManager.CreateAsync(instructor, "TestPassword123!");

                if (createResult.Succeeded)
                {
                    await _userManager.AddToRoleAsync(instructor, "Instructor");
                    result.CreatedUsers.Add(instructor);
                    result.SuccessfulCreations++;
                }
                else
                {
                    result.FailedCreations++;
                    result.Errors.AddRange(createResult.Errors.Select(e => e.Description));
                }
            }

            result.IsSuccess = result.FailedCreations == 0;
            return result;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.Errors.Add($"Exception: {ex.Message}");
            return result;
        }
    }

    public async Task<List<InvalidRegistrationTestCase>> GenerateInvalidTestCases()
    {
        return new List<InvalidRegistrationTestCase>
        {
            // Email validation test cases
            new InvalidRegistrationTestCase
            {
                Description = "Empty email",
                RegisterDto = new RegisterDto
                {
                    Email = "",
                    Password = "TestPassword123!",
                    FirstName = "Test",
                    LastName = "User"
                },
                ExpectedError = "Email is required"
            },
            new InvalidRegistrationTestCase
            {
                Description = "Invalid email format",
                RegisterDto = new RegisterDto
                {
                    Email = "invalid-email",
                    Password = "TestPassword123!",
                    FirstName = "Test",
                    LastName = "User"
                },
                ExpectedError = "valid email"
            },

            // Password validation test cases
            new InvalidRegistrationTestCase
            {
                Description = "Too short password",
                RegisterDto = new RegisterDto
                {
                    Email = "test@example.com",
                    Password = "short",
                    FirstName = "Test",
                    LastName = "User"
                },
                ExpectedError = "Password must be at least"
            },
            new InvalidRegistrationTestCase
            {
                Description = "Password without uppercase",
                RegisterDto = new RegisterDto
                {
                    Email = "test@example.com",
                    Password = "nouppercase123!",
                    FirstName = "Test",
                    LastName = "User"
                },
                ExpectedError = "uppercase"
            },
            new InvalidRegistrationTestCase
            {
                Description = "Password without lowercase",
                RegisterDto = new RegisterDto
                {
                    Email = "test@example.com",
                    Password = "NOLOWERCASE123!",
                    FirstName = "Test",
                    LastName = "User"
                },
                ExpectedError = "lowercase"
            },
            new InvalidRegistrationTestCase
            {
                Description = "Password without digit",
                RegisterDto = new RegisterDto
                {
                    Email = "test@example.com",
                    Password = "NoDigitsPassword!",
                    FirstName = "Test",
                    LastName = "User"
                },
                ExpectedError = "digit"
            },

            // Name validation test cases
            new InvalidRegistrationTestCase
            {
                Description = "Empty first name",
                RegisterDto = new RegisterDto
                {
                    Email = "test@example.com",
                    Password = "TestPassword123!",
                    FirstName = "",
                    LastName = "User"
                },
                ExpectedError = "First name is required"
            },
            new InvalidRegistrationTestCase
            {
                Description = "Empty last name",
                RegisterDto = new RegisterDto
                {
                    Email = "test@example.com",
                    Password = "TestPassword123!",
                    FirstName = "Test",
                    LastName = ""
                },
                ExpectedError = "Last name is required"
            }
        };
    }

    public async Task<SecurityTestData> GenerateSecurityTestData()
    {
        return new SecurityTestData
        {
            SqlInjectionPayloads = new[]
            {
                "' OR '1'='1",
                "'; DROP TABLE AspNetUsers; --",
                "admin'--",
                "' UNION SELECT * FROM AspNetUsers --",
                "1' OR '1'='1' --",
                "' OR 1=1 #"
            },
            XssPayloads = new[]
            {
                "<script>alert('xss')</script>",
                "<img src=x onerror=alert('xss')>",
                "javascript:alert('xss')",
                "<svg onload=alert('xss')>",
                "\"><script>alert('xss')</script>",
                "<iframe src=javascript:alert('xss')></iframe>"
            },
            PathTraversalPayloads = new[]
            {
                "../../../etc/passwd",
                "..\\..\\..\\windows\\system32",
                "file://C:/windows/system.ini",
                "//server/share/file",
                "....//....//....//etc//passwd",
                "%2e%2e%2f%2e%2e%2f%2e%2e%2fetc%2fpasswd"
            },
            WeakPasswords = new[]
            {
                "password",
                "12345678",
                "abcdefgh",
                "ABCDEFGH",
                "Password",
                "Pass123"
            },
            StrongPasswords = new[]
            {
                "StrongPassword123!",
                "MySecure@Pass2024",
                "Complex#Password99",
                "SafeAndSecure$123"
            }
        };
    }

    public async Task<PerformanceTestData> GeneratePerformanceTestData(int userCount = 100)
    {
        var data = new PerformanceTestData
        {
            UserCount = userCount,
            RegistrationData = await GenerateTestRegistrationData(userCount)
        };

        // Add some concurrent registration test scenarios
        data.ConcurrentBatches = new List<List<RegisterDto>>();

        // Create 10 batches of 10 users each for concurrent testing
        for (int batch = 0; batch < 10; batch++)
        {
            var batchData = await GenerateTestRegistrationData(10);
            data.ConcurrentBatches.Add(batchData);
        }

        return data;
    }

    public async Task CleanupTestData()
    {
        try
        {
            // Remove test users (users with email containing "test" or "example.com")
            var testUsers = await _context.Users
                .Where(u => u.Email.Contains("test") || u.Email.Contains("example.com"))
                .ToListAsync();

            foreach (var user in testUsers)
            {
                await _userManager.DeleteAsync(user);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log error but don't throw - cleanup is best effort
            Console.WriteLine($"Error during cleanup: {ex.Message}");
        }
    }

    private User GenerateTestUser(bool isInstructor = false)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = _faker.Internet.Email(),
            Email = _faker.Internet.Email(),
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            EmailConfirmed = true,
            IsVerified = true,
            IsInstructor = isInstructor,
            DateJoined = _faker.Date.Between(DateTime.Now.AddYears(-1), DateTime.Now),
            Country = _faker.Address.Country(),
            PhoneNumber = _faker.Phone.PhoneNumber(),
            Gender = _faker.PickRandom("Male", "Female", "Other", "Prefer not to say")
        };
    }

    private string GenerateSecurePassword()
    {
        var password = _faker.Internet.Password(
            length: 12,
            memorable: false,
            regexPattern: @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$"
        );

        // Ensure it meets our requirements
        if (!password.Any(char.IsUpper))
            password = password.Insert(0, "A");
        if (!password.Any(char.IsLower))
            password = password.Insert(0, "a");
        if (!password.Any(char.IsDigit))
            password = password.Insert(0, "1");
        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            password = password.Insert(0, "!");

        return password;
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _serviceProvider?.Dispose();
    }
}

// Data classes for test scenarios
public class TestRegistrationResult
{
    public bool IsSuccess { get; set; }
    public int SuccessfulCreations { get; set; }
    public int FailedCreations { get; set; }
    public List<User> CreatedUsers { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class InvalidRegistrationTestCase
{
    public string Description { get; set; } = string.Empty;
    public RegisterDto RegisterDto { get; set; } = new();
    public string ExpectedError { get; set; } = string.Empty;
}

public class SecurityTestData
{
    public string[] SqlInjectionPayloads { get; set; } = Array.Empty<string>();
    public string[] XssPayloads { get; set; } = Array.Empty<string>();
    public string[] PathTraversalPayloads { get; set; } = Array.Empty<string>();
    public string[] WeakPasswords { get; set; } = Array.Empty<string>();
    public string[] StrongPasswords { get; set; } = Array.Empty<string>();
}

public class PerformanceTestData
{
    public int UserCount { get; set; }
    public List<RegisterDto> RegistrationData { get; set; } = new();
    public List<List<RegisterDto>> ConcurrentBatches { get; set; } = new();
}

// Comprehensive registration DTO for testing
public class ComprehensiveRegisterDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Country { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string UserType { get; set; } = "Student";
    public string PreferredPaymentMethod { get; set; } = string.Empty;
    public bool AgreeToTerms { get; set; }
    public bool AgreeToPrivacyPolicy { get; set; }
}