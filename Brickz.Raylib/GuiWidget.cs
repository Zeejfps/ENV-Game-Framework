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
    }
    
    protected override IWidget Build(IBuildContext context)
    {
        if (_game.State == GameState.Defeat)
        {
            return new StackWidget
            {
                ScreenRect = new Rect(0, 0, 640, 480),
                Children =
                {
                    new PanelWidget
                    {
                        Style = new PanelStyle
                        {
                            BackgroundColor = new OpenGLSandbox.Color(0f, 0f, 0f, 0.75f),
                        }
                    },
                    new Column
                    {
                        Spacing = 10,
                        MainAxisSize = MainAxisSize.Min,
                        MainAxisAlignment = MainAxisAlignment.Center,
                        Children =
                        {
                            new TextWidget("DEFEAT :(")
                            {
                                Style = new TextStyle
                                {
                                    FontScale = 50,
                                    Color = new OpenGLSandbox.Color(1f, 0f, 0f, 1f),
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
        else
        {
            
        }
        return this;
    }
}