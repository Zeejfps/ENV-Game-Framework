using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.ILSpy;

/// <summary>The panels of the main window's dock space and the layout they start in.</summary>
public static class DockPanels
{
    public const string Assemblies = "assemblies";
    public const string Documents = "documents";
    public const string Search = "search";

    public static DockTab Tab(string panel) => panel switch
    {
        Assemblies => new DockTab("Assemblies", Closable: false),
        Documents => new DockTab("Documents", Closable: false, Movable: false),
        _ => new DockTab("Search"),
    };

    /// <summary>The saved layout without the panels this version does not have or that start
    /// closed, and with the ones it cannot do without; the default layout when none was saved.</summary>
    public static DockLayout Restore(DockLayout? saved)
    {
        var layout = saved ?? new DockLayout
        {
            Root = DockLayout.Split(Direction.Horizontal, 0.34f, DockLayout.Group(Assemblies), DockLayout.Group(Documents)),
        };
        layout.Retain(static panel => panel is Assemblies or Documents);
        if (!layout.Contains(Documents))
            layout.Dock(Documents, layout.Root, DockEdge.Right, 0.66f);
        if (!layout.Contains(Assemblies))
            layout.Dock(Assemblies, layout.GroupOf(Documents)!, DockEdge.Left, 0.34f);
        return layout;
    }
}
