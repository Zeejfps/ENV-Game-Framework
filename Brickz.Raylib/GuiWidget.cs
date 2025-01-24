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

    private IWidget DimmerWidget(IBuildContext context)
    {
        return new PanelWidget
        {
            Style = new PanelStyle
            {
                BackgroundColor = new Color(0f, 0f, 0f, 0.75f),
            }
        };
    }
    
    protected override IWidget BuildContent(IBuildContext context)
    {
        if (_game.State == GameState.Playing)
            return this;
        
        IWidget panel;
        if (_game.State == GameState.Paused)
        {
            panel = new Column
            {
                Spacing = 10,
                MainAxisSize = MainAxisSize.Min,
                MainAxisAlignment = MainAxisAlignment.Center,
                CrossAxisSize = CrossAxisSize.Min,
                CrossAxisAlignment = CrossAxisAlignment.Center,
                Children =
                {
                    new TextWidget("PAUSED")
                    {
                        Style = new TextStyle
                        {
                            FontScale = 50,
                            Color = new Color(0f, 1f, 0f, 1f),
                            HorizontalTextAlignment = TextAlignment.Center,
                            VerticalTextAlignment = TextAlignment.Center,
                        }
                    },
                    new TextButton("resume")
                    {
                        OnClicked = _game.Resume
                    },
                }
            };
        } 
        else if (_game.State == GameState.Victory)
        {
            panel = new Column
            {
                Spacing = 10,
                MainAxisSize = MainAxisSize.Min,
                MainAxisAlignment = MainAxisAlignment.Center,
                CrossAxisSize = CrossAxisSize.Min,
                CrossAxisAlignment = CrossAxisAlignment.Center,
                Children =
                {
                    new TextWidget("VICTORY!")
                    {
                        Style = new TextStyle
                        {
                            FontScale = 50,
                            Color = new Color(0f, 1f, 0f, 1f),
                            HorizontalTextAlignment = TextAlignment.Center,
                            VerticalTextAlignment = TextAlignment.Center,
                        }
                    },
                    new TextButton("restart")
                    {
                        OnClicked = _game.Restart
                    },
                }
            };
        }
        else
        {
            panel = new Column
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
                    new TextButton("restart")
                    {
                        OnClicked = _game.Restart
                    },
                }
            };
        }

        return new StackWidget
        {
            Children =
            {
                DimmerWidget(context),
                panel
            }
        };
    }
}