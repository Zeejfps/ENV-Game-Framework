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
        
        var textInputController = new ChatInputViewController(textInput);
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

        Controller = new ModelSelectorController(this);
    }
}

public sealed class ModelSelectorController : IKeyboardMouseController
{
    private readonly ModelSelector _modelSelector;
    
    private ContextMenuManager? _contextMenuManager;
    private IOpenedContextMenu? _openedContextMenu;
    
    public ModelSelectorController(ModelSelector modelSelector)
    {
        _modelSelector = modelSelector;
    }
    
    public void OnEnabled(Context context)
    {
        _contextMenuManager = context.Get<ContextMenuManager>();
        context.InputSystem.AddInteractable(this);
    }

    public void OnDisabled(Context context)
    {
        context.InputSystem.RemoveInteractable(this);
    }

    public View View => _modelSelector;
    public void OnMouseEnter(ref MouseEnterEvent e)
    {
    }

    public void OnMouseExit(ref MouseExitEvent e)
    {
        if (_openedContextMenu != null)
        {
            _openedContextMenu.CloseRequest();
        }
    }

    public void OnMouseButtonStateChanged(ref MouseButtonEvent e)
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
            AnchorPoint = View.Position.BottomLeft
        };
        var geminiOption = new ContextMenuItem
        {
            Text = "Gemini",
            IsSelected = true
        };
        geminiOption.Controller =
            new ContextMenuItemDefaultKbmController(contextMenu, geminiOption, _contextMenuManager);
        contextMenu.Children.Add(geminiOption);

        var gpt5Option = new ContextMenuItem
        {
            Text = "GPT 5"
        };
        gpt5Option.Controller = new ContextMenuItemDefaultKbmController(contextMenu, gpt5Option, _contextMenuManager);
        contextMenu.Children.Add(gpt5Option);

        var claudOption = new ContextMenuItem
        {
            Text = "Claude Opus"
        };
        claudOption.Controller = new ContextMenuItemDefaultKbmController(contextMenu, claudOption, _contextMenuManager);
        contextMenu.Children.Add(claudOption);

        _openedContextMenu = _contextMenuManager?.ShowContextMenu(contextMenu);
        _openedContextMenu.Closed += OnContextMenuClosed;
        contextMenu.Controller = new ContextMenuKbmController(_openedContextMenu);
    }

    private void OnContextMenuClosed()
    {
        _openedContextMenu.Closed -= OnContextMenuClosed;
        _openedContextMenu = null;
    }

    public void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
    }

    public void OnMouseMoved(ref MouseMoveEvent e)
    {
    }

    public void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e)
    {
    }

    public void OnFocusLost()
    {
    }

    public void OnFocusGained()
    {
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