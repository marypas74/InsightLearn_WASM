using Xunit;
using Moq;
using FluentAssertions;
using InsightLearn.Application.Services;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Tests.Unit.Services;

/// <summary>
/// Unit tests for AuthService
/// Professional test suite with Arrange-Act-Assert pattern
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<SignInManager<User>> _mockSignInManager;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // Arrange - Setup mocks
        var userStoreMock = new Mock<IUserStore<User>>();
        _mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
        _mockSignInManager = new Mock<SignInManager<User>>(
            _mockUserManager.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            null, null, null, null);

        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<AuthService>>();

        // Setup configuration values
        _mockConfiguration.Setup(x => x["Jwt:Secret"])
            .Returns("YourSecretKeyForJWTTokenGeneration12345678");
        _mockConfiguration.Setup(x => x["Jwt:Issuer"])
            .Returns("InsightLearn");
        _mockConfiguration.Setup(x => x["Jwt:Audience"])
            .Returns("InsightLearn");

        _authService = new AuthService(
            _mockUserManager.Object,
            _mockSignInManager.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RegisterUserAsync_WithValidData_ShouldSucceed()
    {
        // Arrange
        var email = "test@example.com";
        var password = "Test123!";
        var fullName = "Test User";

        _mockUserManager
            .Setup(x => x.CreateAsync(It.IsAny<User>(), password))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager
            .Setup(x => x.AddToRoleAsync(It.IsAny<User>(), "Student"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authService.RegisterUserAsync(email, password, fullName);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        _mockUserManager.Verify(
            x => x.CreateAsync(It.Is<User>(u => u.Email == email), password),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RegisterUserAsync_WithInvalidEmail_ShouldFail()
    {
        // Arrange
        var invalidEmail = "invalid-email";
        var password = "Test123!";
        var fullName = "Test User";

        var errors = new[]
        {
            new IdentityError { Code = "InvalidEmail", Description = "Email is invalid." }
        };

        _mockUserManager
            .Setup(x => x.CreateAsync(It.IsAny<User>(), password))
            .ReturnsAsync(IdentityResult.Failed(errors));

        // Act
        var result = await _authService.RegisterUserAsync(invalidEmail, password, fullName);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "InvalidEmail");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task RegisterUserAsync_WithEmptyEmail_ShouldThrowArgumentException(string email)
    {
        // Arrange
        var password = "Test123!";
        var fullName = "Test User";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.RegisterUserAsync(email, password, fullName));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task LoginUserAsync_WithValidCredentials_ShouldSucceed()
    {
        // Arrange
        var email = "test@example.com";
        var password = "Test123!";

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            FullName = "Test User"
        };

        _mockUserManager
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _mockSignInManager
            .Setup(x => x.CheckPasswordSignInAsync(user, password, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        _mockUserManager
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Student" });

        // Act
        var result = await _authService.LoginUserAsync(email, password);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
        _mockSignInManager.Verify(
            x => x.CheckPasswordSignInAsync(user, password, false),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task LoginUserAsync_WithInvalidPassword_ShouldFail()
    {
        // Arrange
        var email = "test@example.com";
        var password = "WrongPassword";

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email
        };

        _mockUserManager
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _mockSignInManager
            .Setup(x => x.CheckPasswordSignInAsync(user, password, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var result = await _authService.LoginUserAsync(email, password);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
        result.Token.Should().BeNullOrEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task LoginUserAsync_WithNonExistentUser_ShouldFail()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var password = "Test123!";

        _mockUserManager
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginUserAsync(email, password);

        // Assert
        result.Should().NotBeNull();
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetUserByIdAsync_WithValidId_ShouldReturnUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FullName = "Test User"
        };

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.GetUserByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetUserByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.GetUserByIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }
}
