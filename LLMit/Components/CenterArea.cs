using LLMit.Components.Primitives;
using LLMit.ViewModels;
using ZGF.Gui;
using ZGF.Gui.Components;

namespace LLMit.Components;

public sealed record CenterArea : Component
{
    protected override IWidget Build(Context ctx)
    {
        var vm = ctx.Require<AppViewModel>();

        return new BorderLayout
        {
            North = new TabBar(),
            Center = new Box
            {
                Background = 0xFF212121,
                BorderSize = new BorderSizeStyle
                {
                    Top = 1,
                    Left = 1,
                },
                BorderColor = BorderColorStyle.All(0xFF4f4f4f),
                Children =
                [
                    new Box
                    {
                        BindVisible = () => vm.IsStartScreenVisible.Value,
                        Children = [new StartNewChat()],
                    },
                ],
            },
        };
    }
}
