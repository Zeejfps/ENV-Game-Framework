using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// A two-segment pill control that drives the global <see cref="MainViewMode"/> state.
/// Lives in <see cref="ActionsToolbar"/>; reads/writes <c>State&lt;MainViewMode&gt;</c>
/// looked up from the Context on attach.
/// </summary>
public sealed class ModeSwitcherView : MultiChildView
{
    private const float PillHeight = 28f;
    private const float SegmentCornerRadius = 5f;

    private static readonly uint PillBorder = DialogPalette.ButtonBorder;
    private static readonly uint SegmentActiveBg = DialogPalette.RowActive;
    private static readonly uint SegmentHoverBg = DialogPalette.ButtonHover;
    private static readonly uint SegmentIdleBg = 0x00000000u;
    private static readonly uint SegmentActiveText = DialogPalette.RowTextActive;
    private static readonly uint SegmentIdleText = DialogPalette.RowText;

    private readonly Segment _history;
    private readonly Segment _localChanges;

    private State<MainViewMode>? _mode;
    private IDisposable? _subscription;

    public ModeSwitcherView()
    {
        PreferredHeight = PillHeight;

        _history = new Segment("History", () => SetMode(MainViewMode.History));
        _localChanges = new Segment("Changes", () => SetMode(MainViewMode.LocalChanges));

        var separator = new RectView
        {
            BackgroundColor = PillBorder,
            PreferredWidth = 1f,
        };

        AddChildToSelf(new RectView
        {
            BackgroundColor = SegmentIdleBg,
            BorderColor = BorderColorStyle.All(PillBorder),
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(SegmentCornerRadius),
            Children =
            {
                new RowView
                {
                    Children = { _localChanges, separator, _history },
                },
            },
        });
    }

    protected override void OnAttachedToContext(Context context)
    {
        _mode = context.Get<State<MainViewMode>>();
        if (_mode != null)
            _subscription = _mode.Subscribe(OnModeChanged);
    }

    protected override void OnDetachedFromContext(Context context)
    {
        _subscription?.Dispose();
        _subscription = null;
        _mode = null;
    }

    private void SetMode(MainViewMode mode)
    {
        if (_mode == null) return;
        _mode.Value = mode;
    }

    private void OnModeChanged(MainViewMode mode)
    {
        _history.SetActive(mode == MainViewMode.History);
        _localChanges.SetActive(mode == MainViewMode.LocalChanges);
    }

    private sealed class Segment : MultiChildView
    {
        private readonly RectView _bg;
        private readonly TextView _label;
        private bool _isActive;
        private bool _isHovered;

        public Segment(string label, Action onClick)
        {
            PreferredHeight = PillHeight;

            _label = new TextView
            {
                Text = label,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = SegmentIdleText,
            };

            _bg = new RectView
            {
                BackgroundColor = SegmentIdleBg,
                Padding = new PaddingStyle { Left = 12, Right = 12 },
                Children = { _label },
            };
            AddChildToSelf(_bg);

            Behaviors.Add(new HoverableButtonController(onClick, OnHoverChanged));
        }

        public void SetActive(bool active)
        {
            if (_isActive == active) return;
            _isActive = active;
            ApplyVisualState();
        }

        private void OnHoverChanged(bool hovered)
        {
            if (_isHovered == hovered) return;
            _isHovered = hovered;
            ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            if (_isActive)
            {
                _bg.BackgroundColor = SegmentActiveBg;
                _label.TextColor = SegmentActiveText;
            }
            else if (_isHovered)
            {
                _bg.BackgroundColor = SegmentHoverBg;
                _label.TextColor = SegmentActiveText;
            }
            else
            {
                _bg.BackgroundColor = SegmentIdleBg;
                _label.TextColor = SegmentIdleText;
            }
        }
    }
}
