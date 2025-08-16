using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace LLMit.Views;

public sealed class LeftSideBarView : View
{
    public LeftSideBarView()
    {
        PreferredWidth = 300;

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