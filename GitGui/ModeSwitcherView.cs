using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

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

    private readonly SegmentView _history;
    private readonly SegmentView _localChanges;

    private ModeSwitcherViewModel? _vm;

    public ModeSwitcherView()
    {
        PreferredHeight = PillHeight;

        const float innerRadius = SegmentCornerRadius - 1f;
        _history = new SegmentView(
            "History",
            () => _vm?.Activate(MainViewMode.History),
            new BorderRadiusStyle { TopRight = innerRadius, BottomRight = innerRadius });
        _localChanges = new SegmentView(
            "Changes",
            () => _vm?.Activate(MainViewMode.LocalChanges),
            new BorderRadiusStyle { TopLeft = innerRadius, BottomLeft = innerRadius });

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

        this.UseViewModel(
            ctx => new ModeSwitcherViewModel(ctx.Require<State<MainViewMode>>()),
            (vm, subs) =>
            {
                _vm = vm;
                subs.Add(vm.Mode.Subscribe(OnModeChanged));
            });
    }

    private void OnModeChanged(MainViewMode mode)
    {
        _history.SetActive(mode == MainViewMode.History);
        _localChanges.SetActive(mode == MainViewMode.LocalChanges);
    }

    private sealed class SegmentView : MultiChildView
    {
        private readonly RectView _bg;
        private readonly TextView _label;
        private bool _isActive;
        private bool _isHovered;

        public SegmentView(string label, Action onClick, BorderRadiusStyle cornerRadius)
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
                BorderRadius = cornerRadius,
                Padding = new PaddingStyle { Left = 12, Right = 12 },
                Children = { _label },
            };
            AddChildToSelf(_bg);

            this.UseController(_ => new HoverableButtonController(onClick, OnHoverChanged));
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
