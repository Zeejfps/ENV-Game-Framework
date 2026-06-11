using ZGF.Gui;
using ZGF.Gui.Components;
using ZGF.Gui.Views;

namespace LLMit.Components.Primitives;

public sealed record BorderLayout : Widget
{
    public IWidget? North { get; init; }
    public IWidget? South { get; init; }
    public IWidget? East { get; init; }
    public IWidget? West { get; init; }
    public IWidget? Center { get; init; }

    protected override View CreateView(Context ctx) => new BorderLayoutView
    {
        North = North?.BuildView(ctx),
        South = South?.BuildView(ctx),
        East = East?.BuildView(ctx),
        West = West?.BuildView(ctx),
        Center = Center?.BuildView(ctx),
    };
}
