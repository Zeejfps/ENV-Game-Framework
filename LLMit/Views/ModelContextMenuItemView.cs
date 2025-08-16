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