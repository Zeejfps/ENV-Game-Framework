using ZGF.Gui.Views;

namespace ZGF.Gui.Widgets;

public sealed record Column : FlexBase
{
    protected override Axis Axis => ZGF.Gui.Views.Axis.Vertical;
}