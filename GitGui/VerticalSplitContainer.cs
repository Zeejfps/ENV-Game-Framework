using ZGF.Gui;

namespace GitGui;

/// <summary>
/// Stacks a top view and (optionally) a bottom view separated by a draggable horizontal
/// splitter. The split is tracked as a fraction of the available height so window resizes
/// scale both halves; the splitter controller calls <see cref="AdjustBottomFractionByPixels"/>
/// to nudge the fraction during a drag.
/// </summary>
internal sealed class VerticalSplitContainer : MultiChildView
{
    private const float SplitterThickness = 5f;
    private const float MinFraction = 0.1f;
    private const float MaxFraction = 0.9f;

    private readonly View _top;
    private readonly View _bottom;
    private readonly View _splitter;
    private bool _bottomVisible;
    private float _bottomFraction;

    public VerticalSplitContainer(View top, View bottom, View splitter, float bottomFraction)
    {
        _top = top;
        _bottom = bottom;
        _splitter = splitter;
        _bottomFraction = Math.Clamp(bottomFraction, MinFraction, MaxFraction);
        AddChildToSelf(_top);
    }

    public bool BottomVisible
    {
        get => _bottomVisible;
        set
        {
            if (_bottomVisible == value) return;
            _bottomVisible = value;
            if (_bottomVisible)
            {
                AddChildToSelf(_splitter);
                AddChildToSelf(_bottom);
            }
            else
            {
                RemoveChildFromSelf(_splitter);
                RemoveChildFromSelf(_bottom);
            }
            SetDirty();
        }
    }

    // Positive dy = mouse moved up (Y-up coords). Up = bigger bottom (diff grows), down =
    // smaller bottom. Clamped so neither side can collapse to zero.
    public void AdjustBottomFractionByPixels(float dy)
    {
        if (!_bottomVisible) return;
        var available = Position.Height - SplitterThickness;
        if (available <= 0f) return;
        _bottomFraction = Math.Clamp(_bottomFraction + dy / available, MinFraction, MaxFraction);
        SetDirty();
    }

    protected override void OnLayoutChildren()
    {
        var pos = Position;
        if (pos.Width <= 0f || pos.Height <= 0f) return;

        if (!_bottomVisible)
        {
            LayoutSlice(_top, pos.Left, pos.Bottom, pos.Width, pos.Height);
            return;
        }

        var available = Math.Max(0f, pos.Height - SplitterThickness);
        var bottomH = available * _bottomFraction;
        var topH = available - bottomH;

        LayoutSlice(_top, pos.Left, pos.Bottom + bottomH + SplitterThickness, pos.Width, topH);
        LayoutSlice(_splitter, pos.Left, pos.Bottom + bottomH, pos.Width, SplitterThickness);
        LayoutSlice(_bottom, pos.Left, pos.Bottom, pos.Width, bottomH);
    }

    private static void LayoutSlice(View child, float left, float bottom, float width, float height)
    {
        child.LeftConstraint = left;
        child.BottomConstraint = bottom;
        child.WidthConstraint = width;
        child.HeightConstraint = height;
        child.LayoutSelf();
    }
}