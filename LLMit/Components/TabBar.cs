using LLMit.ViewModels;
using ZGF.Gui;
using ZGF.Gui.Components;
using ZGF.Gui.Views;

namespace LLMit.Components;

public sealed record TabBar : Component
{
    protected override IComponent Build(Context ctx)
    {
        var vm = ctx.Require<AppViewModel>();

        return new Box
        {
            Height = 40,
            Background = 0xFF1C1C1C,
            Children =
            [
                Each.Of(vm.Tabs, new ChatTab(), axis: Axis.Horizontal),
            ],
        };
    }
}
