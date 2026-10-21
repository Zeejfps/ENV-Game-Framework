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
}

/// <summary>How an element is sized along one axis. <see cref="Fit"/> wraps the content,
/// <see cref="Grow"/> takes a share of the parent's leftover space, <see cref="Percent"/> takes a
/// fraction of the parent's inner size.</summary>
public readonly record struct Sizing(SizingKind Kind, float Value, float Min, float Max)
{
    public static Sizing Fit(float min = 0f, float max = float.PositiveInfinity) => new(SizingKind.Fit, 0f, min, max);
    public static Sizing Grow(float min = 0f, float max = float.PositiveInfinity) => new(SizingKind.Grow, 0f, min, max);
    public static Sizing Fixed(float size) => new(SizingKind.Fixed, size, size, size);
    public static Sizing Percent(float fraction) => new(SizingKind.Percent, fraction, 0f, float.PositiveInfinity);

    public static implicit operator Sizing(float size) => Fixed(size);
}

public readonly record struct Spacing(float Left, float Top, float Right, float Bottom)
{
    public Spacing(float all) : this(all, all, all, all) { }

    public Spacing(float horizontal, float vertical) : this(horizontal, vertical, horizontal, vertical) { }

    public static implicit operator Spacing(float all) => new(all);
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

/// <summary>Declares one element: how it sizes, lays out its children and paints its box.
/// Unset values take the defaults of the method it is passed to.</summary>
public readonly record struct Box
{
    public Direction Direction { get; init; }
    public Sizing? Width { get; init; }
    public Sizing? Height { get; init; }
    public Spacing? Padding { get; init; }
    public float? Gap { get; init; }
    public Align? AlignX { get; init; }
    public Align? AlignY { get; init; }
    public uint Background { get; init; }
    public uint BorderColor { get; init; }
    public float BorderWidth { get; init; }
    public float Radius { get; init; }
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
