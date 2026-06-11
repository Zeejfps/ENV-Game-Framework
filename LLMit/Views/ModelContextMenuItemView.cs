using System.Diagnostics;
using ZGF.Gui;
using ZGF.Gui.Desktop.Components.ContextMenu;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;

namespace LLMit.Views;

public sealed class ModelContextMenuItemView : MultiChildView
{
    private readonly ContextMenuItem _contextMenuItem;

    public Action<ModelContextMenuItemView>? Chosen { get; set; }
    public string Model { get; }

    public ModelContextMenuItemView(Context context, string model)
    {
        Model = model;
        _contextMenuItem = new ContextMenuItem(context.Canvas)
        {
            Text = model,
            NormalBackgroundColor = 0x00000000,
            SelectedBackgroundColor = 0xFFD00EDE,
            TextColor = 0xFFFFFFFF,
        };

        AddChildToSelf(_contextMenuItem);

        _contextMenuItem.UseController(context.Require<InputSystem>(), () => new ContextMenuItemDefaultKbmController(_contextMenuItem, context, () =>
        {
            var contextMenu = _contextMenuItem.GetParentOfType<ContextMenu>();
            Debug.Assert(contextMenu != null);
            var contextMenuManager = context.Get<IContextMenuHost>();
            contextMenuManager?.RequestCloseMenu(contextMenu);
            Chosen?.Invoke(this);
        }));
    }
}