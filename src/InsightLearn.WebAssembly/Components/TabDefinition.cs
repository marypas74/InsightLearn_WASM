namespace InsightLearn.WebAssembly.Components;

/// <summary>
/// Definition for a tab item in ResponsiveTabs component.
/// v2.2.2-dev - Responsive Design System
/// </summary>
public class TabDefinition
{
    /// <summary>
    /// Unique identifier for the tab
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display label for the tab
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// FontAwesome icon class (e.g., "fas fa-home")
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Optional badge count (0 = hidden)
    /// </summary>
    public int Badge { get; set; } = 0;

    /// <summary>
    /// Whether the tab is disabled
    /// </summary>
    public bool IsDisabled { get; set; } = false;

    /// <summary>
    /// Optional tooltip text
    /// </summary>
    public string? Tooltip { get; set; }

    /// <summary>
    /// Creates a new TabDefinition
    /// </summary>
    public TabDefinition() { }

    /// <summary>
    /// Creates a new TabDefinition with basic properties
    /// </summary>
    public TabDefinition(string id, string label, string icon = "")
    {
        Id = id;
        Label = label;
        Icon = icon;
    }

    /// <summary>
    /// Creates a new TabDefinition with badge
    /// </summary>
    public TabDefinition(string id, string label, string icon, int badge)
    {
        Id = id;
        Label = label;
        Icon = icon;
        Badge = badge;
    }
}
