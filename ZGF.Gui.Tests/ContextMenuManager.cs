using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class ContextMenuManager
{
    private Dictionary<ContextMenu, int> _keepOpen = new();
    private readonly Component _contextMenuPane;

    public ContextMenuManager(Component contextMenuPane)
    {
        _contextMenuPane = contextMenuPane;
    }

    public ContextMenu ShowContextMenu(PointF anchor)
    {
        var contextMenu = new ContextMenu(anchor);
        _keepOpen.Add(contextMenu, 1);
        _contextMenuPane.Add(contextMenu);
        return contextMenu;
    }

    public void SetKeepOpen(ContextMenu contextMenu)
    {
        _contextMenuPane.Add(contextMenu);
        _keepOpen[contextMenu]++;
    }
    
    public void HideContextMenu(ContextMenu contextMenu)
    {
        _keepOpen[contextMenu]--;
        if (_keepOpen[contextMenu] == 0)
        {
            _keepOpen.Remove(contextMenu);
            _contextMenuPane.Remove(contextMenu);
        }
    }
}