using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace InsightLearn.WebAssembly.Services;

/// <summary>
/// Service for detecting device type, browser, and platform information.
/// Uses JavaScript interop for accurate client-side detection.
/// </summary>
public class DeviceDetectionService : IDeviceDetectionService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<DeviceDetectionService> _logger;
    private DeviceInfo? _cachedDeviceInfo;
    private DotNetObjectReference<DeviceDetectionService>? _dotNetRef;
    private bool _isInitialized;

    public event EventHandler<OrientationChangedEventArgs>? OrientationChanged;
    public event EventHandler<ViewportChangedEventArgs>? ViewportChanged;

    public DeviceDetectionService(IJSRuntime jsRuntime, ILogger<DeviceDetectionService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    private async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            await _jsRuntime.InvokeVoidAsync("deviceDetection.initialize", _dotNetRef);
            _isInitialized = true;
            _logger.LogDebug("Device detection initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize device detection: {ErrorMessage}", ex.Message);
        }
    }

    public async Task<DeviceInfo> GetDeviceInfoAsync()
    {
        if (_cachedDeviceInfo != null)
        {
            _logger.LogDebug("Returning cached device info (DeviceType: {DeviceType})", _cachedDeviceInfo.DeviceType);
            return _cachedDeviceInfo;
        }

        await InitializeAsync();

        try
        {
            var deviceInfo = await _jsRuntime.InvokeAsync<DeviceInfo>("deviceDetection.getDeviceInfo");

            // Fix v2.3.63: Add null check to prevent NullReferenceException
            if (deviceInfo == null)
            {
                _logger.LogWarning("Device detection returned null, using fallback");
                _cachedDeviceInfo = GetFallbackDeviceInfo();
                return _cachedDeviceInfo;
            }

            _cachedDeviceInfo = deviceInfo;
            _logger.LogInformation("Device info retrieved: DeviceType={DeviceType}, Browser={Browser}, OS={OS}, IsTouch={IsTouch}",
                _cachedDeviceInfo.DeviceType, _cachedDeviceInfo.Browser, _cachedDeviceInfo.OperatingSystem, _cachedDeviceInfo.IsTouch);
            return _cachedDeviceInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device info, using fallback: {ErrorMessage}", ex.Message);
            _cachedDeviceInfo = GetFallbackDeviceInfo();
            return _cachedDeviceInfo;
        }
    }

    public async Task<bool> IsMobileAsync()
    {
        var info = await GetDeviceInfoAsync();
        return info.DeviceType == DeviceType.Mobile;
    }

    public async Task<bool> IsTabletAsync()
    {
        var info = await GetDeviceInfoAsync();
        return info.DeviceType == DeviceType.Tablet;
    }

    public async Task<bool> IsDesktopAsync()
    {
        var info = await GetDeviceInfoAsync();
        return info.DeviceType == DeviceType.Desktop;
    }

    public async Task<bool> IsTouchDeviceAsync()
    {
        var info = await GetDeviceInfoAsync();
        return info.IsTouch;
    }

    public async Task<int> GetViewportWidthAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<int>("deviceDetection.getViewportWidth");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get viewport width via JS, using fallback: {ErrorMessage}", ex.Message);
            return _cachedDeviceInfo?.ViewportWidth ?? 1024;
        }
    }

    public async Task<int> GetViewportHeightAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<int>("deviceDetection.getViewportHeight");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get viewport height via JS, using fallback: {ErrorMessage}", ex.Message);
            return _cachedDeviceInfo?.ViewportHeight ?? 768;
        }
    }

    [JSInvokable]
    public void OnOrientationChanged(string newOrientation, string previousOrientation)
    {
        var newOrient = ParseOrientation(newOrientation);
        var prevOrient = ParseOrientation(previousOrientation);

        if (_cachedDeviceInfo != null)
            _cachedDeviceInfo.Orientation = newOrient;

        OrientationChanged?.Invoke(this, new OrientationChangedEventArgs
        {
            NewOrientation = newOrient,
            PreviousOrientation = prevOrient
        });
    }

    [JSInvokable]
    public void OnViewportChanged(int newWidth, int newHeight, int previousWidth, int previousHeight)
    {
        if (_cachedDeviceInfo != null)
        {
            _cachedDeviceInfo.ViewportWidth = newWidth;
            _cachedDeviceInfo.ViewportHeight = newHeight;
            _cachedDeviceInfo.Orientation = newWidth > newHeight
                ? DeviceOrientation.Landscape
                : DeviceOrientation.Portrait;
            _cachedDeviceInfo.DeviceType = DetermineDeviceType(newWidth);
        }

        ViewportChanged?.Invoke(this, new ViewportChangedEventArgs
        {
            NewWidth = newWidth,
            NewHeight = newHeight,
            PreviousWidth = previousWidth,
            PreviousHeight = previousHeight
        });
    }

    private DeviceOrientation ParseOrientation(string orientation)
    {
        return orientation.ToLowerInvariant() switch
        {
            "landscape" => DeviceOrientation.Landscape,
            _ => DeviceOrientation.Portrait
        };
    }

    private DeviceType DetermineDeviceType(int width)
    {
        return width switch
        {
            < 768 => DeviceType.Mobile,
            < 1024 => DeviceType.Tablet,
            _ => DeviceType.Desktop
        };
    }

    private DeviceInfo GetFallbackDeviceInfo()
    {
        return new DeviceInfo
        {
            DeviceType = DeviceType.Desktop,
            Browser = "Unknown",
            BrowserVersion = "0",
            OperatingSystem = "Unknown",
            OsVersion = "0",
            IsTouch = false,
            ViewportWidth = 1024,
            ViewportHeight = 768,
            PixelRatio = 1.0,
            Orientation = DeviceOrientation.Landscape,
            IsStandalone = false,
            UserAgent = "Unknown",
            PreferredColorScheme = "light",
            PrefersReducedMotion = false
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_isInitialized)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("deviceDetection.dispose");
                _logger.LogDebug("Device detection disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing device detection: {ErrorMessage}", ex.Message);
            }
        }
        _dotNetRef?.Dispose();
    }
}
