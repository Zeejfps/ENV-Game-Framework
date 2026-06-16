using ZGF.Geometry;
using ZGF.Gui;

namespace ZGF.Gui.Desktop.Components.ContextMenu;

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

    /// <summary>The menu view built for this popup.</summary>
    ContextMenu Menu { get; }

    /// <summary>The hosting popup window's build context — per-window services
    /// (input system, coordinates) resolve from here.</summary>
    Context Context { get; }

    void CancelCloseRequest();
    void CloseRequest();
}

// Platform-neutral seam for opening context menus. The desktop implementation backs each
// menu with a borderless popup window; other platforms can host menus as in-canvas overlays.
public interface IContextMenuHost
{
    /// <summary>
    /// Opens a context menu. <paramref name="buildMenu"/> builds the menu against the popup
    /// window's own context, so the menu's controllers register with that popup's input
    /// system — menus are built fresh per show and pinned to their popup.
    /// </summary>
    IOpenedContextMenu? ShowContextMenu(Func<Context, ContextMenu> buildMenu, PointI screenAnchor, ContextMenu? parentMenu = null, MenuPlacement placement = MenuPlacement.Below);
    void RequestCloseMenu(ContextMenu menu);
    void RequestCloseAll();
    void CloseAllImmediately();
}
