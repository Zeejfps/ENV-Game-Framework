using LLMit.Components.Primitives;
using ZGF.Gui;
using ZGF.Gui.Components;

namespace LLMit.Components;

public sealed record AppScreen : Component
{
    protected override IComponent Build(Context ctx) => new BorderLayout
    {
        West = new LeftSideBar(),
        Center = new CenterArea(),
    };
}
