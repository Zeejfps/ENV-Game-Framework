using ZGF.Geometry;

namespace ZGF.Gui;

// Which way a context menu grows from its anchor. Below (default) grows downward from the
// anchor; Above grows upward, for triggers near the bottom of the screen.
public enum MenuPlacement
{
    Below,
    Above,
}

public interface IOpenedContextMenu
{
    event Action Closed;
    bool IsOpened { get; }
    void CancelCloseRequest();
    void CloseRequest();
}

// Platform-neutral seam for opening context menus. The desktop implementation backs each
// menu with a borderless popup window; other platforms can host menus as in-canvas overlays.
public interface IContextMenuHost
{
    IOpenedContextMenu? ShowContextMenu(ContextMenu menu, PointI screenAnchor, ContextMenu? parentMenu = null, MenuPlacement placement = MenuPlacement.Below);
    void RequestCloseMenu(ContextMenu menu);
}
