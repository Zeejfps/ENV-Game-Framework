using ZGF.Geometry;
using ZGF.Gui.Views;

namespace ZGF.Gui.VerticalScrollBar;

public sealed class VerticalScrollPane : MultiChildView, IKeyboardScrollable
{
    public event Action<float>? ScrollPositionChanged;

    // Fraction of the remaining distance the rendered offset closes each frame while easing.
    private const float EaseFactor = 0.28f;

    private float _distanceFromTop;   // the target offset
    private float _displayDistance;   // the rendered offset; eases toward the target when SmoothScrolling
    private float _maxDistanceFromTop;
    private float _bottomInset;
    private readonly ColumnView _columnView;

    private float CurrentDistance => _displayDistance;

    public float ScrollNormalized { get; private set; }
    public float Scale { get; private set; }
    public override IComponentCollection Children => _columnView.Children;

    public override bool ClipsContent => true;

    /// <summary>When true the rendered offset eases toward its target over a few frames instead of
    /// snapping — used for keyboard avoidance so the content glides. Requires a continuously rendering
    /// host (the animation drives its own next frame from <see cref="OnDrawSelf"/>).</summary>
    public bool SmoothScrolling { get; set; }

    /// <summary>Points reserved at the bottom of the viewport (e.g. for an on-screen keyboard);
    /// shrinks the scrollable region so content can be pulled out from behind it.</summary>
    public float BottomInset
    {
        get => _bottomInset;
        set
        {
            if (Math.Abs(_bottomInset - value) < 0.0001f)
                return;
            _bottomInset = value;
            SetDirty();
        }
    }

    public StyleValue<int> Gap
    {
        get => _columnView.Gap;
        set => _columnView.Gap = value;       
    }

    public VerticalScrollPane()
    {
        _columnView = new ColumnView();
        AddChildToSelf(_columnView);
    }

    protected override void OnLayoutChild(in RectF position, View child)
    {
        var childHeight = child.MeasureHeight(position.Width);
        child.BottomConstraint = position.Top + CurrentDistance - childHeight;
        child.LeftConstraint = position.Left;
        child.WidthConstraint = position.Width;
        child.LayoutSelf();
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        // Keep the ease alive: layout only re-runs when dirty, and a SetDirty() issued during a layout
        // pass is cleared by View.LayoutSelf before the next frame. Draw runs every frame after layout
        // and isn't gated by the dirty flag, so request the next frame here until the offset settles.
        if (SmoothScrolling && MathF.Abs(_distanceFromTop - _displayDistance) > 0.0001f)
            SetDirty();
    }

    protected override void OnDrawChildren(ICanvas c)
    {
        // Only clip while content is actually scrolled out of view. When everything fits
        // (Scale == 1) nothing needs hiding, and clipping to the exact viewport would only
        // scissor borders and anti-aliasing flush against the edge — e.g. the right border of a
        // Browse button at the end of a dialog row. Children are forced to the viewport width, so
        // there is never horizontal overflow to clip regardless.
        if (Scale < 1f)
        {
            c.PushClip(Position);
            base.OnDrawChildren(c);
            c.PopClip();
        }
        else
        {
            base.OnDrawChildren(c);
        }
    }
    
    public void ScrollUp(float delta)
    {
        Scroll(-delta);
    }
    
    public void ScrollDown(float delta)
    {
        Scroll(delta);
    }

    /// <summary>
    /// Scroll so <paramref name="descendant"/> (at any depth) sits within the visible region — the
    /// viewport minus <see cref="BottomInset"/>. Works in absolute canvas coordinates, so the
    /// descendant need not be a direct child.
    /// </summary>
    public void ScrollIntoView(View descendant)
    {
        RecomputeRange();

        var viewport = Position;
        const float margin = 8f;

        // Where the descendant sits inside the scrolled content, independent of the current
        // scroll/ease state: content top and descendant both carry the same offset, so it cancels.
        // Using this (rather than the rendered Position, which lags behind the target while
        // SmoothScrolling eases) keeps the computation idempotent — repeated calls converge instead
        // of compounding, which is what the keyboard's repeated frame notifications would otherwise do.
        var offsetFromTop = _columnView.Position.Top - descendant.Position.Top;
        var offsetFromBottom = offsetFromTop + descendant.Position.Height;

        // Canvas-y of the content top is viewport.Top + distance; an edge at offset `o` sits at
        // (viewport.Top + distance) - o. Solve for the distance that lands the descendant inside the
        // band [visibleBottom, viewport.Top]. The keyboard covers up to canvas-y == _bottomInset.
        var visibleBottom = MathF.Max(viewport.Bottom, _bottomInset);
        var target = _distanceFromTop;
        var descBottom = viewport.Top + target - offsetFromBottom;
        var descTop = viewport.Top + target - offsetFromTop;

        if (descBottom < visibleBottom + margin)
            target += visibleBottom + margin - descBottom;         // hidden below the inset: pull up
        else if (descTop > viewport.Top - margin)
            target -= descTop - (viewport.Top - margin);           // above the viewport: pull down

        var clamped = Math.Clamp(target, 0f, _maxDistanceFromTop);
        if (Math.Abs(clamped - _distanceFromTop) < 0.0001f)
            return;

        _distanceFromTop = clamped;
        SetDirty();
    }

    // Scroll range depends on the inset (content can be pulled out from behind the keyboard even
    // when it would otherwise fit). Kept in sync by OnLayoutChildren and recomputed on demand
    // before a programmatic scroll so it isn't a frame stale.
    private void RecomputeRange()
    {
        var contentHeight = _columnView.MeasureHeight(Position.Width);
        var effectiveHeight = Position.Height - _bottomInset;
        _maxDistanceFromTop = MathF.Max(0f, contentHeight - effectiveHeight);
        if (_distanceFromTop > _maxDistanceFromTop)
            _distanceFromTop = _maxDistanceFromTop;
    }

    public void Scroll(float delta)
    {
        _distanceFromTop += delta;
        if (_distanceFromTop < 0)
        {
            _distanceFromTop = 0;
        }
        else if (_distanceFromTop > _maxDistanceFromTop)
        {
            _distanceFromTop = _maxDistanceFromTop;
        }
        SetDirty();
    }

    public void ScrollToTop()
    {
        _distanceFromTop = 0;
        SetDirty();
    }
    
    public void ScrollToBottom()
    {
        var viewportHeight = Position.Height;
        var contentHeight = _columnView.MeasureHeight(Position.Width);

        if (contentHeight <= viewportHeight)
            return;

        var delta = _distanceFromTop + contentHeight - viewportHeight;
        Scroll(delta);
    }
    
    protected override void OnLayoutChildren()
    {
        // Finalize the scroll range and clamp _distanceFromTop BEFORE laying out children: the base
        // pass positions the content at CurrentDistance, so any reset/clamp has to happen first or it
        // won't take effect until a later pass — and there's no guaranteed later pass once this view
        // goes clean. (This is what left the content stuck scrolled after the keyboard closed.)
        var viewportHeight = Position.Height;
        var effectiveHeight = viewportHeight - _bottomInset;
        var contentHeight = _columnView.MeasureHeight(Position.Width);

        if (contentHeight <= effectiveHeight)
        {
            _maxDistanceFromTop = 0;
            _distanceFromTop = 0;        // content fits again: return to the top
            Scale = 1f;
        }
        else
        {
            _maxDistanceFromTop = contentHeight - effectiveHeight;
            _distanceFromTop = Math.Clamp(_distanceFromTop, 0f, _maxDistanceFromTop);
            Scale = effectiveHeight / contentHeight;
        }

        // Ease the rendered offset toward the target (snapping the last half-point so it settles
        // cleanly). Not clamped to the range: when the keyboard closes the range collapses to 0, and
        // the offset still needs to glide down through the now-out-of-range values to reach it.
        // OnDrawSelf keeps requesting frames until this converges.
        if (SmoothScrolling)
        {
            var diff = _distanceFromTop - _displayDistance;
            _displayDistance = MathF.Abs(diff) < 0.5f ? _distanceFromTop : _displayDistance + diff * EaseFactor;
        }
        else
        {
            _displayDistance = _distanceFromTop;
        }

        base.OnLayoutChildren();

        if (_maxDistanceFromTop <= 0f)
        {
            ScrollNormalized = 0f;
        }
        else
        {
            var scrollOffset = Position.Bottom - _columnView.Position.Bottom;
            ScrollNormalized = 1f - Math.Clamp(scrollOffset / _maxDistanceFromTop, 0f, 1f);
        }

        ScrollPositionChanged?.Invoke(ScrollNormalized);
    }

    public void SetNormalizedScrollPosition(float normalizedPosition, bool notify = true)
    {
        var viewportHeight = Position.Height;
        var contentHeight = _columnView.MeasureHeight(Position.Width);

        var delta = contentHeight - viewportHeight;
        _distanceFromTop = delta * normalizedPosition;
        SetDirty();
    }
}