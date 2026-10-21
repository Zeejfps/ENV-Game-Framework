using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The views of the main window's dock space besides the open documents, which share it
/// with them as tabs, and the layout they start in.</summary>
public static class DockPanels
{
    public const string Assemblies = "assemblies";
    public const string Search = "search";
    public const string Bookmarks = "bookmarks";

    /// <summary>Whether <paramref name="panel"/> is one of these views rather than a document.</summary>
    public static bool IsView(string panel) => panel is Assemblies or Search or Bookmarks;

    public static DockTab Tab(Strings t, string panel) => panel switch
    {
        Assemblies => new DockTab(t.PanelAssemblies),
        Bookmarks => new DockTab(t.PanelBookmarks),
        _ => new DockTab(t.PanelSearch),
    };

    /// <summary>The saved layout with only the views that outlive a run — documents and search
    /// results do not; the assemblies alone when none was saved.</summary>
    public static DockLayout Restore(DockLayout? saved)
    {
        var layout = saved ?? new DockLayout { Root = DockLayout.Group(Assemblies) };
        layout.Retain(static panel => panel is Assemblies or Bookmarks);
        return layout;
    }
}
