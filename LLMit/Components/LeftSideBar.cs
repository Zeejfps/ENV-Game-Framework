using ZGF.Gui;
using ZGF.Gui.Widgets;

namespace LLMit.Components;

public sealed record LeftSideBar : Widget
{
    protected override IWidget Build(Context ctx) => new Box
    {
        Width = 300,
        Background = 0xFF181818,
        Children =
        [
            new Padding
            {
                Amount = PaddingStyle.All(8),
                Children =
                [
                    new Column
                    {
                        Children =
                        [
                            new Text
                            {
                                Value = "Chat History",
                                Color = 0xFFFFFFFF,
                            },
                        ],
                    },
                ],
            },
        ],
    };
}
