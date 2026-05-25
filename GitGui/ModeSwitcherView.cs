using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public sealed class ModeSwitcherView : MultiChildView
{
    private const float PillHeight = 28f;
    private const float PillCornerRadius = 5f;

    private const uint PillBorder = DialogPalette.ButtonBorder;

    public ModeSwitcherView()
    {
        PreferredHeight = PillHeight;

        const float innerRadius = PillCornerRadius - 1f;
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
            BackgroundColor = 0x00000000u,
            BorderColor = BorderColorStyle.All(PillBorder),
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(PillCornerRadius),
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
            vm =>
            {
                history.Bind(vm.HistorySegment);
                localChanges.Bind(vm.LocalChangesSegment);
            });
    }
}
