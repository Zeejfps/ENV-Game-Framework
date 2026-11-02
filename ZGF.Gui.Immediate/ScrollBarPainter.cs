namespace ZGF.Gui.Immediate;

/// <summary>What one bar of a scroll shows this frame, for its <see cref="ScrollBarPainter"/>. The
/// painter is called while the bar's grab area is being recorded: an element over the thumb, the
/// thumb's length along <see cref="Axis"/> and the bar's grab thickness across it. A
/// <see cref="Ui.Paint{TState}"/> recorded there finds where the thumb and its track are drawn with
/// <see cref="ThumbRect"/> and <see cref="TrackRect"/>.</summary>
public readonly struct ScrollBarState
{
    private readonly float _inset;
    private readonly float _thumbStart;
    private readonly float _trackLength;

    internal ScrollBarState(Direction axis, Interaction thumb, bool pointerWithin, float scrolled, double sinceMoved, float thickness,
        float inset, float thumbStart, float trackLength)
    {
        Axis = axis;
        Id = thumb.Id;
        Hovered = thumb.Hovered;
        Dragged = thumb.Held;
        PointerWithin = pointerWithin;
        Scrolled = scrolled;
        SinceMoved = sinceMoved;
        Thickness = thickness;
        _inset = inset;
        _thumbStart = thumbStart;
        _trackLength = trackLength;
    }

    /// <summary>The way the thumb moves: <see cref="Direction.Vertical"/> for the bar at the right edge.</summary>
    public Direction Axis { get; }

    /// <summary>The thumb's id, for state an app's painter keeps per bar, such as an animation.</summary>
    public int Id { get; }

    /// <summary>The pointer is over the thumb's grab area.</summary>
    public bool Hovered { get; }

    /// <summary>The thumb is being dragged.</summary>
    public bool Dragged { get; }

    /// <summary>The pointer is over the scroll.</summary>
    public bool PointerWithin { get; }

    /// <summary>How far the content is scrolled along <see cref="Axis"/>, from 0 at its start to 1 at its end.</summary>
    public float Scrolled { get; }

    /// <summary>The seconds of <see cref="Ui.RealTime"/> since the scroll last moved along
    /// <see cref="Axis"/>; infinity when it has not moved since it was first shown.</summary>
    public double SinceMoved { get; }

    /// <summary>The thumb's painted thickness, the theme's <see cref="UiTheme.ScrollBarWidth"/>.</summary>
    public float Thickness { get; }

    /// <summary>Where the stock thumb is drawn, given <paramref name="element"/>, the grab area's
    /// laid-out rect: its length, <see cref="Thickness"/> thick, <see cref="UiTheme.ScrollBarInset"/>
    /// in from the scroll's edge.</summary>
    public UiRect ThumbRect(UiRect element) => Axis == Direction.Horizontal
        ? new UiRect(element.X, element.Bottom - _inset - Thickness, element.Width, Thickness)
        : new UiRect(element.Right - _inset - Thickness, element.Y, Thickness, element.Height);

    /// <summary>The lane the thumb travels along, given the grab area's laid-out rect: as thick as
    /// <see cref="ThumbRect"/>, and as long as the scroll's view, less any corner the other bar takes.</summary>
    public UiRect TrackRect(UiRect element)
    {
        var thumb = ThumbRect(element);
        return Axis == Direction.Horizontal
            ? thumb with { X = thumb.X - _thumbStart, Width = _trackLength }
            : thumb with { Y = thumb.Y - _thumbStart, Height = _trackLength };
    }
}

/// <summary>Records what a scroll's bar draws, into its grab area — see <see cref="ScrollBarState"/>.</summary>
public delegate void ScrollBarPainter(Ui ui, in ScrollBarState bar);
