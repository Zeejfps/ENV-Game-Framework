using ZGF.Gui.Views;

namespace ZGF.Gui.Components;

public sealed record Row : FlexBase
{
    protected override Axis Axis => ZGF.Gui.Views.Axis.Horizontal;
}