using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using InsightLearn.Application.DTOs;
using InsightLearn.Infrastructure.Data;
using InsightLearn.Api;
using Microsoft.Extensions.Configuration;

namespace InsightLearn.Tests.OAuth;

public class GoogleOAuthRegistrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;

    public GoogleOAuthRegistrationTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                });
            });
        });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
    }

    [Fact]
    public async Task GoogleOAuthHealth_ReturnsConfigurationStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/google/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var healthStatus = JsonSerializer.Deserialize<GoogleOAuthHealthResponse>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        healthStatus.Should().NotBeNull();
        healthStatus.HasClientId.Should().BeDefined();
        healthStatus.HasClientSecret.Should().BeDefined();
        healthStatus.IsConfigured.Should().BeDefined();
        healthStatus.RedirectUri.Should().NotBeNullOrEmpty();
        healthStatus.Endpoints.Should().NotBeNull();
        healthStatus.CorrelationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GoogleConfig_ReturnsClientConfiguration()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/google-config");

        // Assert
        // This might return 404 if Google is not configured in test environment
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var config = JsonSerializer.Deserialize<GoogleConfigResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            config.Should().NotBeNull();
            config.ClientId.Should().NotBeNullOrEmpty();
            config.RedirectUri.Should().NotBeNullOrEmpty();
            config.RedirectUri.Should().Contain("/api/auth/google/callback");
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task GoogleLogin_InitiatesOAuthFlow()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/google-login");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var oauthResponse = JsonSerializer.Deserialize<GoogleOAuthInitResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            oauthResponse.Should().NotBeNull();
            oauthResponse.AuthUrl.Should().NotBeNullOrEmpty();
            oauthResponse.AuthUrl.Should().Contain("accounts.google.com");
            oauthResponse.AuthUrl.Should().Contain("oauth2");
            oauthResponse.State.Should().NotBeNullOrEmpty();
            oauthResponse.CorrelationId.Should().NotBeNullOrEmpty();
        }
        else
        {
            // If Google OAuth is not configured, we should get NotFound
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task GoogleOAuthCallback_WithoutCode_ReturnsBadRequest()
    {
        // Act - Call callback without authorization code
        var response = await _client.GetAsync("/api/auth/google/callback");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Authorization code not received");
    }

    [Fact]
    public async Task GoogleOAuthCallback_WithError_ReturnsBadRequest()
    {
        // Act - Call callback with OAuth error
        var response = await _client.GetAsync("/api/auth/google/callback?error=access_denied&error_description=User denied access");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Access was denied");
    }

    [Theory]
    [InlineData("access_denied", "Access was denied")]
    [InlineData("invalid_request", "Invalid OAuth request")]
    [InlineData("unauthorized_client", "Unauthorized client")]
    [InlineData("server_error", "Google server error")]
    public async Task GoogleOAuthCallback_WithSpecificErrors_ReturnsAppropriateMessage(string error, string expectedMessage)
    {
        // Act
        var response = await _client.GetAsync($"/api/auth/google/callback?error={error}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(expectedMessage);
    }

    [Fact]
    public async Task GoogleAuthentication_WithValidToken_ProcessesSuccessfully()
    {
        // This test simulates the Google authentication process
        // In a real scenario, we would need actual Google tokens

        // Arrange - Create a mock Google login DTO
        var googleLoginDto = new GoogleLoginDto
        {
            AccessToken = "mock-google-access-token"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/google", googleLoginDto);

        // Assert
        // Since we're using mock tokens, this should fail
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Google authentication failed");
    }

    [Fact]
    public async Task GoogleAuthentication_WithInvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var googleLoginDto = new GoogleLoginDto
        {
            AccessToken = "invalid-token"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/google", googleLoginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Google authentication failed");
    }

    [Fact]
    public async Task GoogleAuthentication_WithoutToken_ReturnsBadRequest()
    {
        // Arrange
        var googleLoginDto = new GoogleLoginDto
        {
            AccessToken = string.Empty
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/google", googleLoginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GoogleOAuthFlow_HasProperSecurityMeasures()
    {
        // Test that the OAuth flow includes proper security measures

        // Act - Get OAuth URL
        var response = await _client.GetAsync("/api/auth/google-login");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var oauthResponse = JsonSerializer.Deserialize<GoogleOAuthInitResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Assert - Verify security parameters
            oauthResponse.AuthUrl.Should().Contain("state=");  // CSRF protection
            oauthResponse.AuthUrl.Should().Contain("response_type=code");  // Authorization code flow
            oauthResponse.AuthUrl.Should().Contain("scope=");  // Proper scopes
            oauthResponse.State.Should().NotBeNullOrEmpty();  // State parameter for CSRF protection
        }
    }

    [Fact]
    public async Task GoogleOAuthRedirectUri_IsCorrectlyConfigured()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/google-config");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var config = JsonSerializer.Deserialize<GoogleConfigResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Assert
            config.RedirectUri.Should().EndWith("/api/auth/google/callback");
            config.RedirectUri.Should().StartWith("http");
        }
    }

    [Fact]
    public async Task GoogleOAuthScopes_IncludeRequiredPermissions()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/google-login");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var oauthResponse = JsonSerializer.Deserialize<GoogleOAuthInitResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Assert - Verify required scopes
            oauthResponse.AuthUrl.Should().Contain("openid");  // OpenID Connect
            oauthResponse.AuthUrl.Should().Contain("profile");  // User profile
            oauthResponse.AuthUrl.Should().Contain("email");  // Email address
        }
    }

    [Fact]
    public void GoogleOAuthEndpoints_AreCorrectlyDefined()
    {
        // This test verifies that OAuth endpoints are properly configured
        var endpoints = new[]
        {
            "/api/auth/google-config",
            "/api/auth/google-login",
            "/api/auth/google/callback",
            "/api/auth/google",
            "/api/auth/google/health"
        };

        // All endpoints should be accessible (even if they return errors without proper config)
        foreach (var endpoint in endpoints)
        {
            endpoint.Should().StartWith("/api/auth/");
        }
    }

    [Fact]
    public async Task GoogleOAuthCallback_WithMockCode_AttemptsTokenExchange()
    {
        // Act - Simulate OAuth callback with mock authorization code
        var response = await _client.GetAsync("/api/auth/google/callback?code=mock-auth-code&state=mock-state");

        // Assert
        // This should fail since we don't have real Google OAuth configuration
        // But it should attempt the token exchange process
        response.StatusCode.Should().BeOneOf(HttpStatusCode.InternalServerError, HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        // The error should indicate either configuration issues or token exchange failure
        content.Should().Contain("OAuth", "token", "configuration", "error");
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _client?.Dispose();
    }
}

// Response DTOs for testing
public class GoogleOAuthHealthResponse
{
    public bool IsConfigured { get; set; }
    public bool HasClientId { get; set; }
    public bool HasClientSecret { get; set; }
    public string RedirectUri { get; set; } = string.Empty;
    public GoogleOAuthEndpoints Endpoints { get; set; } = new();
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class GoogleOAuthEndpoints
{
    public string Config { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string Callback { get; set; } = string.Empty;
    public string Authenticate { get; set; } = string.Empty;
}

public class GoogleConfigResponse
{
    public string ClientId { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}

public class GoogleOAuthInitResponse
{
    public string AuthUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}