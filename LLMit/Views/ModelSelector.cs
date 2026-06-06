using System.Diagnostics;
using ZGF.Gui;
using ZGF.Gui.Desktop;
using ZGF.Gui.Tests;
using ZGF.Gui.Views;

namespace LLMit.Views;

public sealed class ModelSelector : MultiChildView
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


}

public sealed class ModelSelectorController : KeyboardMouseController
{
    private readonly ModelSelector _modelSelector;
    private readonly IContextMenuHost _contextMenuManager;

    private IOpenedContextMenu? _openedContextMenu;
    private InputSystem? _inputSystem;
    private ContextMenu? _registeredContextMenu;

    public ModelSelectorController(ModelSelector modelSelector, Context context)
    {
        _modelSelector = modelSelector;
        _contextMenuManager = context.Get<IContextMenuHost>()!;
        Debug.Assert(_contextMenuManager != null);
    }

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
            BackgroundColor = 0xFF353535,
            BorderColor = BorderColorStyle.All(0xFF3C3C3C),
            Children =
            {
                new ModelContextMenuItemView("Gemini")
                {
                    Chosen = item => _modelSelector.Model = item.Model
                },
                new ModelContextMenuItemView("GPT 5")
                {
                    Chosen = item => _modelSelector.Model = item.Model
                },
                new ModelContextMenuItemView("Claude Opus")
                {
                    Chosen = item => _modelSelector.Model = item.Model
                },
            }
        };

        var coords = _modelSelector.Context?.Get<IWindowCoordinates>();
        var screen = coords != null
            ? coords.ToScreenPoints(_modelSelector.Position.BottomLeft)
            : default;
        _openedContextMenu = _contextMenuManager.ShowContextMenu(contextMenu, screen);
        _openedContextMenu.Closed += OnContextMenuClosed;
        _inputSystem = _modelSelector.Context?.Get<InputSystem>();
        _inputSystem?.RegisterController(contextMenu, new ContextMenuKbmController(_openedContextMenu));
        _registeredContextMenu = contextMenu;
    }

    private void OnContextMenuClosed()
    {
        Console.WriteLine("CloseD?");
        _openedContextMenu.Closed -= OnContextMenuClosed;
        _openedContextMenu = null;
        if (_registeredContextMenu != null)
        {
            _inputSystem?.UnregisterController(_registeredContextMenu);
            _registeredContextMenu = null;
        }
        _inputSystem = null;
    }
}