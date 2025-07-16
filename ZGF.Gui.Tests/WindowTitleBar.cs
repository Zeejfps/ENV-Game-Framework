using ZGF.Geometry;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class WindowTitleBar : Component, IHoverable
{
    public WindowTitleBar(Window window)
    {
        Constraints = new RectF
        {
            Height = 20f,
        };

        var background = new Rect
        {
            Style =
            {
                BackgroundColor = 0xCECECE,
                BorderColor = new BorderColorStyle
                {
                    Top = 0xFFFFFF,
                    Left = 0xFFFFFF,
                    Right = 0x9C9C9C,
                },
                BorderSize = new BorderSizeStyle
                {
                    Top = 1,
                    Left = 1,
                    Right = 1,
                },
                Padding = PaddingStyle.All(3)
            }
        };

        var row = new FlexRow
        {
            Gap = 3,
            CrossAxisAlignment = CrossAxisAlignment.Stretch
        };
        background.Add(row);


        var button = new Rect
        {
            Constraints = new RectF
            {
                Width = 13,
            },
            Style =
            {
                BackgroundColor = 0xFF00FF
            }
        };
        button.AddStyleClass("inset_panel");

        var button2 = new Rect
        {
            Constraints = new RectF
            {
                Width = 13f,
            },
            Style =
            {
                BackgroundColor = 0xFF00FF
            }
        };
        button2.AddStyleClass("inset_panel");

        var button3 = new Rect
        {
            Constraints = new RectF
            {
                Width = 13f,
            },
            Style =
            {
                BackgroundColor = 0xFF00FF
            }
        };
        button3.AddStyleClass("inset_panel");

        var spacer = new Rect
        {
            Style =
            {
                BackgroundColor = 0xFF00FF,
            }
        };
        spacer.AddStyleClass("inset_panel");

        row.Add(button);
        row.Add(spacer, new FlexStyle
        {
            Grow = 1f,
        });
        row.Add(button2);
        row.Add(button3);

        Add(background);

        AddMouseListener(this);
    }

    public void HandleMouseEnterEvent()
    {
        Console.WriteLine("OnMouseEnterEvent");
    }

    public void HandleMouseExitEvent()
    {
        Console.WriteLine("OnMouseExitEvent");
    }
}