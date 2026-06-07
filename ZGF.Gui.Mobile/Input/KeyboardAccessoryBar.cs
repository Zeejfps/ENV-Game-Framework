using ZGF.Geometry;
using ZGF.Gui.Mobile.Controllers;
using ZGF.Gui.Views;

namespace ZGF.Gui.Mobile.Input;

/// <summary>
/// A framework-drawn "Done" bar for dismissing the on-screen keyboard — the in-house replacement
/// for a platform input-accessory view, so the toolbar is rendered and themed by ZGF.Gui like the
/// rest of the UI (the keyboard itself is still the OS's). Add it as a full-screen overlay child of
/// the root: it sizes to the whole screen but only paints a bar flush above the keyboard, placed
/// from <see cref="KeyboardInsets"/>, and hides itself when no keyboard is shown. Tapping Done
/// raises <see cref="DoneClicked"/>.
/// </summary>
public sealed class KeyboardAccessoryBar : MultiChildView
{
    /// <summary>Visible height of the bar in canvas points. Keyboard avoidance adds this on top of
    /// the keyboard height so a focused field clears the bar as well as the keyboard.</summary>
    public const float BarHeight = 44f;

    // The bar extends this far below the keyboard's top edge so it tucks behind the keyboard (whose
    // top has rounded corners) with no seam; the overlap is occluded by the keyboard.
    private const float KeyboardOverlap = 16f;

    // Matches the keyboard's rounded top corners.
    private const float CornerRadius = 16f;

    // Slide + fade duration, matched to the system keyboard's animation; eased with cubic ease-out.
    private const float AnimationMs = 250f;

    // Drawn above all screen content (sliders place their thumbs a few z-levels above their base).
    private const int OverlayZIndex = 1000;

    private readonly KeyboardInsets _insets;
    private readonly RectView _bar;
    private readonly TextView _doneLabel;
    private readonly uint _bgColor;
    private readonly uint _borderColor;
    private readonly uint _doneColor;

    // Cubic-eased tween state. The bar slides (inset) and fades (opacity) together over AnimationMs
    // from the values captured when the keyboard last changed to the current open/closed target.
    private float _displayInset;
    private float _opacity;
    private float _fromInset, _toInset;
    private float _fromOpacity, _toOpacity;
    private long _animStartTick;

    public Action? DoneClicked { get; set; }

    public KeyboardAccessoryBar(KeyboardInsets insets, uint backgroundColor, uint doneColor, uint borderColor)
    {
        _insets = insets;
        _bgColor = backgroundColor;
        _doneColor = doneColor;
        _borderColor = borderColor;

        _doneLabel = new TextView
        {
            Text = "Done",
            FontSize = 17f,
            TextColor = doneColor,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };

        var doneButton = new RectView
        {
            Width = 72f,
            Children = { _doneLabel },
        };
        doneButton.UsePointerController(_ => new ButtonPointerController(doneButton)
        {
            Clicked = () => DoneClicked?.Invoke(),
        });

        var row = new FlexRowView
        {
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                new FlexItem { Grow = 1f, Child = new MultiChildView() },
                doneButton,
            },
        };

        _bar = new RectView
        {
            BackgroundColor = backgroundColor,
            BorderColor = new BorderColorStyle { Top = borderColor },
            BorderSize = new BorderSizeStyle { Top = 1f },
            BorderRadius = new BorderRadiusStyle { TopLeft = CornerRadius, TopRight = CornerRadius },
            // Keep the Done row in the visible BarHeight; the overlap below is hidden by the keyboard.
            Padding = new PaddingStyle { Right = 8, Bottom = (int)KeyboardOverlap },
            Children = { row },
        };
        AddChildToSelf(_bar);

        ZIndex = OverlayZIndex;
        _displayInset = _toInset = _fromInset = _insets.Bottom;
        _opacity = _toOpacity = _fromOpacity = _insets.Bottom > 0f ? 1f : 0f;
        IsVisible = _insets.Bottom > 0f;
        _insets.Changed += HandleInsetsChanged;
    }

    // Place the bar above the keyboard, using the eased _displayInset so it slides with the keyboard.
    // The canvas is Y-up, so the keyboard covers [0, inset]; the bar's visible BarHeight sits just
    // above it, and it extends KeyboardOverlap below to tuck behind the keyboard's rounded top edge.
    protected override void OnLayoutChild(in RectF position, View child)
    {
        child.LeftConstraint = position.Left;
        child.BottomConstraint = position.Bottom + _displayInset - KeyboardOverlap;
        child.WidthConstraint = position.Width;
        child.HeightConstraint = BarHeight + KeyboardOverlap;
        child.LayoutSelf();
    }

    protected override void OnLayoutChildren()
    {
        // Sample the cubic-eased tween at the current time, then lay out and recolor at that point.
        // OnDrawSelf keeps requesting frames until the tween completes.
        var e = Easing.OutCubic(Progress());
        _displayInset = Easing.Lerp(_fromInset, _toInset, e);
        _opacity = Easing.Lerp(_fromOpacity, _toOpacity, e);

        _bar.BackgroundColor = WithOpacity(_bgColor, _opacity);
        _bar.BorderColor = new BorderColorStyle { Top = WithOpacity(_borderColor, _opacity) };
        _doneLabel.TextColor = WithOpacity(_doneColor, _opacity);

        base.OnLayoutChildren();
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        // Layout is dirty-gated and a SetDirty() during layout is cleared before the next frame;
        // draw runs every frame, so re-arm the tween here until it completes. Once closed and done,
        // hide so the overlay stops drawing and hit-testing.
        if (Progress() < 1f)
            SetDirty();
        else if (_insets.Bottom <= 0f)
            IsVisible = false;
    }

    private void HandleInsetsChanged()
    {
        // Retarget the tween from wherever the bar currently sits, so a mid-animation reverse is
        // smooth. Opening: show immediately so it can slide up and fade in; closing: stay visible and
        // let the tween fade/slide it out — OnDrawSelf hides it once done.
        _fromInset = _displayInset;
        _toInset = _insets.Bottom;
        _fromOpacity = _opacity;
        _toOpacity = _insets.Bottom > 0f ? 1f : 0f;
        _animStartTick = Environment.TickCount64;

        if (_insets.Bottom > 0f)
            IsVisible = true;
        SetDirty();
    }

    private float Progress()
    {
        var elapsed = Environment.TickCount64 - _animStartTick;
        return Math.Clamp(elapsed / AnimationMs, 0f, 1f);
    }

    private static uint WithOpacity(uint argb, float opacity)
    {
        var baseAlpha = argb >> 24;
        var alpha = (uint)(baseAlpha * Math.Clamp(opacity, 0f, 1f));
        return (alpha << 24) | (argb & 0x00FFFFFF);
    }
}
