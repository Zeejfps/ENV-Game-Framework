using ZGF.Desktop;
using ZGF.Geometry;

namespace ZGF.Gui.Desktop;

/// <summary>
/// The only place a coordinate crosses between the canvas, the window, the desktop and the
/// framebuffer: the scale, the Y flip and the window origin are each written down here once per
/// direction and nowhere else.
/// </summary>
/// <remarks>
/// A snapshot of one window, taken when it is needed. Nothing holds one across a resize or a monitor
/// drag — every field it reads can change under it, and a stale space is a mis-placed menu.
/// </remarks>
public readonly struct WindowSpace
{
    private WindowSpace(float scale, WindowSize windowSize, DeviceSize deviceSize, ScreenPoint origin)
    {
        Scale = scale > 0f ? scale : 1f;
        WindowSize = windowSize;
        DeviceSize = deviceSize;
        Origin = origin;
        CanvasSize = new CanvasSize(
            MathF.Max(1f, MathF.Round(deviceSize.Width / Scale)),
            MathF.Max(1f, MathF.Round(deviceSize.Height / Scale)));
    }

    /// <summary>Device pixels per logical point: the monitor's content scale times the user's UI
    /// scale.</summary>
    public float Scale { get; }

    public WindowSize WindowSize { get; }
    public DeviceSize DeviceSize { get; }

    /// <summary>The window's top-left corner on the desktop.</summary>
    public ScreenPoint Origin { get; }

    /// <summary>What the view tree gets to lay out in: the framebuffer divided by the scale, rounded
    /// to whole points because a canvas is sized in whole points.</summary>
    public CanvasSize CanvasSize { get; }

    /// <summary>Takes a window's current geometry at the given scale. Read the window once here
    /// rather than field by field at each call site, so a resize mid-conversion can't produce a point
    /// that is half in the old geometry and half in the new.</summary>
    public static WindowSpace Of(IWindow window, float scale)
    {
        window.GetPosition(out var originX, out var originY);
        return new WindowSpace(
            scale,
            new WindowSize(Math.Max(1, window.Width), Math.Max(1, window.Height)),
            new DeviceSize(Math.Max(1, window.FramebufferWidth), Math.Max(1, window.FramebufferHeight)),
            new ScreenPoint(originX, originY));
    }

    /// <summary>The same window at a different scale — for sizing content against a monitor the window
    /// has not moved to yet.</summary>
    public WindowSpace WithScale(float scale) => new(scale, WindowSize, DeviceSize, Origin);

    /// <summary>A space for a window that is about to be resized to <paramref name="screenSize"/> —
    /// the popup case, where the OS has been asked for a size the framebuffer may not have caught up
    /// with yet.</summary>
    public WindowSpace WithWindowSize(ScreenSize screenSize)
    {
        var backing = BackingRatio;
        return new WindowSpace(
            Scale,
            new WindowSize(Math.Max(1, screenSize.Width), Math.Max(1, screenSize.Height)),
            new DeviceSize(
                Math.Max(1, (int)MathF.Round(screenSize.Width * backing)),
                Math.Max(1, (int)MathF.Round(screenSize.Height * backing))),
            Origin);
    }

    /// <summary>Framebuffer pixels per screen point — 1 where the OS hands out window sizes in pixels,
    /// 2 on a Retina panel. Not the UI scale: it is a property of the panel, not of the setting.</summary>
    public float BackingRatio => (float)DeviceSize.Width / WindowSize.Width;

    // Screen points per logical point. Derived from the exact quotient rather than from CanvasSize,
    // which is rounded to whole points for the canvas's benefit: on a 32-point placeholder popup that
    // rounding is worth several percent, and it would land in the size the OS is asked for.
    private float ScreenPerCanvasX => WindowSize.Width * Scale / DeviceSize.Width;
    private float ScreenPerCanvasY => WindowSize.Height * Scale / DeviceSize.Height;

    public CanvasPoint ToCanvas(WindowPoint point) => new(
        point.X / ScreenPerCanvasX,
        // The canvas measures up from the bottom edge; the OS measures down from the top.
        (WindowSize.Height - point.Y) / ScreenPerCanvasY);

    public CanvasPoint ToCanvas(ScreenPoint point) =>
        ToCanvas(new WindowPoint(point.X - Origin.X, point.Y - Origin.Y));

    public CanvasSize ToCanvas(ScreenSize size) => new(
        size.Width / ScreenPerCanvasX,
        size.Height / ScreenPerCanvasY);

    public ScreenPoint ToScreen(CanvasPoint point)
    {
        var windowX = point.X * ScreenPerCanvasX;
        var windowY = WindowSize.Height - point.Y * ScreenPerCanvasY;
        return new ScreenPoint(Origin.X + (int)windowX, Origin.Y + (int)windowY);
    }

    public ScreenRect ToScreen(CanvasRect rect)
    {
        var topLeft = ToScreen(rect.TopLeft);
        var bottomRight = ToScreen(rect.BottomRight);
        return new ScreenRect(
            topLeft.X,
            topLeft.Y,
            bottomRight.X - topLeft.X,
            bottomRight.Y - topLeft.Y);
    }

    public ScreenSize ToScreen(CanvasSize size) => new(
        (int)MathF.Ceiling(size.Width * ScreenPerCanvasX),
        (int)MathF.Ceiling(size.Height * ScreenPerCanvasY));
}
