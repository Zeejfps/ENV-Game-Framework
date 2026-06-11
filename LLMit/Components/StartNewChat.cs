using LLMit.Components.Primitives;
using ZGF.Gui;
using ZGF.Gui.Components;

namespace LLMit.Components;

public sealed record StartNewChat : Component
{
    protected override IComponent Build(Context ctx) => new Center
    {
        Child = new Column
        {
            Gap = 10,
            Children =
            [
                new Center
                {
                    Child = new Row
                    {
                        Gap = 5,
                        Children =
                        [
                            new Text
                            {
                                Value = "What would you like to ask",
                                Color = 0xFFFFFFFF,
                                VAlign = TextAlignment.Center,
                            },
                            new ModelSelector(),
                            new Text
                            {
                                Value = "?",
                                Color = 0xFFFFFFFF,
                                VAlign = TextAlignment.Center,
                            },
                        ],
                    },
                },
                new ChatTextInput(),
            ],
        },
    };
}
