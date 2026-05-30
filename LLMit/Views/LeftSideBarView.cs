using ZGF.Gui;
using ZGF.Gui.Views;

namespace LLMit.Views;

public sealed class LeftSideBarView : MultiChildView
{
    public LeftSideBarView()
    {
        Width = 300;

        var background = new RectView
        {
            BackgroundColor = 0xFF181818,
            Padding = PaddingStyle.All(8),
            Children =
            {
                new ColumnView
                {
                    Children =
                    {
                        new TextView
                        {
                            Text = "Chat History",
                            TextColor = 0xFFFFFFFF,
                            //HorizontalTextAlignment = TextAlignment.Center,
                        }
                    }
                }
            }
        };

        AddChildToSelf(background);
    }
}