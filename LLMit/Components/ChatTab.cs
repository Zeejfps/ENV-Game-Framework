using LLMit.ViewModels;
using LLMit.Views;
using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Desktop.Widgets;
using ZGF.Gui.Widgets;

namespace LLMit.Components;

public sealed record ChatTab : Widget
{
    protected override IWidget Build(Context ctx)
    {
        var tab = ctx.Require<ChatTabViewModel>();

        var view = new TabView(ctx.Canvas)
        {
            Text = tab.Title,
        };
        view.Bind(tab.IsActive, isActive => view.IsActive = isActive);

        return new KbmInput
        {
            OnHoverEnter = () => view.IsHighlighted = true,
            OnHoverExit = () => view.IsHighlighted = false,
            Child = new Raw { View = view },
        };
    }
}
