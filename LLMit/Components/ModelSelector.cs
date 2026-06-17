using System.Diagnostics;
using LLMit.ViewModels;
using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Desktop;
using ZGF.Gui.Desktop.Components.ContextMenu;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;
using ZGF.Gui.Widgets;

namespace LLMit.Components;

public sealed record ModelSelector : Widget
{
    protected override View CreateView(Context ctx)
    {
        var vm = ctx.Require<AppViewModel>();

        var textView = new TextView(ctx.Canvas)
        {
            TextColor = 0xFF0493BF,
            VerticalTextAlignment = TextAlignment.Center,
        };
        textView.BindText(() => vm.SelectedModel.Value);

        var view = new RectView
        {
            BorderColor = BorderColorStyle.All(0xFF0493BF),
            BorderSize = BorderSizeStyle.All(1),
            Children =
            {
                new PaddingView
                {
                    Padding = PaddingStyle.All(4),
                    Children =
                    {
                        textView
                    },
                }
            },
        };

        view.UseController(ctx.Require<InputSystem>(), () => new ModelSelectorController(view, ctx, vm));
        return view;
    }
}

public sealed class ModelSelectorController : KeyboardMouseController
{
    private readonly View _anchor;
    private readonly AppViewModel _vm;
    private readonly IContextMenuHost _contextMenuManager;
    private readonly IWindowCoordinates? _coordinates;

    private IOpenedContextMenu? _openedContextMenu;
    private InputSystem? _inputSystem;
    private ContextMenu? _registeredContextMenu;

    public ModelSelectorController(View anchor, Context context, AppViewModel vm)
    {
        _anchor = anchor;
        _vm = vm;
        _contextMenuManager = context.Get<IContextMenuHost>()!;
        _coordinates = context.Get<IWindowCoordinates>();
        Debug.Assert(_contextMenuManager != null);
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        _openedContextMenu?.CloseRequest();
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

        var screen = _coordinates != null
            ? _coordinates.ToScreenPoints(_anchor.Position.BottomLeft)
            : default;
        _openedContextMenu = _contextMenuManager.ShowContextMenu(BuildContextMenu, screen);
        if (_openedContextMenu == null)
            return;

        _openedContextMenu.Closed += OnContextMenuClosed;
        _inputSystem = _openedContextMenu.Context.Get<InputSystem>();
        _inputSystem?.RegisterController(_openedContextMenu.Menu, new ContextMenuKbmController(_openedContextMenu));
        _registeredContextMenu = _openedContextMenu.Menu;
    }

    private ContextMenu BuildContextMenu(Context popupContext)
    {
        var menu = new ContextMenu
        {
            BackgroundColor = 0xFF353535,
            BorderColor = BorderColorStyle.All(0xFF3C3C3C),
        };

        foreach (var model in _vm.AvailableModels)
        {
            menu.Children.Add(BuildMenuItem(popupContext, model));
        }

        return menu;
    }

    private ContextMenuItem BuildMenuItem(Context popupContext, string model)
    {
        var item = new ContextMenuItem(popupContext.Canvas)
        {
            Text = model,
            NormalBackgroundColor = 0x00000000,
            SelectedBackgroundColor = 0xFFD00EDE,
            TextColor = 0xFFFFFFFF,
        };

        item.UseController(popupContext.Require<InputSystem>(), () => new ContextMenuItemDefaultKbmController(item, popupContext, clicked: () =>
        {
            var contextMenu = item.GetParentOfType<ContextMenu>();
            Debug.Assert(contextMenu != null);
            popupContext.Get<IContextMenuHost>()?.RequestCloseMenu(contextMenu);
            _vm.SelectedModel.Value = model;
        }));

        return item;
    }

    private void OnContextMenuClosed()
    {
        _openedContextMenu!.Closed -= OnContextMenuClosed;
        _openedContextMenu = null;
        if (_registeredContextMenu != null)
        {
            _inputSystem?.UnregisterController(_registeredContextMenu);
            _registeredContextMenu = null;
        }
        _inputSystem = null;
    }
}
