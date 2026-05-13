using System.Diagnostics;
using ZGF.Gui;
using ZGF.Gui.Tests;

namespace LLMit.Views;

public sealed class ModelContextMenuItemView : View
{
    private readonly ContextMenuItem _contextMenuItem;

    public Action<ModelContextMenuItemView>? Chosen { get; set; }
    public string Model { get; }

    public ModelContextMenuItemView(string model)
    {
        Model = model;
        _contextMenuItem = new ContextMenuItem
        {
            Text = model,
            NormalBackgroundColor = 0x00000000,
            SelectedBackgroundColor = 0xFFD00EDE,
            TextColor = 0xFFFFFFFF,
        };

        AddChildToSelf(_contextMenuItem);

        _contextMenuItem.Behaviors.Add(new ContextMenuItemDefaultKbmController(_contextMenuItem, () =>
        {
            var contextMenu = _contextMenuItem.GetParentOfType<ContextMenu>();
            Debug.Assert(contextMenu != null);
            var contextMenuManager = _contextMenuItem.Context?.Get<ContextMenuManager>();
            contextMenuManager?.RequestCloseMenu(contextMenu);
            Chosen?.Invoke(this);
        }));
    }
}