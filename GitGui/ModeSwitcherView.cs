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

    public ModeSwitcherView()
    {
        PreferredHeight = PillHeight;

        const float innerRadius = SegmentCornerRadius - 1f;
        var history = new SegmentView(
            "History",
            new BorderRadiusStyle { TopRight = innerRadius, BottomRight = innerRadius });
        var localChanges = new SegmentView(
            "Changes",
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
                    Children = { localChanges, separator, history },
                },
            },
        });

        this.UseViewModel(
            ctx => new ModeSwitcherViewModel(ctx.Require<State<MainViewMode>>()),
            (vm, subs) =>
            {
                history.Bind(vm.HistorySegment, subs);
                localChanges.Bind(vm.LocalChangesSegment, subs);
            });
    }

    private sealed class SegmentView : MultiChildView
    {
        private readonly RectView _bg;
        private readonly TextView _label;
        private bool _isActive;
        private bool _isHovered;
        private Action? _onClick;

        public SegmentView(string label, BorderRadiusStyle cornerRadius)
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

            this.UseController(_ => new HoverableButtonController(
                () => _onClick?.Invoke(),
                OnHoverChanged));
        }

        public void Bind(SegmentViewModel vm, SubscriptionGroup subs)
        {
            _onClick = vm.Click;
            subs.Add(vm.IsActive.Subscribe(SetActive));
            subs.Add(() => _onClick = null);
        }

        private void SetActive(bool active)
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
