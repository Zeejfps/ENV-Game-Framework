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

    /// <summary>Passes <see cref="VerticalScrollPane.StretchContent"/> through: while the content is
    /// shorter than the viewport it is laid out at the viewport height, so a <see cref="Grow"/> child
    /// fills the area (e.g. a text editor whose whole surface should be clickable). Once the content
    /// outgrows the viewport this has no effect — the area just scrolls.</summary>
    public bool StretchContent { get; init; }

    /// <summary>Passes <see cref="VerticalScrollPane.FillParent"/> through: the area reports no
    /// intrinsic height (a flex basis of 0), taking only the leftover space its slot hands it.
    /// Set this when the area sits in a <see cref="Grow"/>; otherwise its content's full height
    /// leaks into the parent's measure and growing content inflates the surrounding layout
    /// instead of scrolling.</summary>
    public bool FillParent { get; init; }

    /// <summary>Follows content that grows at the end — a log, a chat transcript — while the view is
    /// resting at (or within a hair of) the bottom. Scrolling away deliberately releases the pin and
    /// the area stays where it was put; scrolling back to the bottom takes it again. Off by default,
    /// so an ordinary list never moves under the reader.</summary>
    public bool StickToBottom { get; init; }

    protected override IWidget Build(Context ctx)
    {
        var pane = new VerticalScrollPane { Gap = Gap, StretchContent = StretchContent, FillParent = FillParent };

        // The pane is the subtree's ambient IScrollScope, so content that tracks a point of
        // interest (a text editor's caret, a focus ring's focused control) can ask the nearest
        // enclosing scroll container to keep it in view without any explicit wiring.
        var scope = new Context(ctx);
        scope.AddService<IScrollScope>(pane);
        foreach (var child in Children)
            pane.Children.Add(child.BuildView(scope));

        var thumb = new VerticalScrollBarThumbView();
        var scrollBar = new ScrollBar { Thumb = thumb, Style = Style }.BuildView(ctx);
        return new KbmInput
        {
            Controller = _ => new ScrollAreaKbmController(pane, thumb, AutoHide ? scrollBar : null, WheelStep, StickToBottom),
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
    // How close to the bottom still counts as resting there. A few pixels of slack, so content that
    // ends mid-line or a fractional layout offset doesn't silently drop the pin.
    private const float BottomStickThreshold = 24f;

    private readonly VerticalScrollPane _pane;
    private readonly VerticalScrollBarThumbView _thumb;
    private readonly View? _autoHideTarget;
    private readonly float _wheelStep;
    private readonly bool _stickToBottom;

    // Whether the view is resting at the end and should follow content that grows there. Only a
    // deliberate scroll changes it, and the decision is deferred to the next layout, where the
    // pane's normalized position is current rather than one frame stale.
    private bool _pinnedToBottom = true;
    private bool _repinPending;

    // True while the pane's own layout is pushing its position onto the thumb, so the echo back
    // through OnBarScrolled isn't mistaken for the user dragging it.
    private bool _syncingThumb;

    public ScrollAreaKbmController(
        VerticalScrollPane pane,
        VerticalScrollBarThumbView thumb,
        View? autoHideTarget = null,
        float wheelStep = ScrollDefaults.WheelStep,
        bool stickToBottom = false)
    {
        _pane = pane;
        _thumb = thumb;
        _autoHideTarget = autoHideTarget;
        _wheelStep = wheelStep;
        _stickToBottom = stickToBottom;

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
        _syncingThumb = true;
        _thumb.Scale = _pane.Scale;
        _thumb.SetScrollPositionNormalized(normalized);
        _syncingThumb = false;

        // Scale < 1 means content overflows the viewport. Toggling visibility (not presence)
        // keeps the gutter reserved, so engaging the scrollbar never reflows the content.
        if (_autoHideTarget != null)
            _autoHideTarget.IsVisible = _pane.Scale < 1f;

        if (!_stickToBottom) return;

        // A scroll the user asked for decides the pin; every other pass is content changing under a
        // view that is either following the end or parked away from it on purpose.
        if (_repinPending)
        {
            _repinPending = false;
            _pinnedToBottom = IsNearBottom(normalized);
        }
        else if (_pinnedToBottom)
        {
            // No-ops once already at the end, so this settles rather than relaying out forever.
            _pane.ScrollToBottom();
        }
    }

    // Converts the remaining travel back into pixels — the pane's normalized position compresses as
    // content grows, so a normalized epsilon would mean a different distance on every transcript.
    private bool IsNearBottom(float normalized)
    {
        if (_pane.Scale >= 1f) return true; // everything fits; there is no "away from the bottom"
        var travel = _pane.Position.Height * (1f / _pane.Scale - 1f);
        return (1f - normalized) * travel <= BottomStickThreshold;
    }

    private void OnBarScrolled(float normalized)
    {
        if (!_syncingThumb) _repinPending = true;
        _pane.SetNormalizedScrollPosition(normalized);
    }

    public override void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;

        _repinPending = true;
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
            _repinPending = true;
            _pane.ScrollUp(10f);
            e.Consume();
        }
        else if (e.Key == ZGF.KeyboardModule.KeyboardKey.DownArrow)
        {
            _repinPending = true;
            _pane.ScrollDown(10f);
            e.Consume();
        }
    }
}
