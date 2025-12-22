namespace InsightLearn.WebAssembly.Services;

/// <summary>
/// Service interface for detecting device type, browser, and platform information.
/// Used for adaptive rendering and responsive optimizations.
/// </summary>
public interface IDeviceDetectionService
{
    /// <summary>
    /// Gets the current device information.
    /// </summary>
    Task<DeviceInfo> GetDeviceInfoAsync();

    /// <summary>
    /// Checks if the current device is a mobile device.
    /// </summary>
    Task<bool> IsMobileAsync();

    /// <summary>
    /// Checks if the current device is a tablet.
    /// </summary>
    Task<bool> IsTabletAsync();

    /// <summary>
    /// Checks if the current device is a desktop.
    /// </summary>
    Task<bool> IsDesktopAsync();

    /// <summary>
    /// Checks if the device supports touch input.
    /// </summary>
    Task<bool> IsTouchDeviceAsync();

    /// <summary>
    /// Gets the current viewport width.
    /// </summary>
    Task<int> GetViewportWidthAsync();

    /// <summary>
    /// Gets the current viewport height.
    /// </summary>
    Task<int> GetViewportHeightAsync();

    /// <summary>
    /// Event fired when device orientation changes.
    /// </summary>
    event EventHandler<OrientationChangedEventArgs>? OrientationChanged;

    /// <summary>
    /// Event fired when viewport size changes.
    /// </summary>
    event EventHandler<ViewportChangedEventArgs>? ViewportChanged;
}

/// <summary>
/// Represents detailed device information.
/// </summary>
public class DeviceInfo
{
    /// <summary>
    /// Device type (Mobile, Tablet, Desktop).
    /// </summary>
    public DeviceType DeviceType { get; set; }

    /// <summary>
    /// Browser name (Chrome, Firefox, Safari, Edge, etc.).
    /// </summary>
    public string Browser { get; set; } = string.Empty;

    /// <summary>
    /// Browser version.
    /// </summary>
    public string BrowserVersion { get; set; } = string.Empty;

    /// <summary>
    /// Operating system (Windows, macOS, iOS, Android, Linux).
    /// </summary>
    public string OperatingSystem { get; set; } = string.Empty;

    /// <summary>
    /// Operating system version.
    /// </summary>
    public string OsVersion { get; set; } = string.Empty;

    /// <summary>
    /// Whether the device supports touch input.
    /// </summary>
    public bool IsTouch { get; set; }

    /// <summary>
    /// Current viewport width in pixels.
    /// </summary>
    public int ViewportWidth { get; set; }

    /// <summary>
    /// Current viewport height in pixels.
    /// </summary>
    public int ViewportHeight { get; set; }

    /// <summary>
    /// Device pixel ratio (for retina/high-DPI displays).
    /// </summary>
    public double PixelRatio { get; set; } = 1.0;

    /// <summary>
    /// Current device orientation.
    /// </summary>
    public DeviceOrientation Orientation { get; set; }

    /// <summary>
    /// Whether the device is in standalone mode (PWA).
    /// </summary>
    public bool IsStandalone { get; set; }

    /// <summary>
    /// User-Agent string.
    /// </summary>
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// Preferred color scheme (light, dark).
    /// </summary>
    public string PreferredColorScheme { get; set; } = "light";

    /// <summary>
    /// Whether reduced motion is preferred.
    /// </summary>
    public bool PrefersReducedMotion { get; set; }
}

/// <summary>
/// Device type enumeration.
/// </summary>
public enum DeviceType
{
    Desktop,
    Tablet,
    Mobile
}

/// <summary>
/// Device orientation enumeration.
/// </summary>
public enum DeviceOrientation
{
    Portrait,
    Landscape
}

/// <summary>
/// Event args for orientation change events.
/// </summary>
public class OrientationChangedEventArgs : EventArgs
{
    public DeviceOrientation NewOrientation { get; set; }
    public DeviceOrientation PreviousOrientation { get; set; }
}

/// <summary>
/// Event args for viewport change events.
/// </summary>
public class ViewportChangedEventArgs : EventArgs
{
    public int NewWidth { get; set; }
    public int NewHeight { get; set; }
    public int PreviousWidth { get; set; }
    public int PreviousHeight { get; set; }
}
