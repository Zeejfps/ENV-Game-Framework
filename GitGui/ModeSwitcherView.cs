using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

internal sealed class ModeSwitcherView : MultiChildView, IBind<ModeSwitcherViewModel>
{
    private const float PillHeight = 28f;
    private const float PillCornerRadius = 5f;

    private const uint PillBorder = DialogPalette.ButtonBorder;

    private readonly SegmentView _history;
    private readonly SegmentView _localChanges;

    public ModeSwitcherView()
    {
        PreferredHeight = PillHeight;

        const float innerRadius = PillCornerRadius - 1f;
        _history = new SegmentView(
            "History",
            new BorderRadiusStyle { TopRight = innerRadius, BottomRight = innerRadius });
        _localChanges = new SegmentView(
            "Changes",
            new BorderRadiusStyle { TopLeft = innerRadius, BottomLeft = innerRadius });

        var separator = new RectView
        {
            BackgroundColor = PillBorder,
            PreferredWidth = 1f,
        };

        AddChildToSelf(new RectView
        {
            BackgroundColor = 0x00000000u,
            BorderColor = BorderColorStyle.All(PillBorder),
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(PillCornerRadius),
            Children =
            {
                new RowView
                {
                    Children = { _localChanges, separator, _history },
                },
            },
        });

        this.UseViewModel(this);
    }

    public void Bind(ModeSwitcherViewModel vm)
    {
        _history.Bind(vm.HistorySegment);
        _localChanges.Bind(vm.LocalChangesSegment);
    }
}
