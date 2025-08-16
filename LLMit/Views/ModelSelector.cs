using System.Diagnostics;
using ZGF.Gui;
using ZGF.Gui.Tests;

namespace LLMit.Views;

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