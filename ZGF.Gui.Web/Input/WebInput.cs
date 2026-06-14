using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using ZGF.Geometry;

namespace ZGF.Gui.Web.Input;

[Flags]
internal enum WebMods
{
    None = 0,
    Shift = 1,
    Ctrl = 2,
    Alt = 4,
    Meta = 8,
}

internal readonly record struct WebKeyEvent(string Code, bool Down, WebMods Mods);

/// <summary>
/// Self-contained DOM input bridge for the web host. <c>main.js</c> attaches the
/// browser pointer/keyboard/wheel listeners and forwards them to the [JSExport]
/// callbacks here with coordinates already converted to canvas-logical, Y-up GUI
/// space (origin bottom-left, matching the canvas).
///
/// This intentionally does NOT reuse the desktop interaction layer (InputSystem /
/// controllers / components live in ZGF.Gui.Desktop, which is coupled to GLFW/GL/
/// Metal). Driving the real view/controller framework from the browser needs that
/// layer extracted into a platform-neutral package first — see the host README /
/// docs. For now this exposes a simple polled snapshot the demo render reads.
/// </summary>
[SupportedOSPlatform("browser")]
public static partial class WebInput
{
    private static readonly HashSet<int> Buttons = new();
    private static readonly Queue<WebKeyEvent> KeyEvents = new();

    public static PointF MousePoint { get; private set; } = new(float.MinValue, float.MinValue);
    public static bool MouseInside { get; private set; }
    public static float WheelX { get; private set; }
    public static float WheelY { get; private set; }
    internal static WebMods Mods { get; private set; }

    public static bool IsButtonDown(int button) => Buttons.Contains(button);
    public static bool IsOver(RectF rect) =>
        MouseInside &&
        MousePoint.X >= rect.Left && MousePoint.X < rect.Right &&
        MousePoint.Y >= rect.Bottom && MousePoint.Y < rect.Top;

    /// Drains accumulated wheel delta (call once per frame).
    public static (float X, float Y) TakeWheel()
    {
        var r = (WheelX, WheelY);
        WheelX = 0;
        WheelY = 0;
        return r;
    }

    internal static bool TryDequeueKey(out WebKeyEvent e)
    {
        if (KeyEvents.Count > 0) { e = KeyEvents.Dequeue(); return true; }
        e = default;
        return false;
    }

    // ---- [JSExport] callbacks from main.js (coords already in GUI space) ----

    [JSExport]
    internal static void PointerMove(double x, double y)
    {
        MousePoint = new PointF((float)x, (float)y);
        MouseInside = true;
    }

    [JSExport]
    internal static void PointerEnter() => MouseInside = true;

    [JSExport]
    internal static void PointerLeave()
    {
        MouseInside = false;
        MousePoint = new PointF(float.MinValue, float.MinValue);
    }

    [JSExport]
    internal static void PointerDown(int button, double x, double y, int mods)
    {
        MousePoint = new PointF((float)x, (float)y);
        MouseInside = true;
        Mods = (WebMods)mods;
        Buttons.Add(button);
    }

    [JSExport]
    internal static void PointerUp(int button, double x, double y, int mods)
    {
        MousePoint = new PointF((float)x, (float)y);
        Mods = (WebMods)mods;
        Buttons.Remove(button);
    }

    [JSExport]
    internal static void Wheel(double dx, double dy)
    {
        WheelX += (float)dx;
        WheelY += (float)dy;
    }

    [JSExport]
    internal static void KeyDown(string code, int mods)
    {
        Mods = (WebMods)mods;
        KeyEvents.Enqueue(new WebKeyEvent(code, true, (WebMods)mods));
    }

    [JSExport]
    internal static void KeyUp(string code, int mods)
    {
        Mods = (WebMods)mods;
        KeyEvents.Enqueue(new WebKeyEvent(code, false, (WebMods)mods));
    }

    [JSExport]
    internal static void Blur()
    {
        Buttons.Clear();
        Mods = WebMods.None;
    }
}
