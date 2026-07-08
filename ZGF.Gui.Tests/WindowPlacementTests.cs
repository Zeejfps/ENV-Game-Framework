using ZGF.Desktop;

namespace ZGF.Gui.Tests;

/// <summary>
/// Pins the main-window placement rules: center on the primary monitor when there's no saved
/// position, honor a saved one that's still visible, and clamp/re-center one whose monitor
/// changed — so a window never opens off-screen on a multi-monitor layout.
/// </summary>
public class WindowPlacementTests
{
    // Primary at origin (1920×1040 work area) with a second monitor to its left at negative X.
    private static readonly MonitorWorkArea[] TwoMonitors =
    {
        new(X: 0, Y: 0, Width: 1920, Height: 1040),
        new(X: -1920, Y: 0, Width: 1920, Height: 1040),
    };

    [Fact]
    public void NoSavedPosition_CentersOnPrimary()
    {
        var (x, y) = WindowPlacement.Compute(TwoMonitors, 1400, 900, savedX: null, savedY: null);

        Assert.Equal((1920 - 1400) / 2, x);
        Assert.Equal((1040 - 900) / 2, y);
    }

    [Fact]
    public void CenteringUsesMonitorOffset_NotVirtualScreenOrigin()
    {
        // Primary sits at X=1920 (to the right of a secondary). Centering must land on the
        // primary, not near the virtual-screen origin — the multi-monitor "off to the left" bug.
        var monitors = new[]
        {
            new MonitorWorkArea(1920, 0, 1920, 1040),
            new MonitorWorkArea(0, 0, 1920, 1040),
        };

        var (x, _) = WindowPlacement.Compute(monitors, 1400, 900, savedX: null, savedY: null);

        Assert.Equal(1920 + (1920 - 1400) / 2, x);
    }

    [Fact]
    public void SavedPositionFullyOnMonitor_IsHonoredUnchanged()
    {
        var (x, y) = WindowPlacement.Compute(TwoMonitors, 800, 600, savedX: 200, savedY: 100);

        Assert.Equal(200, x);
        Assert.Equal(100, y);
    }

    [Fact]
    public void SavedPositionOnSecondaryMonitor_IsHonored()
    {
        var (x, y) = WindowPlacement.Compute(TwoMonitors, 800, 600, savedX: -1700, savedY: 100);

        Assert.Equal(-1700, x);
        Assert.Equal(100, y);
    }

    [Fact]
    public void SavedPositionHangingOffEdge_IsClampedFullyOnScreen()
    {
        // Overlaps the primary but spills off the right/bottom; clamp back inside its work area.
        var (x, y) = WindowPlacement.Compute(TwoMonitors, 800, 600, savedX: 1800, savedY: 900);

        Assert.Equal(1920 - 800, x);
        Assert.Equal(1040 - 600, y);
    }

    [Fact]
    public void SavedPositionOnRemovedMonitor_ReCentersOnPrimary()
    {
        // Saved spot lives entirely on a monitor that's no longer connected → no overlap → center.
        var (x, y) = WindowPlacement.Compute(TwoMonitors, 1400, 900, savedX: -1800, savedY: 5000);

        Assert.Equal((1920 - 1400) / 2, x);
        Assert.Equal((1040 - 900) / 2, y);
    }

    [Fact]
    public void WindowLargerThanMonitor_PinsTopLeftToWorkArea()
    {
        var (x, y) = WindowPlacement.Compute(TwoMonitors, 3000, 2000, savedX: 100, savedY: 50);

        Assert.Equal(0, x);
        Assert.Equal(0, y);
    }

    [Fact]
    public void NoMonitors_FallsBackToSavedOrOrigin()
    {
        Assert.Equal((10, 20), WindowPlacement.Compute(Array.Empty<MonitorWorkArea>(), 800, 600, 10, 20));
        Assert.Equal((0, 0), WindowPlacement.Compute(Array.Empty<MonitorWorkArea>(), 800, 600, null, null));
    }
}
