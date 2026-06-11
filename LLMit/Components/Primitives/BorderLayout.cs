using ZGF.Gui;
using ZGF.Gui.Components;
using ZGF.Gui.Views;

namespace LLMit.Components.Primitives;

public sealed record BorderLayout : Primitive
{
    public IComponent? North { get; init; }
    public IComponent? South { get; init; }
    public IComponent? East { get; init; }
    public IComponent? West { get; init; }
    public IComponent? Center { get; init; }

    protected override View CreateView(Context ctx) => new BorderLayoutView
    {
        North = North?.BuildView(ctx),
        South = South?.BuildView(ctx),
        East = East?.BuildView(ctx),
        West = West?.BuildView(ctx),
        Center = Center?.BuildView(ctx),
    };
}
