using EasyGameFramework.GUI;
using OpenGLSandbox;

namespace Bricks.RaylibBackend;

public sealed class GuiWidget : StatefulWidget
{
    private readonly BrickzGame _game;

    public GuiWidget(BrickzGame game)
    {
        _game = game;
        _game.StateChanged += ()=>
        {
            SetDirty();
        };
        ScreenRect = new Rect(0, 0, 640, 480);
    }
    
    protected override IWidget BuildContent(IBuildContext context)
    {
        if (_game.State == GameState.Defeat)
        {
            return new StackWidget
            {
                Children =
                {
                    new PanelWidget
                    {
                        Style = new PanelStyle
                        {
                            BackgroundColor = new Color(0f, 0f, 0f, 0.75f),
                        }
                    },
                    new Column
                    {
                        Spacing = 10,
                        MainAxisSize = MainAxisSize.Min,
                        MainAxisAlignment = MainAxisAlignment.Center,
                        CrossAxisSize = CrossAxisSize.Min,
                        CrossAxisAlignment = CrossAxisAlignment.Center,
                        Children =
                        {
                            new TextWidget("DEFEAT :(")
                            {
                                Style = new TextStyle
                                {
                                    FontScale = 50,
                                    Color = new Color(1f, 0f, 0f, 1f),
                                    HorizontalTextAlignment = TextAlignment.Center,
                                    VerticalTextAlignment = TextAlignment.Center,
                                }
                            },
                            new TextButton("Restart")
                            {
                                OnClicked = _game.Restart
                            },
                        }
                    }
                }
            };
        }
        return this;
    }
}