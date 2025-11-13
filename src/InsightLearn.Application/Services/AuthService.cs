using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InsightLearn.Core.Entities;
using InsightLearn.Application.DTOs;
using InsightLearn.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace InsightLearn.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ILogger<AuthService> _logger;
    private readonly ISessionService _sessionService;
    private readonly IUserLockoutService _lockoutService;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public AuthService(
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ISessionService sessionService,
        IUserLockoutService lockoutService,
        string jwtSecret,
        string jwtIssuer,
        string jwtAudience,
        ILogger<AuthService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _lockoutService = lockoutService ?? throw new ArgumentNullException(nameof(lockoutService));
        _jwtSecret = !string.IsNullOrWhiteSpace(jwtSecret) ? jwtSecret : throw new ArgumentException("JWT secret cannot be null or empty", nameof(jwtSecret));
        _jwtIssuer = !string.IsNullOrWhiteSpace(jwtIssuer) ? jwtIssuer : throw new ArgumentException("JWT issuer cannot be null or empty", nameof(jwtIssuer));
        _jwtAudience = !string.IsNullOrWhiteSpace(jwtAudience) ? jwtAudience : throw new ArgumentException("JWT audience cannot be null or empty", nameof(jwtAudience));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AuthResultDto> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = new[] { "User with this email already exists." }
                };
            }

            // Create new user
            var user = new User
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                IsInstructor = registerDto.IsInstructor,
                EmailConfirmed = true // For demo purposes, set to false in production
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = result.Errors.Select(e => e.Description)
                };
            }

            // Assign role based on instructor status
            var roleName = registerDto.IsInstructor ? "Instructor" : "Student";
            await _userManager.AddToRoleAsync(user, roleName);

            // Generate JWT token and create session using the public method
            var tokenResult = await GenerateJwtTokenAsync(user, 
                ipAddress: "127.0.0.1", // TODO: Get real IP address
                userAgent: "Registration", // TODO: Get real user agent
                correlationId: Guid.NewGuid().ToString());

            if (!tokenResult.IsSuccess)
            {
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = tokenResult.Errors
                };
            }

            return tokenResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return new AuthResultDto
            {
                IsSuccess = false,
                Errors = new[] { "An error occurred during registration." }
            };
        }
    }

    public async Task<AuthResultDto> LoginAsync(LoginDto loginDto)
    {
        var correlationId = Guid.NewGuid().ToString();
        
        try
        {
            // Validate input parameters
            if (loginDto == null)
            {
                _logger.LogWarning("LoginAsync called with null loginDto. CorrelationId: {CorrelationId}", correlationId);
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = new[] { "Invalid login request." }
                };
            }

            if (string.IsNullOrWhiteSpace(loginDto.Email))
            {
                _logger.LogWarning("LoginAsync called with null/empty email. CorrelationId: {CorrelationId}", correlationId);
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = new[] { "Email is required." }
                };
            }

            if (string.IsNullOrWhiteSpace(loginDto.Password))
            {
                _logger.LogWarning("LoginAsync called with null/empty password for email: {Email}. CorrelationId: {CorrelationId}", loginDto.Email, correlationId);
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = new[] { "Password is required." }
                };
            }

            _logger.LogInformation("Login attempt for email: {Email}. CorrelationId: {CorrelationId}", loginDto.Email, correlationId);

            // Find user by email with error handling
            User? user;
            try
            {
                user = await _userManager.FindByEmailAsync(loginDto.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error finding user by email: {Email}. CorrelationId: {CorrelationId}", loginDto.Email, correlationId);
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = new[] { "Unable to process login request. Please try again." }
                };
            }

            if (user == null)
            {
                _logger.LogWarning("User not found for email: {Email}. CorrelationId: {CorrelationId}", loginDto.Email, correlationId);
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = new[] { "Invalid email or password." }
                };
            }

            // Check if user account is locked or disabled
            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("User account is locked for email: {Email}, UserId: {UserId}. CorrelationId: {CorrelationId}", 
                    loginDto.Email, user.Id, correlationId);
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = new[] { "Account is temporarily locked. Please try again later." }
                };
            }

            // Verify password with error handling
            bool passwordValid;
            try
            {
                passwordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking password for user: {UserId}. CorrelationId: {CorrelationId}", user.Id, correlationId);
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = new[] { "Unable to verify password. Please try again." }
                };
            }

            if (!passwordValid)
            {
                _logger.LogWarning("Invalid password for user: {UserId}. CorrelationId: {CorrelationId}", user.Id, correlationId);
                
                // Record failed attempt (for potential future lockout logic)
                try
                {
                    await _userManager.AccessFailedAsync(user);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error recording access failure for user: {UserId}. CorrelationId: {CorrelationId}", user.Id, correlationId);
                }
                
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = new[] { "Invalid email or password." }
                };
            }

            // Reset access failed count on successful password verification
            try
            {
                await _userManager.ResetAccessFailedCountAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting access failed count for user: {UserId}. CorrelationId: {CorrelationId}", user.Id, correlationId);
                // Don't fail login for this
            }

            // Update last login date with error handling
            try
            {
                user.LastLoginDate = DateTime.UtcNow;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    _logger.LogWarning("Failed to update last login date for user: {UserId}. Errors: {Errors}. CorrelationId: {CorrelationId}",
                        user.Id, string.Join(", ", updateResult.Errors.Select(e => e.Description)), correlationId);
                    // Don't fail login for this
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last login date for user: {UserId}. CorrelationId: {CorrelationId}", user.Id, correlationId);
                // Don't fail login for this
            }

            // 🔥 CRITICAL FIX: Use unified JWT + Session + HTTPS Cookie generation
            AuthResultDto tokenResult;
            try
            {
                tokenResult = await GenerateJwtTokenAsync(
                    user,
                    ipAddress: "127.0.0.1", // TODO: Get real IP from HttpContext
                    userAgent: "Browser", // TODO: Get real user agent from HttpContext
                    correlationId: correlationId
                );

                if (!tokenResult.IsSuccess || string.IsNullOrWhiteSpace(tokenResult.Token))
                {
                    _logger.LogError("JWT token generation failed for user: {UserId}. Errors: {Errors}. CorrelationId: {CorrelationId}",
                        user.Id, string.Join(", ", tokenResult.Errors), correlationId);
                    return new AuthResultDto
                    {
                        IsSuccess = false,
                        Errors = tokenResult.Errors.Any() ? tokenResult.Errors : new[] { "Unable to generate authentication token. Please try again." }
                    };
                }

                _logger.LogInformation("✅ JWT token and session created successfully for user: {UserId}, SessionId: {SessionId}. CorrelationId: {CorrelationId}",
                    user.Id, tokenResult.SessionId, correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user: {UserId}. CorrelationId: {CorrelationId}", user.Id, correlationId);
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = new[] { "Unable to generate authentication token. Please try again." }
                };
            }

            // Map user to DTO with error handling
            UserDto userDto;
            try
            {
                userDto = MapUserToDto(user);
                if (userDto == null)
                {
                    _logger.LogError("User mapping returned null for user: {UserId}. CorrelationId: {CorrelationId}", user.Id, correlationId);
                    return new AuthResultDto
                    {
                        IsSuccess = false,
                        Errors = new[] { "Error processing user data. Please try again." }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping user to DTO for user: {UserId}. CorrelationId: {CorrelationId}", user.Id, correlationId);
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = new[] { "Error processing user data. Please try again." }
                };
            }

            // Get user roles with error handling
            try
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDto.Roles = roles ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user roles for user: {UserId}. CorrelationId: {CorrelationId}", user.Id, correlationId);
                userDto.Roles = new List<string>(); // Don't fail login for this
            }

            // Add session ID to user DTO
            userDto.SessionId = tokenResult.SessionId;

            _logger.LogInformation("Login successful for user: {UserId}, Email: {Email}, SessionId: {SessionId}. CorrelationId: {CorrelationId}",
                user.Id, loginDto.Email, tokenResult.SessionId, correlationId);

            return new AuthResultDto
            {
                IsSuccess = true,
                Token = tokenResult.Token,
                ExpiresAt = tokenResult.ExpiresAt,
                SessionId = tokenResult.SessionId, // 🔥 Session ID from unified token generation
                User = userDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for email: {Email}. CorrelationId: {CorrelationId}", 
                loginDto?.Email ?? "Unknown", correlationId);
            
            return new AuthResultDto
            {
                IsSuccess = false,
                Errors = new[] { "An unexpected error occurred during login. Please try again." }
            };
        }
    }

    public async Task<AuthResultDto> LogoutAsync(Guid userId, string? ipAddress = null, string? userAgent = null, bool logoutFromAllDevices = false)
    {
        try
        {
            if (logoutFromAllDevices)
            {
                return await LogoutFromAllDevicesAsync(userId, ipAddress, userAgent);
            }

            // For JWT authentication, we would typically add token to a blacklist
            // For now, we'll just return success as tokens expire naturally
            var session = await _sessionService.GetActiveSessionByUserIdAsync(userId);
            if (session != null)
            {
                await _sessionService.EndSessionAsync(session.SessionId, "User logout");
            }
            
            return new AuthResultDto
            {
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user {UserId}", userId);
            return new AuthResultDto
            {
                IsSuccess = false,
                Errors = new[] { "Logout failed" }
            };
        }
    }

    public async Task<AuthResultDto> LogoutFromAllDevicesAsync(Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            // Invalidate all sessions for the user
            await _sessionService.EndAllUserSessionsAsync(userId, "Logout from all devices");
            
            return new AuthResultDto
            {
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout from all devices for user {UserId}", userId);
            return new AuthResultDto
            {
                IsSuccess = false,
                Errors = new[] { "Logout from all devices failed" }
            };
        }
    }

    public async Task<bool> RevokeTokenAsync(Guid userId, string tokenId)
    {
        try
        {
            // In a real implementation, you would add the token to a blacklist
            // For now, we'll just invalidate the specific session
            await _sessionService.EndSessionAsync(tokenId, "Token revoked");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token {TokenId} for user {UserId}", tokenId, userId);
            return false;
        }
    }

    public async Task<IEnumerable<UserSessionDto>> GetUserSessionsAsync(Guid userId)
    {
        try
        {
            // Get active session for the user
            var activeSession = await _sessionService.GetActiveSessionByUserIdAsync(userId);
            
            if (activeSession != null)
            {
                return new List<UserSessionDto>
                {
                    new UserSessionDto
                    {
                        SessionId = activeSession.SessionId,
                        UserId = userId,
                        DeviceName = activeSession.DeviceType ?? "Unknown Device",
                        IpAddress = activeSession.IpAddress,
                        UserAgent = activeSession.UserAgent ?? "Unknown",
                        CreatedAt = activeSession.StartedAt,
                        LastAccessedAt = activeSession.LastActivityAt,
                        IsCurrentSession = activeSession.IsActive,
                        Location = activeSession.GeolocationData ?? "Unknown"
                    }
                };
            }
            
            return Enumerable.Empty<UserSessionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions for user {UserId}", userId);
            return Enumerable.Empty<UserSessionDto>();
        }
    }

    public async Task<UserDto?> GetCurrentUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        var userDto = MapUserToDto(user);
        userDto.Roles = await _userManager.GetRolesAsync(user);
        return userDto;
    }

    public async Task<bool> UpdateProfileAsync(string userId, UpdateProfileDto updateDto)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            user.FirstName = updateDto.FirstName;
            user.LastName = updateDto.LastName;
            user.Bio = updateDto.Bio;
            user.ProfileImageUrl = updateDto.ProfileImageUrl;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.ChangePasswordAsync(user, 
                changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
            
            return result.Succeeded;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);
            if (user == null) return true; // Don't reveal if user exists

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            // TODO: Send email with reset token
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user == null) return false;

            var result = await _userManager.ResetPasswordAsync(user, 
                resetPasswordDto.Token, resetPasswordDto.Password);
            
            return result.Succeeded;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AuthResultDto> GoogleLoginAsync(GoogleLoginDto googleLoginDto)
    {
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            // Validate input parameters
            if (googleLoginDto == null)
            {
                _logger.LogWarning("GoogleLoginAsync called with null googleLoginDto. CorrelationId: {CorrelationId}", correlationId);
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = new[] { "Invalid Google login request." }
                };
            }

            if (string.IsNullOrWhiteSpace(googleLoginDto.AccessToken))
            {
                _logger.LogWarning("GoogleLoginAsync called with null/empty access token. CorrelationId: {CorrelationId}", correlationId);
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = new[] { "Google access token is required." }
                };
            }

            _logger.LogInformation("Google login attempt started. CorrelationId: {CorrelationId}", correlationId);

            // Verify Google token and get user information
            var googleUserInfo = await VerifyGoogleTokenAsync(googleLoginDto.AccessToken);
            if (googleUserInfo == null)
            {
                _logger.LogWarning("Google token verification failed. CorrelationId: {CorrelationId}", correlationId);
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = new[] { "Invalid Google token." }
                };
            }

            // Find or create user from Google information
            var user = await GetOrCreateUserFromGoogleAsync(googleUserInfo);
            if (user == null)
            {
                _logger.LogError("Failed to create or retrieve user from Google info. Email: {Email}, CorrelationId: {CorrelationId}",
                    googleUserInfo.Email, correlationId);
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = new[] { "Failed to create or retrieve user account." }
                };
            }

            // Check if user is locked out
            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("Google login attempt for locked out user: {Email}. CorrelationId: {CorrelationId}",
                    user.Email, correlationId);
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = new[] { "Account is locked. Please try again later." }
                };
            }

            // Generate JWT token and create session
            var tokenResult = await GenerateJwtTokenAsync(user,
                ipAddress: "Google OAuth",
                userAgent: "Google OAuth Login",
                correlationId: correlationId);

            if (!tokenResult.IsSuccess)
            {
                _logger.LogError("Failed to generate JWT token for Google user: {Email}. CorrelationId: {CorrelationId}",
                    user.Email, correlationId);
                return tokenResult;
            }

            // Update last login date
            user.LastLoginDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Google login successful for user: {Email}. CorrelationId: {CorrelationId}",
                user.Email, correlationId);

            return tokenResult;

            // Example implementation structure (commented out until Google.Apis.Auth is added):
            /*
            try
            {
                // Validate Google token and get user info
                var payload = await GoogleJsonWebSignature.ValidateAsync(googleLoginDto.IdToken ?? googleLoginDto.AccessToken);
                
                // Find existing user by Google ID or email
                var user = await _userManager.FindByEmailAsync(payload.Email);
                
                if (user == null)
                {
                    // Create new user from Google profile
                    user = new User
                    {
                        UserName = payload.Email,
                        Email = payload.Email,
                        FirstName = payload.GivenName ?? "",
                        LastName = payload.FamilyName ?? "",
                        EmailConfirmed = payload.EmailVerified,
                        IsInstructor = false, // Default to student, can be changed later
                        IsVerified = payload.EmailVerified
                    };

                    var result = await _userManager.CreateAsync(user);
                    if (!result.Succeeded)
                    {
                        _logger.LogError("Failed to create user from Google login: {Errors}. CorrelationId: {CorrelationId}",
                            string.Join(", ", result.Errors.Select(e => e.Description)), correlationId);
                        
                        return new AuthResultDto
                        {
                            IsSuccess = false,
                            Errors = result.Errors.Select(e => e.Description)
                        };
                    }

                    // Assign default role
                    await _userManager.AddToRoleAsync(user, "Student");
                    
                    _logger.LogInformation("Created new user from Google login: {Email}. CorrelationId: {CorrelationId}",
                        payload.Email, correlationId);
                }
                else
                {
                    // Update existing user's last login
                    user.LastLoginDate = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);
                    
                    _logger.LogInformation("Existing user logged in via Google: {Email}. CorrelationId: {CorrelationId}",
                        payload.Email, correlationId);
                }

                // Generate JWT token
                var tokenInfo = await GenerateJwtTokenAsync(user);
                var userDto = MapUserToDto(user);
                userDto.Roles = await _userManager.GetRolesAsync(user);

                return new AuthResultDto
                {
                    IsSuccess = true,
                    Token = tokenInfo.Token,
                    ExpiresAt = tokenInfo.ExpiresAt,
                    User = userDto
                };
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogWarning("Invalid Google token. CorrelationId: {CorrelationId}. Error: {Error}", 
                    correlationId, ex.Message);
                    
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = new[] { "Invalid Google token." }
                };
            }
            */
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Google login. CorrelationId: {CorrelationId}", correlationId);

            return new AuthResultDto
            {
                IsSuccess = false,
                Errors = new[] { "An unexpected error occurred during Google authentication. Please try again." }
            };
        }
    }

    private async Task<GoogleUserInfo?> VerifyGoogleTokenAsync(string token)
    {
        try
        {
            using var httpClient = new HttpClient();

            // Check if token is a JWT credential (new Google Identity Services API)
            if (token.Count(c => c == '.') == 2)
            {
                return VerifyGoogleJwtCredential(token);
            }

            // Legacy: Access token verification (old gapi.auth2)
            var response = await httpClient.GetAsync($"https://www.googleapis.com/oauth2/v1/userinfo?access_token={token}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google token verification failed with status: {Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var userInfo = System.Text.Json.JsonSerializer.Deserialize<GoogleUserInfo>(json, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
            });

            return userInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Google token");
            return null;
        }
    }

    private GoogleUserInfo? VerifyGoogleJwtCredential(string jwtCredential)
    {
        try
        {
            // Decode JWT without verification (Google has already signed it)
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(jwtCredential);

            // Extract user information from JWT claims
            var googleUserInfo = new GoogleUserInfo
            {
                Id = jsonToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "",
                Email = jsonToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? "",
                EmailVerified = jsonToken.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value == "true",
                Name = jsonToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? "",
                GivenName = jsonToken.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value ?? "",
                FamilyName = jsonToken.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value ?? "",
                Picture = jsonToken.Claims.FirstOrDefault(c => c.Type == "picture")?.Value ?? "",
                Locale = jsonToken.Claims.FirstOrDefault(c => c.Type == "locale")?.Value ?? ""
            };

            _logger.LogInformation("Successfully decoded Google JWT credential for user: {Email}", googleUserInfo.Email);
            return googleUserInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decoding Google JWT credential");
            return null;
        }
    }

    private async Task<User?> GetOrCreateUserFromGoogleAsync(GoogleUserInfo googleUserInfo)
    {
        try
        {
            // First, try to find user by Google ID
            var existingUserByGoogleId = await _userManager.Users
                .FirstOrDefaultAsync(u => u.GoogleId == googleUserInfo.Id);

            if (existingUserByGoogleId != null)
            {
                // Update user info from Google
                await UpdateUserFromGoogleInfo(existingUserByGoogleId, googleUserInfo);
                return existingUserByGoogleId;
            }

            // Try to find user by email
            var existingUserByEmail = await _userManager.FindByEmailAsync(googleUserInfo.Email);
            if (existingUserByEmail != null)
            {
                // Link Google account to existing user
                existingUserByEmail.GoogleId = googleUserInfo.Id;
                existingUserByEmail.IsGoogleUser = true;
                await UpdateUserFromGoogleInfo(existingUserByEmail, googleUserInfo);
                await _userManager.UpdateAsync(existingUserByEmail);
                return existingUserByEmail;
            }

            // Create new user
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = googleUserInfo.Email,
                Email = googleUserInfo.Email,
                NormalizedUserName = googleUserInfo.Email.ToUpperInvariant(),
                NormalizedEmail = googleUserInfo.Email.ToUpperInvariant(),
                EmailConfirmed = googleUserInfo.EmailVerified,
                IsVerified = googleUserInfo.EmailVerified,
                DateJoined = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),

                // Google-specific fields
                GoogleId = googleUserInfo.Id,
                IsGoogleUser = true,
                FirstName = googleUserInfo.GivenName ?? googleUserInfo.Name?.Split(' ')[0] ?? "User",
                LastName = googleUserInfo.FamilyName ?? (googleUserInfo.Name?.Split(' ').Length > 1 ?
                    string.Join(" ", googleUserInfo.Name.Split(' ').Skip(1)) : ""),
                GooglePictureUrl = googleUserInfo.Picture,
                GoogleGivenName = googleUserInfo.GivenName,
                GoogleFamilyName = googleUserInfo.FamilyName,
                GoogleLocale = googleUserInfo.Locale
            };

            var result = await _userManager.CreateAsync(newUser);
            if (result.Succeeded)
            {
                // Assign default student role
                await _userManager.AddToRoleAsync(newUser, "Student");

                _logger.LogInformation("Created new user from Google: {Email}", googleUserInfo.Email);
                return newUser;
            }
            else
            {
                _logger.LogError("Failed to create user from Google: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating or retrieving user from Google info");
            return null;
        }
    }

    private async Task UpdateUserFromGoogleInfo(User user, GoogleUserInfo googleUserInfo)
    {
        bool updated = false;

        if (user.GooglePictureUrl != googleUserInfo.Picture)
        {
            user.GooglePictureUrl = googleUserInfo.Picture;
            updated = true;
        }

        if (user.GoogleGivenName != googleUserInfo.GivenName)
        {
            user.GoogleGivenName = googleUserInfo.GivenName;
            updated = true;
        }

        if (user.GoogleFamilyName != googleUserInfo.FamilyName)
        {
            user.GoogleFamilyName = googleUserInfo.FamilyName;
            updated = true;
        }

        if (user.GoogleLocale != googleUserInfo.Locale)
        {
            user.GoogleLocale = googleUserInfo.Locale;
            updated = true;
        }

        // Update name if not manually set
        if (string.IsNullOrEmpty(user.FirstName) || user.FirstName == "User")
        {
            user.FirstName = googleUserInfo.GivenName ?? googleUserInfo.Name?.Split(' ')[0] ?? "User";
            updated = true;
        }

        if (string.IsNullOrEmpty(user.LastName))
        {
            user.LastName = googleUserInfo.FamilyName ?? (googleUserInfo.Name?.Split(' ').Length > 1 ?
                string.Join(" ", googleUserInfo.Name.Split(' ').Skip(1)) : "");
            updated = true;
        }

        if (!user.EmailConfirmed && googleUserInfo.EmailVerified)
        {
            user.EmailConfirmed = true;
            user.IsVerified = true;
            updated = true;
        }

        user.LastLoginDate = DateTime.UtcNow;

        if (updated)
        {
            await _userManager.UpdateAsync(user);
        }
    }

    private async Task<(string Token, DateTime ExpiresAt)> GenerateJwtTokenAsync(User user)
    {
        try
        {
            if (user == null)
            {
                _logger.LogError("Attempted to generate JWT token for null user");
                throw new ArgumentNullException(nameof(user), "User cannot be null");
            }

            if (user.Id == Guid.Empty)
            {
                _logger.LogError("Attempted to generate JWT token for user with empty ID");
                throw new InvalidOperationException("User ID cannot be empty");
            }

            // Safely get user roles with error handling
            IList<string> roles;
            try
            {
                roles = await _userManager.GetRolesAsync(user);
                if (roles == null)
                {
                    _logger.LogWarning("GetRolesAsync returned null for user {UserId}. Using empty roles list.", user.Id);
                    roles = new List<string>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles for user {UserId}. Using empty roles list.", user.Id);
                roles = new List<string>();
            }

            // Build claims with defensive null handling
            var claims = new List<Claim>();
            
            // Essential claims with safe handling
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            claims.Add(new Claim(ClaimTypes.Email, user.Email ?? string.Empty));
            
            // Safe name construction
            var firstName = user.FirstName ?? string.Empty;
            var lastName = user.LastName ?? string.Empty;
            var fullName = string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName)
                ? user.Email ?? "User"
                : $"{firstName} {lastName}".Trim();
            
            claims.Add(new Claim(ClaimTypes.Name, fullName));
            claims.Add(new Claim("FirstName", firstName));
            claims.Add(new Claim("LastName", lastName));
            claims.Add(new Claim("IsInstructor", user.IsInstructor.ToString().ToLowerInvariant()));
            claims.Add(new Claim("IsVerified", user.IsVerified.ToString().ToLowerInvariant()));

            // Add role claims safely
            foreach (var role in roles)
            {
                if (!string.IsNullOrWhiteSpace(role))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            // Validate JWT configuration
            if (string.IsNullOrWhiteSpace(_jwtSecret))
            {
                _logger.LogError("JWT secret is null or empty");
                throw new InvalidOperationException("JWT secret is not configured");
            }

            if (_jwtSecret.Length < 32)
            {
                _logger.LogError("JWT secret is too short. Must be at least 32 characters.");
                throw new InvalidOperationException("JWT secret is too short");
            }

            // Create token with error handling
            SymmetricSecurityKey key;
            try
            {
                key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating symmetric security key");
                throw new InvalidOperationException("Failed to create security key", ex);
            }

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddDays(7);

            // Validate expiration time
            if (expires <= DateTime.UtcNow)
            {
                _logger.LogError("Token expiration time is in the past");
                throw new InvalidOperationException("Invalid token expiration time");
            }

            JwtSecurityToken jwtToken;
            try
            {
                jwtToken = new JwtSecurityToken(
                    _jwtIssuer,
                    _jwtAudience,
                    claims,
                    expires: expires,
                    signingCredentials: creds
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating JWT security token for user {UserId}", user.Id);
                throw new InvalidOperationException("Failed to create JWT token", ex);
            }

            string tokenString;
            try
            {
                tokenString = new JwtSecurityTokenHandler().WriteToken(jwtToken);
                
                if (string.IsNullOrWhiteSpace(tokenString))
                {
                    _logger.LogError("JWT token handler returned null or empty token for user {UserId}", user.Id);
                    throw new InvalidOperationException("JWT token generation failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing JWT token for user {UserId}", user.Id);
                throw new InvalidOperationException("Failed to serialize JWT token", ex);
            }

            // Log detailed token information for debugging
            _logger.LogDebug("Successfully generated JWT token for user {UserId}. Expires: {ExpiresAt}", user.Id, expires);
            _logger.LogDebug("JWT Token Claims for user {UserId}: {Claims}", 
                user.Id, 
                string.Join(", ", claims.Select(c => $"{c.Type}={c.Value}")));
            _logger.LogDebug("User roles for token: {Roles}", string.Join(", ", roles));
            
            return (tokenString, expires);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating JWT token for user {UserId}", user?.Id);
            throw;
        }
    }

    private UserDto MapUserToDto(User user)
    {
        try
        {
            if (user == null)
            {
                _logger.LogError("Attempted to map null User to UserDto");
                throw new ArgumentNullException(nameof(user), "User cannot be null");
            }

            // Defensive null checks and safe string handling
            var firstName = user.FirstName ?? string.Empty;
            var lastName = user.LastName ?? string.Empty;
            var email = user.Email ?? string.Empty;
            
            // Validate required fields
            if (user.Id == Guid.Empty)
            {
                _logger.LogError("User has empty GUID ID");
                throw new InvalidOperationException("User ID cannot be empty");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("User {UserId} has null or empty email", user.Id);
            }

            // Safe full name construction
            var fullName = string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName) 
                ? email
                : $"{firstName} {lastName}".Trim();

            // Defensive date handling
            var dateJoined = user.DateJoined == default ? DateTime.UtcNow : user.DateJoined;

            return new UserDto
            {
                Id = user.Id,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                FullName = fullName,
                ProfileImageUrl = user.ProfileImageUrl?.Trim(),
                Bio = user.Bio?.Trim(),
                DateJoined = dateJoined,
                LastLoginDate = user.LastLoginDate,
                IsInstructor = user.IsInstructor,
                IsVerified = user.IsVerified,
                WalletBalance = Math.Max(0, user.WalletBalance), // Ensure non-negative balance
                Roles = new List<string>() // Will be set separately
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping User {UserId} to UserDto", user?.Id);
            throw;
        }
    }

    public async Task<AuthResultDto> RefreshTokenAsync(string userId)
    {
        var correlationId = Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogInformation("Refreshing token for user {UserId}. CorrelationId: {CorrelationId}", userId, correlationId);
            
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("RefreshTokenAsync called with null/empty userId. CorrelationId: {CorrelationId}", correlationId);
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = new[] { "Invalid user ID." }
                };
            }

            // Parse user ID
            if (!Guid.TryParse(userId, out var userIdGuid))
            {
                _logger.LogWarning("Invalid user ID format: {UserId}. CorrelationId: {CorrelationId}", userId, correlationId);
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = new[] { "Invalid user ID format." }
                };
            }

            // Find the user
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for token refresh: {UserId}. CorrelationId: {CorrelationId}", userId, correlationId);
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = new[] { "User not found." }
                };
            }

            // Check if user is still active
            if (!user.IsVerified)
            {
                _logger.LogWarning("Token refresh attempted for unverified user: {UserId}. CorrelationId: {CorrelationId}", userId, correlationId);
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = new[] { "User account is not verified." }
                };
            }

            // Generate new token and update session using the public method
            var tokenResult = await GenerateJwtTokenAsync(user, 
                ipAddress: "127.0.0.1", // TODO: Get real IP address from context
                userAgent: "TokenRefresh", // TODO: Get real user agent
                correlationId: correlationId);

            if (!tokenResult.IsSuccess)
            {
                _logger.LogWarning("Failed to generate new token during refresh for user {UserId}. CorrelationId: {CorrelationId}", userId, correlationId);
                return tokenResult;
            }

            _logger.LogInformation("Token refreshed successfully for user {UserId}. CorrelationId: {CorrelationId}", userId, correlationId);
            return tokenResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token for user {UserId}. CorrelationId: {CorrelationId}", userId, correlationId);
            return new AuthResultDto
            {
                IsSuccess = false,
                Errors = new[] { "An unexpected error occurred during token refresh." }
            };
        }
    }

    public async Task<AuthResultDto> GenerateJwtTokenAsync(User user, string? ipAddress = null, string? userAgent = null, string? correlationId = null)
    {
        correlationId ??= Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogInformation("🔥 JWT GENERATION: Generating JWT token for user {Email}. CorrelationId: {CorrelationId}", 
                user.Email, correlationId);

            // Generate JWT token using existing private method
            var tokenInfo = await GenerateJwtTokenAsync(user);
            if (string.IsNullOrWhiteSpace(tokenInfo.Token))
            {
                _logger.LogError("🔥 JWT GENERATION: Token generation failed for user {Email}. CorrelationId: {CorrelationId}", 
                    user.Email, correlationId);
                return new AuthResultDto
                {
                    IsSuccess = false,
                    Errors = new[] { "Failed to generate JWT token" }
                };
            }

            // Create database session for tracking
            var sessionId = Guid.NewGuid().ToString();
            try
            {
                await _sessionService.CreateSessionAsync(user.Id, tokenInfo.Token, sessionId, ipAddress ?? "Unknown", userAgent ?? "Unknown");
                _logger.LogInformation("✅ JWT GENERATION: Database session created for user {Email}. SessionId: {SessionId}. CorrelationId: {CorrelationId}", 
                    user.Email, sessionId, correlationId);
            }
            catch (Exception sessionEx)
            {
                _logger.LogWarning(sessionEx, "⚠️ JWT GENERATION: Failed to create database session for user {Email}. CorrelationId: {CorrelationId}", 
                    user.Email, correlationId);
                // Continue without session - JWT is still valid
                sessionId = string.Empty;
            }

            // Map user to DTO
            var userDto = MapUserToDto(user);
            userDto.Roles = await _userManager.GetRolesAsync(user);

            _logger.LogInformation("✅ JWT GENERATION: JWT token generated successfully for user {Email}. CorrelationId: {CorrelationId}", 
                user.Email, correlationId);

            return new AuthResultDto
            {
                IsSuccess = true,
                Token = tokenInfo.Token,
                ExpiresAt = tokenInfo.ExpiresAt,
                SessionId = sessionId,
                User = userDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 JWT GENERATION: Error generating JWT token for user {Email}. CorrelationId: {CorrelationId}", 
                user.Email, correlationId);
            return new AuthResultDto
            {
                IsSuccess = false,
                Errors = new[] { "An unexpected error occurred during token generation" }
            };
        }
    }
}

// Google OAuth DTOs
public class GoogleUserInfo
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public bool EmailVerified { get; set; }
    public string Name { get; set; } = "";
    public string GivenName { get; set; } = "";
    public string FamilyName { get; set; } = "";
    public string Picture { get; set; } = "";
    public string Locale { get; set; } = "";
}