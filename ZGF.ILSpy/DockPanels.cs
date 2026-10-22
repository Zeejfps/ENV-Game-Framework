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
    public const string Analyzer = "analyzer";

    /// <summary>Whether <paramref name="panel"/> is one of these views rather than a document.</summary>
    public static bool IsView(string panel) => panel is Assemblies or Search or Bookmarks or Analyzer;

    public static DockTab Tab(Strings t, string panel) => panel switch
    {
        Assemblies => new DockTab(t.PanelAssemblies),
        Bookmarks => new DockTab(t.PanelBookmarks),
        Analyzer => new DockTab(t.PanelAnalyzer),
        _ => new DockTab(t.PanelSearch),
    };

    /// <summary>The saved layout with only its views — documents do not outlive a run — and
    /// <paramref name="document"/> in the place of the documents: in the first
    /// group that held one, so the views docked around them stay where they were. The assemblies
    /// alone when none was saved. <paramref name="placed"/> tells whether the document has a place.</summary>
    public static DockLayout Restore(DockLayout? saved, string document, out bool placed)
    {
        var layout = saved ?? new DockLayout { Root = DockLayout.Group(Assemblies) };
        placed = false;
        foreach (var group in layout.AllGroups())
        {
            var index = group.Panels.FindIndex(static p => !IsView(p));
            if (index < 0) continue;
            var replaced = group.Panels[index];
            group.Panels[index] = document;
            if (group.Active == replaced || group.Active is null || !IsView(group.Active))
                group.Active = document;
            placed = true;
            break;
        }
        layout.Retain(panel => IsView(panel) || panel == document);
        return layout;
    }
}
