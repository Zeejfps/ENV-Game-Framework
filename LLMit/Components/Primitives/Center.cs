using ZGF.Gui;
using ZGF.Gui.Components;
using ZGF.Gui.Views;

namespace LLMit.Components.Primitives;

public sealed record Center : Primitive
{
    public required IComponent Child { get; init; }

    protected override View CreateView(Context ctx) => new CenterView
    {
        Children = { Child.BuildView(ctx) },
    };
}
