using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class VerticalScrollBarView : View
{
    private readonly VerticalScrollBarThumbView _thumbView;
    
    public VerticalScrollBarView()
    {
        PreferredWidth = 25;

        _thumbView = new VerticalScrollBarThumbView();
        
        var slideArea = new RectView
        {
            BackgroundColor = 0xCECECE,
            BorderSize = BorderSizeStyle.All(1),
            BorderColor = new BorderColorStyle
            {
                Left = 0x9C9C9C,
                Top = 0x9C9C9C,
                Right = 0xFFFFFF,
                Bottom = 0xFFFFFF
            },
            Children =
            {
                _thumbView
            }
        };

        var scrollUpButton = new RectView
        {
            Padding = PaddingStyle.All(4),
            BackgroundColor = 0xDEDEDE,
            PreferredHeight = 20,
            BorderSize = BorderSizeStyle.All(1),
            StyleClasses =
            {
                "raised_panel"
            },
            Children =
            {
                new Image
                {
                    ImageUri = "Assets/Icons/arrow_up.png"
                }
            }
        };
        
        var scrollDownButton = new RectView
        {
            Padding = PaddingStyle.All(4),
            BackgroundColor = 0xDEDEDE,
            PreferredHeight = 20,
            BorderSize = BorderSizeStyle.All(1),
            StyleClasses =
            {
                "raised_panel"
            },
            Children =
            {
                new Image
                {
                    ImageUri = "Assets/Icons/arrow_down.png"
                }
            }
        };
        
        AddChildToSelf(new BorderLayoutView
        {
            North = scrollUpButton,
            Center = slideArea,
            South = scrollDownButton
        });
    }

    public float Scale
    {
        get => _thumbView.Scale;
        set => _thumbView.Scale = value;
    }

    public float ScrollNormalized
    {
        get => _thumbView.ScrollPositionNormalized;
        set => _thumbView.ScrollPositionNormalized = value;
    }
}