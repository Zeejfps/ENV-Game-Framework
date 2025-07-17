using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class ContextMenuManager
{
    private readonly Component _contextMenuPane;

    private HashSet<Component> _closingContextMenus = new();
    
    public ContextMenuManager(Component contextMenuPane)
    {
        _contextMenuPane = contextMenuPane;
    }

    public ContextMenu ShowContextMenu(PointF anchor)
    {
        var contextMenu = new ContextMenu(anchor);
        _contextMenuPane.Add(contextMenu);
        return contextMenu;
    }

    public void SetKeepOpen(ContextMenu contextMenu)
    {
        _closingContextMenus.Remove(contextMenu);
    }
    
    public void HideContextMenu(ContextMenu contextMenu)
    {
        _closingContextMenus.Add(contextMenu);
    }

    public void Update()
    {
        foreach (var contextMenu in _closingContextMenus)
        {
            _contextMenuPane.Remove(contextMenu);
        }
        _closingContextMenus.Clear();
    }
}