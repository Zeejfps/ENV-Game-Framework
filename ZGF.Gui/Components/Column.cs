using ZGF.Gui.Views;

namespace ZGF.Gui.Components;

public sealed record Column : FlexBase
{
    protected override Axis Axis => ZGF.Gui.Views.Axis.Vertical;
}