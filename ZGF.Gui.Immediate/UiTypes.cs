using ZGF.Geometry;

namespace ZGF.Gui.Immediate;

/// <summary>A rect in UI space: points, origin top-left, Y down.</summary>
public readonly record struct UiRect(float X, float Y, float Width, float Height)
{
    public float Right => X + Width;
    public float Bottom => Y + Height;

    public bool Contains(PointF point) => point.X >= X && point.X < Right && point.Y >= Y && point.Y < Bottom;

    public bool Intersects(UiRect other) => X < other.Right && other.X < Right && Y < other.Bottom && other.Y < Bottom;

    public UiRect Intersect(UiRect other)
    {
        var x = MathF.Max(X, other.X);
        var y = MathF.Max(Y, other.Y);
        return new UiRect(x, y, MathF.Max(0f, MathF.Min(Right, other.Right) - x), MathF.Max(0f, MathF.Min(Bottom, other.Bottom) - y));
    }
}

public enum SizingKind
{
    Fit,
    Grow,
    Fixed,
    Percent,
    Shrink,
}

/// <summary>How an element is sized along one axis. <see cref="Fit"/> wraps the content,
/// <see cref="Grow"/> takes a share of the parent's leftover space, <see cref="Percent"/> takes a
/// fraction of the parent's inner size, <see cref="Shrink"/> wraps the content but gives up room when
/// the parent runs out; each kept within its min and max.</summary>
public readonly record struct Sizing(SizingKind Kind, float Value, float Min, float Max)
{
    /// <summary>How long a growable or shrinkable element keeps its room when its parent runs out:
    /// siblings of the lowest priority shrink to their minimum before any of a higher one shrinks at
    /// all. Zero unless set — <c>Sizing.Grow() with { Priority = 1 }</c> for a primary label beside a
    /// secondary one.</summary>
    public int Priority { get; init; }

    public static Sizing Fit(float min = 0f, float max = float.PositiveInfinity) => new(SizingKind.Fit, 0f, min, max);
    public static Sizing Grow(float min = 0f, float max = float.PositiveInfinity) => new(SizingKind.Grow, 0f, min, max);
    public static Sizing Fixed(float size) => new(SizingKind.Fixed, size, size, size);
    public static Sizing Percent(float fraction, float min = 0f, float max = float.PositiveInfinity) => new(SizingKind.Percent, fraction, min, max);

    /// <summary>As large as the content, or <paramref name="size"/> when that is larger, up to
    /// <paramref name="max"/>, and never larger than that even with room to spare; when the parent runs
    /// out of room it gives up room down to <paramref name="min"/>, or to the least its children can
    /// take when that is more: a picker sized for its widest choice, or the row of controls it sits in,
    /// in a narrow pane.</summary>
    public static Sizing Shrink(float min = 0f, float size = 0f, float max = float.PositiveInfinity) => new(SizingKind.Shrink, size, min, max);

    public static implicit operator Sizing(float size) => Fixed(size);
}

public readonly record struct Spacing(float Left, float Top, float Right, float Bottom)
{
    public Spacing(float all) : this(all, all, all, all) { }

    public Spacing(float horizontal, float vertical) : this(horizontal, vertical, horizontal, vertical) { }

    public static implicit operator Spacing(float all) => new(all);
}

/// <summary>A drop shadow: <see cref="Blur"/> is a CSS-style blur radius, <see cref="Spread"/>
/// grows the shadow's rect before blurring, and <see cref="Color"/> is 0xAARRGGBB.</summary>
public readonly record struct UiShadow(float OffsetX, float OffsetY, float Blur, float Spread, uint Color)
{
    public bool IsVisible => (Color >> 24) != 0;

    /// <summary>How far the shadow reaches past each edge of its box.</summary>
    public Spacing Extent
    {
        get
        {
            if (!IsVisible) return default;
            var reach = MathF.Max(0f, Spread + Blur * 1.5f + 1f);
            return new Spacing(
                MathF.Ceiling(MathF.Max(0f, reach - OffsetX)),
                MathF.Ceiling(MathF.Max(0f, reach - OffsetY)),
                MathF.Ceiling(MathF.Max(0f, reach + OffsetX)),
                MathF.Ceiling(MathF.Max(0f, reach + OffsetY)));
        }
    }
}

/// <summary>A radius per corner, for a rect rounded only on some.</summary>
public readonly record struct Corners(float TopLeft, float TopRight, float BottomRight, float BottomLeft)
{
    public Corners(float all) : this(all, all, all, all) { }

    public static Corners Left(float radius) => new(radius, 0f, 0f, radius);
    public static Corners Right(float radius) => new(0f, radius, radius, 0f);
}

/// <summary>A small stroked shape <see cref="Ui.PaintGlyph"/> draws in its element.</summary>
public enum UiGlyph
{
    Checkmark,
    Cross,
    DisclosureClosed,
    DisclosureOpen,
    DropdownArrow,
}

public enum Align
{
    Start,
    Center,
    End,
}

public enum Direction
{
    Vertical,
    Horizontal,
}

/// <summary>Where a <see cref="Ui.Popup"/> sits against its anchor.</summary>
public enum PopupSide
{
    Below,
    End,
}

/// <summary>Which ways a <see cref="Ui.Scroll(int, ScrollAxes, Box)"/> scrolls.</summary>
public enum ScrollAxes
{
    Vertical,
    Horizontal,
    Both,
}

/// <summary>Declares one element: how it sizes, lays out its children and paints its box.
/// Unset values take the defaults of the method it is passed to; set ones, zero included, are kept,
/// so <c>Radius = 0f</c> asks a widget for square corners and <c>BorderWidth = 0f</c> for none.</summary>
public readonly record struct Box
{
    public Direction Direction { get; init; }
    public Sizing? Width { get; init; }
    public Sizing? Height { get; init; }
    public Spacing? Padding { get; init; }
    public float? Gap { get; init; }
    public Align? AlignX { get; init; }
    public Align? AlignY { get; init; }
    public uint? Background { get; init; }
    public uint? BorderColor { get; init; }
    public float? BorderWidth { get; init; }
    public float? Radius { get; init; }

    /// <summary>A shadow drawn under the box, outside it. A popup's window grows to hold it.</summary>
    public UiShadow? Shadow { get; init; }
    public bool Clip { get; init; }

    /// <summary>Shifts the children left/up by this much, for content scrolled inside a clip.</summary>
    public float ScrollX { get; init; }
    public float ScrollY { get; init; }
}

/// <summary>What the pointer did to element <see cref="Id"/> this frame. Record the element with
/// that id so the next frame can hit-test it. <see cref="Pressed"/>, <see cref="Held"/> and
/// <see cref="Clicked"/> are for the left button; <see cref="ClickedWith"/> answers for any.</summary>
public readonly record struct Interaction(int Id, bool Hovered, bool Pressed, bool Held, bool Clicked)
{
    private readonly int _otherClicks;

    internal Interaction(int id, bool hovered, bool pressed, bool held, bool clicked, int clickCount, int otherClicks)
        : this(id, hovered, pressed, held, clicked)
    {
        ClickCount = clickCount;
        _otherClicks = otherClicks;
    }

    /// <summary>On the frame of <see cref="Pressed"/>, how many presses in quick succession landed
    /// on this element: 1, then 2 for a double-click, 3 for a triple. Zero on other frames.</summary>
    public int ClickCount { get; }

    public bool DoubleClicked => Pressed && ClickCount == 2;

    /// <summary>True on the frame <paramref name="button"/> is released over this element after
    /// being pressed on it.</summary>
    public bool ClickedWith(PointerButton button) =>
        button == PointerButton.Left ? Clicked : (_otherClicks & (1 << (int)button)) != 0;

    public static Interaction Inert(int id) => new(id, false, false, false, false);
}

/// <summary>The link of a <see cref="Ui.RichText"/> run clicked this frame, and the button it was
/// clicked with; <see cref="Link"/> is null when none was.</summary>
public readonly record struct RichTextClick(object? Link, PointerButton Button);

/// <summary>What a single-line text does when it is wider than its element.</summary>
public enum TextOverflow
{
    Clip,
    Ellipsis,
    EllipsisMiddle,
}
