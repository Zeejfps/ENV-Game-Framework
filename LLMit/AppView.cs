using System.Diagnostics;
using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;

namespace LLMit;

public sealed class AppView : View
{
    public AppView()
    {
        var layout = new BorderLayoutView
        {
            West = new LeftSideBar(),
            Center = new CenterArea(),
        };

        AddChildToSelf(layout);   
    }
}

public sealed class CenterArea : View
{
    public CenterArea()
    {
        var background = new RectView
        {
            BackgroundColor = 0xFF212121
        };

        var layout = new BorderLayoutView
        {
            North = new TabBarView(),
            Center = new RectView
            {
                BorderSize = new BorderSizeStyle
                {
                    Top = 1,
                    Left = 1,
                },
                BorderColor = BorderColorStyle.All(0xFF4f4f4f),
                Children =
                {
                    new StartNewChatView(),
                }
            }
        };

        AddChildToSelf(background);
        AddChildToSelf(layout);
    }
}

public sealed class TabBarView : View
{
    public TabBarView()
    {
        PreferredHeight = 40;

        var bg = new RectView
        {
            BackgroundColor = 0xFF1C1C1C
        };

        var layout = new RowView
        {
            Children =
            {
                new TabView
                {
                    IsHighlighted = true,
                }
            }
        };

        AddChildToSelf(bg);
        AddChildToSelf(layout);
    }
}

public sealed class TabView : View
{
    private bool _isHighlighted;
    public bool IsHighlighted
    {
        get => _isHighlighted;
        set => SetField(ref _isHighlighted, value);
    }

    public TabView()
    {
        PreferredWidth = 150;

        var text = new TextView
        {
            Text = "New Chat",
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            TextColor = 0xFFFFFFFF
        };

        var bg = new RectView
        {
            BackgroundColor = 0xFF212121,
            Padding = PaddingStyle.All(6),
            BorderSize = new BorderSizeStyle
            {
                Top = 1,
                Right = 1,
                Left = 1
            },
            BorderColor = BorderColorStyle.All(0xFF4f4f4f),
            Children =
            {
                text
            }
        };

        AddChildToSelf(bg);
        ZIndex = 10;
    }

    protected override void OnLayoutSelf()
    {
        base.OnLayoutSelf();
        Position = Position with { Bottom = Position.Bottom - 1, Height = Position.Height + 1 };
    }
}

public sealed class StartNewChatView : View
{
    public StartNewChatView()
    {
        var textInput = new TextInputView
        {
            PreferredWidth = 500,
            TextWrap = TextWrap.Wrap,
            TextColor = 0xFFA6A6A6,
            CaretColor = 0xFFA6A6A6,
            SelectionRectColor = 0xAA466583,
        };
        var layout = new CenterView
        {
            Children =
            {
                new ColumnView
                {
                    Gap = 10,
                    Children =
                    {
                        new CenterView
                        {
                            Children =
                            {
                                new RowView
                                {
                                    Gap = 5,
                                    Children =
                                    {
                                        new TextView
                                        {
                                            Text = "What would you like to ask",
                                            TextColor = 0xFFFFFFFF,
                                            VerticalTextAlignment = TextAlignment.Center,
                                            //HorizontalTextAlignment = TextAlignment.Center,
                                        },
                                        new ModelSelector(),
                                        new TextView
                                        {
                                            Text = "?",
                                            TextColor = 0xFFFFFFFF,
                                            VerticalTextAlignment = TextAlignment.Center,
                                        }
                                    }
                                }
                            }
                        },
                        new RectView
                        {
                            Padding = PaddingStyle.All(10),
                            BackgroundColor = 0xFF303030,
                            Children =
                            {
                                textInput
                            }
                        }
                    }
                }
            }
        };
        
        var textInputController = new TextInputViewKbmController(textInput)
        {
            IsMultiLine = true
        };
        textInput.Controller = textInputController;
        
        AddChildToSelf(layout);
    }
}

public sealed class ModelSelector : View
{
    private readonly TextView _textView;

    public string? Model
    {
        get => _textView.Text;
        set => _textView.Text = value;
    }
    
    public ModelSelector()
    {
        _textView = new TextView
        {
            Text = "Gemini",
            TextColor = 0xFF0493BF,
            VerticalTextAlignment = TextAlignment.Center,
        };
        var background = new RectView
        {
            BorderColor = BorderColorStyle.All(0xFF0493BF),
            BorderSize = BorderSizeStyle.All(1),
            Padding = PaddingStyle.All(4),
            Children =
            {
                _textView
            }
        };
        
        AddChildToSelf(background);
    }


    protected override void OnAttachedToContext(Context context)
    {
        base.OnAttachedToContext(context);
        var contextMenuManager = context.Get<ContextMenuManager>();
        Debug.Assert(contextMenuManager != null);
        Controller = new ModelSelectorController(this, contextMenuManager);
    }
}

public sealed class ModelSelectorController : KeyboardMouseController
{
    private readonly ModelSelector _modelSelector;
    private readonly ContextMenuManager _contextMenuManager;
    
    private IOpenedContextMenu? _openedContextMenu;
    
    public ModelSelectorController(ModelSelector modelSelector, ContextMenuManager contextMenuManager)
    {
        _modelSelector = modelSelector;
        _contextMenuManager = contextMenuManager;
    }

    public override View View => _modelSelector;
    
    public override void OnMouseExit(ref MouseExitEvent e)
    {
        if (_openedContextMenu != null)
        {
            _openedContextMenu.CloseRequest();
        }
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;
        
        if (e.Button != MouseButton.Left)
            return;
        
        if (e.State != InputState.Pressed)
            return;

        if (_openedContextMenu != null && _openedContextMenu.IsOpened)
        {
            return;
        }
        
        var contextMenu = new ContextMenu
        {
            AnchorPoint = View.Position.BottomLeft,
            BackgroundColor = 0xFF353535,
            BorderColor = BorderColorStyle.All(0xFF3C3C3C),
            Children =
            {
                new ModelContextMenuItem("Gemini")
                {
                    Chosen = item => _modelSelector.Model = item.Model
                },
                new ModelContextMenuItem("GPT 5")
                {
                    Chosen = item => _modelSelector.Model = item.Model
                },
                new ModelContextMenuItem("Claude Opus")
                {
                    Chosen = item => _modelSelector.Model = item.Model
                },
            }
        };

        _openedContextMenu = _contextMenuManager.ShowContextMenu(contextMenu);
        _openedContextMenu.Closed += OnContextMenuClosed;
        contextMenu.Controller = new ContextMenuKbmController(_openedContextMenu);
    }

    private void OnContextMenuClosed()
    {
        Console.WriteLine("CloseD?");
        _openedContextMenu.Closed -= OnContextMenuClosed;
        _openedContextMenu = null;
    }
}

public sealed class ModelContextMenuItem : View
{
    private readonly ContextMenuItem _contextMenuItem;
    
    public Action<ModelContextMenuItem>? Chosen { get; set; }
    public string Model { get; }

    public ModelContextMenuItem(string model)
    {
        Model = model;
        _contextMenuItem = new ContextMenuItem
        {
            Text = model,
            NormalBackgroundColor = 0x00000000,
            SelectedBackgroundColor = 0xFF4A4A4A,
            TextColor = 0xFFFFFFFF,
        };
        
        AddChildToSelf(_contextMenuItem);
    }

    protected override void OnAttachedToContext(Context context)
    {
        base.OnAttachedToContext(context);
        var contextMenuManager = context.Get<ContextMenuManager>();
        Debug.Assert(contextMenuManager != null);
        Controller = new ContextMenuItemDefaultKbmController(_contextMenuItem, contextMenuManager, () =>
        {
            var contextMenu = _contextMenuItem.GetParentOfType<ContextMenu>();
            Debug.Assert(contextMenu != null);
            contextMenuManager.RequestCloseMenu(contextMenu);
            Chosen?.Invoke(this);
        });
    }
}

public sealed class LeftSideBar : View
{
    public LeftSideBar()
    {
        PreferredWidth = 300;
        
        var background = new RectView
        {
            BackgroundColor = 0xFF181818,
        };
        
        AddChildToSelf(background);
    }
}