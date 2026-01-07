using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;
using System.Timers;

namespace InsightLearn.WebAssembly.Services.Auth;

/// <summary>
/// Service that monitors user activity and automatically logs out after inactivity timeout.
/// Default timeout: 30 minutes of inactivity.
/// </summary>
public class SessionTimeoutService : IDisposable
{
    private readonly IAuthService _authService;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<SessionTimeoutService> _logger;

    private System.Timers.Timer? _inactivityTimer;
    private System.Timers.Timer? _warningTimer;
    private DateTime _lastActivity;
    private bool _isInitialized;
    private DotNetObjectReference<SessionTimeoutService>? _dotNetRef;

    // Configurable timeout settings (in minutes)
    public int TimeoutMinutes { get; set; } = 30;
    public int WarningMinutes { get; set; } = 5; // Show warning 5 minutes before timeout

    // Events
    public event Action? OnWarningShown;
    public event Action? OnSessionExpired;
    public event Action<int>? OnCountdownTick; // Remaining seconds

    public bool IsWarningVisible { get; private set; }
    public int RemainingSeconds { get; private set; }

    public SessionTimeoutService(
        IAuthService authService,
        IJSRuntime jsRuntime,
        ILogger<SessionTimeoutService> logger)
    {
        _authService = authService;
        _jsRuntime = jsRuntime;
        _logger = logger;
        _lastActivity = DateTime.UtcNow;
    }

    /// <summary>
    /// Initialize activity tracking via JavaScript interop
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            _dotNetRef = DotNetObjectReference.Create(this);

            // Setup JavaScript event listeners for user activity
            await _jsRuntime.InvokeVoidAsync("sessionTimeout.initialize", _dotNetRef);

            // Start inactivity check timer (every 30 seconds)
            _inactivityTimer = new System.Timers.Timer(30000);
            _inactivityTimer.Elapsed += CheckInactivity;
            _inactivityTimer.AutoReset = true;
            _inactivityTimer.Start();

            _isInitialized = true;
            _logger.LogInformation("Session timeout service initialized. Timeout: {TimeoutMinutes} minutes", TimeoutMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize session timeout service: {ErrorMessage}", ex.Message);
        }
    }

    /// <summary>
    /// Called from JavaScript when user activity is detected
    /// </summary>
    [JSInvokable]
    public void OnUserActivity()
    {
        _lastActivity = DateTime.UtcNow;

        // If warning was visible, hide it and reset
        if (IsWarningVisible)
        {
            IsWarningVisible = false;
            _warningTimer?.Stop();
            _warningTimer?.Dispose();
            _warningTimer = null;
            _logger.LogInformation("User activity detected, warning dismissed");
        }
    }

    private void CheckInactivity(object? sender, ElapsedEventArgs e)
    {
        var inactiveMinutes = (DateTime.UtcNow - _lastActivity).TotalMinutes;
        var timeoutThreshold = TimeoutMinutes - WarningMinutes;

        // Check if we should show warning
        if (inactiveMinutes >= timeoutThreshold && !IsWarningVisible)
        {
            ShowWarning();
        }

        // Check if session has expired
        if (inactiveMinutes >= TimeoutMinutes)
        {
            ExpireSession();
        }
    }

    private void ShowWarning()
    {
        IsWarningVisible = true;
        RemainingSeconds = WarningMinutes * 60;

        _logger.LogInformation("Showing inactivity warning. Session expires in {WarningMinutes} minutes", WarningMinutes);

        OnWarningShown?.Invoke();

        // Start countdown timer (every second)
        _warningTimer = new System.Timers.Timer(1000);
        _warningTimer.Elapsed += (s, e) =>
        {
            RemainingSeconds--;
            OnCountdownTick?.Invoke(RemainingSeconds);

            if (RemainingSeconds <= 0)
            {
                ExpireSession();
            }
        };
        _warningTimer.AutoReset = true;
        _warningTimer.Start();
    }

    private async void ExpireSession()
    {
        _inactivityTimer?.Stop();
        _warningTimer?.Stop();

        _logger.LogWarning("Session expired due to inactivity. Logging out user");

        try
        {
            OnSessionExpired?.Invoke();
            await _authService.LogoutAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during session expiry logout: {ErrorMessage}", ex.Message);
        }
    }

    /// <summary>
    /// Reset the inactivity timer (call after successful authentication)
    /// </summary>
    public void ResetTimer()
    {
        _lastActivity = DateTime.UtcNow;
        IsWarningVisible = false;
        _warningTimer?.Stop();
        _logger.LogDebug("Session timer reset");
    }

    /// <summary>
    /// Stop monitoring (call on logout)
    /// </summary>
    public void Stop()
    {
        _inactivityTimer?.Stop();
        _warningTimer?.Stop();
        IsWarningVisible = false;
        _isInitialized = false;
        _logger.LogInformation("Session monitoring stopped");
    }

    /// <summary>
    /// Extend the session by the configured timeout duration
    /// </summary>
    public void ExtendSession()
    {
        OnUserActivity();
        _logger.LogInformation("Session extended by user request");
    }

    public void Dispose()
    {
        _inactivityTimer?.Stop();
        _inactivityTimer?.Dispose();
        _warningTimer?.Stop();
        _warningTimer?.Dispose();
        _dotNetRef?.Dispose();
    }
}
