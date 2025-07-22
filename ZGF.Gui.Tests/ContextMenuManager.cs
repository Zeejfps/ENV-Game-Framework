using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class ContextMenuManager
{
    private readonly Component _contextMenuPane;
    private readonly Dictionary<Component, long> _closingContextMenus = new();
    
    public ContextMenuManager(Component contextMenuPane)
    {
        _contextMenuPane = contextMenuPane;
    }

    public ContextMenu ShowContextMenu(PointF anchor, ContextMenu? parentMenu = null)
    {
        var contextMenu = new ContextMenu(anchor, parentMenu);
        contextMenu.AddController(new ContextMenuDefaultKbmController(contextMenu));
        _contextMenuPane.Add(contextMenu);
        return contextMenu;
    }

    public void SetKeepOpen(ContextMenu contextMenu)
    {
        _closingContextMenus.Remove(contextMenu);
        var parentMenu = contextMenu.ParentMenu;
        while (parentMenu is not null)
        {
            _closingContextMenus.Remove(parentMenu);
            parentMenu = parentMenu.ParentMenu;
        }
    }
    
    public void HideContextMenu(ContextMenu contextMenu)
    {
        var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        _closingContextMenus[contextMenu] = timestamp;
        var parentMenu = contextMenu.ParentMenu;
        while (parentMenu is not null)
        {
            _closingContextMenus[parentMenu] = timestamp;
            parentMenu = parentMenu.ParentMenu;
        }
    }

    public void Update()
    {
        var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        foreach (var kvp in _closingContextMenus.ToList())
        {
            var contextMenu = kvp.Key;
            var timestamp = kvp.Value;
            if (now - timestamp > 100)
            {
                _contextMenuPane.Remove(contextMenu);
                _closingContextMenus.Remove(contextMenu);
            }
        }
    }
}