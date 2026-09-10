namespace ZGF.Geometry;

// Four spaces that were all called "coordinates" and all typed PointF/PointI/int, so every
// conversion between them was a convention someone had to remember. Naming them makes a mixed-space
// expression fail to compile instead of quietly rendering at the wrong size.
//
//   Canvas — logical points, bottom-up Y, window-relative: the view tree and everything it measures.
//   Window — screen points, top-down Y, window-relative: what the OS reports about a window.
//   Screen — screen points, top-down Y, desktop-absolute: window positions, monitor work areas.
//   Device — framebuffer pixels: the viewport, glyph baking, read-back.
//
// None of them convert implicitly, in either direction — an implicit conversion would switch the
// checker straight back off. Construct explicitly, unwrap explicitly, and convert only through
// WindowSpace, which is the one place the scale, the Y flip and the window origin are written down.

/// <summary>A point in the view tree's coordinates: logical points, measured up from the window's
/// bottom-left corner.</summary>
public readonly record struct CanvasPoint(float X, float Y)
{
    public PointF Points => new(X, Y);

    public static CanvasPoint From(PointF point) => new(point.X, point.Y);
}

/// <summary>A rectangle in the view tree's coordinates, laid out like <see cref="RectF"/>: anchored
/// at its bottom-left, with <see cref="Top"/> the larger Y.</summary>
public readonly record struct CanvasRect(float Left, float Bottom, float Width, float Height)
{
    public float Top => Bottom + Height;
    public float Right => Left + Width;
    public CanvasPoint TopLeft => new(Left, Top);
    public CanvasPoint BottomRight => new(Right, Bottom);

    public RectF Points => new(Left, Bottom, Width, Height);

    public static CanvasRect From(RectF rect) => new(rect.Left, rect.Bottom, rect.Width, rect.Height);
}

/// <summary>A size in logical points. Fractional: it is a framebuffer divided by a scale, and the
/// rounding to whole points belongs to whoever sizes a canvas with it.</summary>
public readonly record struct CanvasSize(float Width, float Height);

/// <summary>A point in a window's own screen-point coordinates, measured down from its top-left
/// corner — where the OS reports the cursor.</summary>
public readonly record struct WindowPoint(float X, float Y);

/// <summary>A window's client size in screen points — what the OS was asked for and what it
/// reports, which on a Retina panel is half the framebuffer.</summary>
public readonly record struct WindowSize(int Width, int Height);

/// <summary>A point on the desktop in screen points, absolute across every monitor. Negative on a
/// display left of or above the primary one.</summary>
public readonly record struct ScreenPoint(int X, int Y)
{
    public PointI Points => new(X, Y);

    public static ScreenPoint From(PointI point) => new(point.X, point.Y);
}

/// <summary>A rectangle on the desktop in screen points — a window's placement, a monitor's work
/// area.</summary>
public readonly record struct ScreenRect(int X, int Y, int Width, int Height)
{
    public ScreenPoint TopLeft => new(X, Y);

    public RectI Points => new(X, Y, Width, Height);

    public static ScreenRect From(RectI rect) => new(rect.X, rect.Y, rect.Width, rect.Height);
}

public readonly record struct ScreenSize(int Width, int Height);

/// <summary>A size in framebuffer pixels — the surface the GPU actually draws into.</summary>
public readonly record struct DeviceSize(int Width, int Height);
