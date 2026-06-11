using LLMit.ViewModels;
using LLMit.Views;
using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Components;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;

namespace LLMit.Components;

public sealed record ChatTab : Primitive
{
    protected override View CreateView(Context ctx)
    {
        var tab = ctx.Require<ChatTabViewModel>();

        var view = new TabView(ctx.Canvas)
        {
            Text = tab.Title,
        };
        view.Bind(tab.IsActive, isActive => view.IsActive = isActive);
        view.UseController(ctx.Require<InputSystem>(), () => new TabViewController(view));
        return view;
    }
}

public sealed class TabViewController : KeyboardMouseController
{
    private readonly TabView _tabView;

    public TabViewController(TabView tabView)
    {
        _tabView = tabView;
    }

    public override void OnMouseEnter(ref MouseEnterEvent e)
    {
        _tabView.IsHighlighted = true;
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        _tabView.IsHighlighted = false;
    }
}
