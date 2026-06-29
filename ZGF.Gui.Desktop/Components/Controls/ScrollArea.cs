using ZGF.Gui.Desktop.Components.VerticalScrollBar;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Desktop.Widgets;
using ZGF.Gui.VerticalScrollBar;
using ZGF.Gui.Widgets;
using ZGF.Gui;

namespace ZGF.Gui.Desktop.Components.Controls;

/// <summary>
/// Vertically scrollable content with a synced scrollbar: mouse wheel, thumb drag,
/// track click and arrow keys. Content scrolls only when it outgrows the viewport, so
/// give the area a bounded height (fixed, MaxHeight, or a fill slot like BorderLayout's
/// center) for scrolling to engage.
/// </summary>
public sealed record ScrollArea : Widget
{
    public IWidget[] Children { get; init; } = [];
    public int Gap { get; init; }

    /// <summary>Track/thumb colors for the scrollbar; unset, falls back to
    /// <see cref="ScrollBarStyle.Default"/>.</summary>
    public Prop<ScrollBarStyle> Style { get; init; } = ScrollBarStyle.Default;

    /// <summary>When true, the scrollbar is hidden while all content fits and reappears once it
    /// overflows. Its gutter stays reserved either way, so content width doesn't jump as scrolling
    /// engages. Defaults to false: the scrollbar is always shown.</summary>
    public bool AutoHide { get; init; }

    /// <summary>Pixels travelled per mouse-wheel notch. Defaults to <see cref="ScrollDefaults.WheelStep"/>;
    /// set it to keep wheel speed uniform with other scroll surfaces in the host app.</summary>
    public float WheelStep { get; init; } = ScrollDefaults.WheelStep;

    protected override IWidget Build(Context ctx)
    {
        var pane = new VerticalScrollPane { Gap = Gap };
        foreach (var child in Children)
            pane.Children.Add(child.BuildView(ctx));

        var thumb = new VerticalScrollBarThumbView();
        var scrollBar = new ScrollBar { Thumb = thumb, Style = Style }.BuildView(ctx);
        return new KbmInput
        {
            Controller = _ => new ScrollAreaKbmController(pane, thumb, AutoHide ? scrollBar : null, WheelStep),
            Child = new BorderLayout
            {
                Center = new Raw { View = pane },
                East = new Raw { View = scrollBar },
            },
        };
    }
}

/// <summary>
/// Keeps a scroll pane and its scrollbar thumb in sync (position and thumb scale, both ways)
/// and handles wheel/arrow-key scrolling for the area it is registered on. Subscriptions
/// follow the controller's mounted lifetime.
/// </summary>
public sealed class ScrollAreaKbmController : KeyboardMouseController, IDisposable
{
    private readonly VerticalScrollPane _pane;
    private readonly VerticalScrollBarThumbView _thumb;
    private readonly View? _autoHideTarget;
    private readonly float _wheelStep;

    public ScrollAreaKbmController(VerticalScrollPane pane, VerticalScrollBarThumbView thumb, View? autoHideTarget = null, float wheelStep = ScrollDefaults.WheelStep)
    {
        _pane = pane;
        _thumb = thumb;
        _autoHideTarget = autoHideTarget;
        _wheelStep = wheelStep;

        _pane.ScrollToTop();
        _thumb.ScrollToTop();

        _thumb.ScrollPositionChanged += OnBarScrolled;
        _pane.ScrollPositionChanged += OnPaneScrolled;
    }

    public void Dispose()
    {
        _thumb.ScrollPositionChanged -= OnBarScrolled;
        _pane.ScrollPositionChanged -= OnPaneScrolled;
    }

    private void OnPaneScrolled(float normalized)
    {
        // The pane raises this from every layout pass, so it doubles as the seam that
        // keeps the thumb's scale in sync as content grows or shrinks.
        _thumb.Scale = _pane.Scale;
        _thumb.SetScrollPositionNormalized(normalized);

        // Scale < 1 means content overflows the viewport. Toggling visibility (not presence)
        // keeps the gutter reserved, so engaging the scrollbar never reflows the content.
        if (_autoHideTarget != null)
            _autoHideTarget.IsVisible = _pane.Scale < 1f;
    }

    private void OnBarScrolled(float normalized)
    {
        _pane.SetNormalizedScrollPosition(normalized);
    }

    public override void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;

        _pane.Scroll(e.DeltaY * -_wheelStep);
        e.Consume();
    }

    public override void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;
        if (e.State != InputState.Pressed)
            return;

        if (e.Key == ZGF.KeyboardModule.KeyboardKey.UpArrow)
        {
            _pane.ScrollUp(10f);
            e.Consume();
        }
        else if (e.Key == ZGF.KeyboardModule.KeyboardKey.DownArrow)
        {
            _pane.ScrollDown(10f);
            e.Consume();
        }
    }
}
