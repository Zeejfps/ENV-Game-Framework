using ZGF.Gui;
using ZGF.Gui.Widgets;

namespace LLMit.Components;

public sealed record AppScreen : Widget
{
    protected override IWidget Build(Context ctx) => new BorderLayout
    {
        West = new LeftSideBar(),
        Center = new CenterArea(),
    };
}
