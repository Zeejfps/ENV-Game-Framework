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
        
        AddChildToSelf(background);
        AddChildToSelf(new StartNewChatView());
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
    public ModelSelector()
    {
        var background = new RectView
        {
            BorderColor = BorderColorStyle.All(0xFF0493BF),
            BorderSize = BorderSizeStyle.All(1),
            Padding = PaddingStyle.All(4),
            Children =
            {
                new TextView
                {
                    Text = "Gemini",
                    TextColor = 0xFF0493BF,
                    VerticalTextAlignment = TextAlignment.Center,
                }
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
            Children =
            {
                new ModelContextMenuItem("Gemini"),
                new ModelContextMenuItem("GPT 5"),
                new ModelContextMenuItem("Claude Opus"),
            }
        };

        _openedContextMenu = _contextMenuManager.ShowContextMenu(contextMenu);
        _openedContextMenu.Closed += OnContextMenuClosed;
        contextMenu.Controller = new ContextMenuKbmController(_openedContextMenu);
    }

    private void OnContextMenuClosed()
    {
        _openedContextMenu.Closed -= OnContextMenuClosed;
        _openedContextMenu = null;
    }
}

public sealed class ModelContextMenuItem : View
{
    private readonly ContextMenuItem _contextMenuItem;
    
    public ModelContextMenuItem(string model)
    {
        _contextMenuItem = new ContextMenuItem
        {
            Text = model,
        };
        
        AddChildToSelf(_contextMenuItem);
    }

    protected override void OnAttachedToContext(Context context)
    {
        base.OnAttachedToContext(context);
        var contextMenuManager = context.Get<ContextMenuManager>();
        Debug.Assert(contextMenuManager != null);
        Controller = new ContextMenuItemDefaultKbmController(_contextMenuItem, contextMenuManager);
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