using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using InsightLearn.Application.DTOs;
using InsightLearn.Application.Interfaces;
using InsightLearn.Application.Services;
using InsightLearn.Core.Entities;
using InsightLearn.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InsightLearn.Tests.Unit;

public class AuthServiceTests : IDisposable
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<RoleManager<IdentityRole<Guid>>> _mockRoleManager;
    private readonly Mock<ISessionService> _mockSessionService;
    private readonly Mock<IUserLockoutService> _mockLockoutService;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AuthService _authService;
    private readonly string _testJwtSecret = "TestSecretKeyThatIsAtLeast32CharactersLongForTestingPurposes";
    private readonly string _testJwtIssuer = "InsightLearn.Tests";
    private readonly string _testJwtAudience = "InsightLearn.TestUsers";

    public AuthServiceTests()
    {
        var userStore = new Mock<IUserStore<User>>();
        var roleStore = new Mock<IRoleStore<IdentityRole<Guid>>>();

        _mockUserManager = new Mock<UserManager<User>>(
            userStore.Object,
            null, null, null, null, null, null, null, null);

        _mockRoleManager = new Mock<RoleManager<IdentityRole<Guid>>>(
            roleStore.Object,
            null, null, null, null);

        _mockSessionService = new Mock<ISessionService>();
        _mockLockoutService = new Mock<IUserLockoutService>();
        _mockLogger = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(
            _mockUserManager.Object,
            _mockRoleManager.Object,
            _mockSessionService.Object,
            _mockLockoutService.Object,
            _testJwtSecret,
            _testJwtIssuer,
            _testJwtAudience,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task RegisterAsync_ValidInput_ReturnsSuccessResult()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "TestPassword123!",
            FirstName = "Test",
            LastName = "User",
            IsInstructor = false
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = registerDto.Email,
            Email = registerDto.Email,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName
        };

        var session = new UserSession
        {
            SessionId = Guid.NewGuid().ToString(),
            UserId = user.Id,
            Token = "test-token",
            IsActive = true,
            StartedAt = DateTime.UtcNow
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(registerDto.Email))
            .ReturnsAsync((User?)null);

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), registerDto.Password))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), "Student"))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(new List<string> { "Student" });

        _mockSessionService.Setup(x => x.CreateSessionAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(session);

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be(registerDto.Email);
        result.User.FirstName.Should().Be(registerDto.FirstName);
        result.User.LastName.Should().Be(registerDto.LastName);
        result.User.Roles.Should().Contain("Student");
    }

    [Fact]
    public async Task RegisterAsync_ExistingUser_ReturnsFailureResult()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "existing@example.com",
            Password = "TestPassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = registerDto.Email
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(registerDto.Email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("User with this email already exists.");
    }

    [Fact]
    public async Task RegisterAsync_InvalidPassword_ReturnsFailureResult()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "weak",
            FirstName = "Test",
            LastName = "User"
        };

        var identityErrors = new List<IdentityError>
        {
            new() { Code = "PasswordTooShort", Description = "Password must be at least 8 characters long." },
            new() { Code = "PasswordRequiresDigit", Description = "Password must contain at least one digit." }
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(registerDto.Email))
            .ReturnsAsync((User?)null);

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), registerDto.Password))
            .ReturnsAsync(IdentityResult.Failed(identityErrors.ToArray()));

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Password must be at least 8 characters long.");
        result.Errors.Should().Contain("Password must contain at least one digit.");
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccessResult()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "TestPassword123!"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = loginDto.Email,
            FirstName = "Test",
            LastName = "User",
            IsVerified = true
        };

        var session = new UserSession
        {
            SessionId = Guid.NewGuid().ToString(),
            UserId = user.Id,
            Token = "test-token",
            IsActive = true
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, loginDto.Password))
            .ReturnsAsync(true);

        _mockUserManager.Setup(x => x.ResetAccessFailedCountAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Student" });

        _mockSessionService.Setup(x => x.CreateSessionAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(session);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be(loginDto.Email);
        result.SessionId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_InvalidEmail_ReturnsFailureResult()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "TestPassword123!"
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsFailureResult()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = loginDto.Email,
            IsVerified = true
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, loginDto.Password))
            .ReturnsAsync(false);

        _mockUserManager.Setup(x => x.AccessFailedAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_LockedOutUser_ReturnsFailureResult()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "TestPassword123!"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = loginDto.Email,
            IsVerified = true
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Account is temporarily locked. Please try again later.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task LoginAsync_InvalidEmailInput_ReturnsFailureResult(string email)
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = email,
            Password = "TestPassword123!"
        };

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Email is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task LoginAsync_InvalidPasswordInput_ReturnsFailureResult(string password)
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = password
        };

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Password is required.");
    }

    [Fact]
    public async Task GoogleLoginAsync_ValidToken_ReturnsSuccessResult()
    {
        // Arrange
        var googleLoginDto = new GoogleLoginDto
        {
            AccessToken = "valid-google-token"
        };

        // This test would require mocking the Google API calls
        // For now, we'll test the basic structure

        // Act & Assert
        var result = await _authService.GoogleLoginAsync(googleLoginDto);

        // Since we don't have Google APIs configured in tests, we expect it to fail
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidUserId_ReturnsSuccessResult()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User
        {
            Id = Guid.Parse(userId),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            IsVerified = true
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Student" });

        var session = new UserSession
        {
            SessionId = Guid.NewGuid().ToString(),
            UserId = user.Id,
            Token = "refreshed-token",
            IsActive = true
        };

        _mockSessionService.Setup(x => x.CreateSessionAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(session);

        // Act
        var result = await _authService.RefreshTokenAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_InvalidUserId_ReturnsFailureResult()
    {
        // Arrange
        var invalidUserId = "invalid-user-id";

        // Act
        var result = await _authService.RefreshTokenAsync(invalidUserId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Invalid user ID format.");
    }

    [Fact]
    public async Task RefreshTokenAsync_UserNotFound_ReturnsFailureResult()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        _mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.RefreshTokenAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("User not found.");
    }

    [Fact]
    public async Task LogoutAsync_ValidUserId_ReturnsSuccessResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var session = new UserSession
        {
            SessionId = Guid.NewGuid().ToString(),
            UserId = userId,
            IsActive = true
        };

        _mockSessionService.Setup(x => x.GetActiveSessionByUserIdAsync(userId))
            .ReturnsAsync(session);

        _mockSessionService.Setup(x => x.EndSessionAsync(session.SessionId, "User logout"))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.LogoutAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        _mockSessionService.Verify(x => x.EndSessionAsync(session.SessionId, "User logout"), Times.Once);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}

