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
    public const string DecompilerOptions = "decompiler-options";
    public const string DisplayOptions = "display-options";
    public const string Exports = "exports";

    /// <summary>Whether <paramref name="panel"/> is one of these views rather than a document.</summary>
    public static bool IsView(string panel) => panel is Assemblies or Search or Bookmarks or Analyzer or DecompilerOptions or DisplayOptions or Exports;

    public static DockTab Tab(Strings t, AppIcons icons, string panel) => new(Title(t, panel), Icon: Icon(icons, panel));

    private static string Title(Strings t, string panel) => panel switch
    {
        Assemblies => t.PanelAssemblies,
        Bookmarks => t.PanelBookmarks,
        Analyzer => t.PanelAnalyzer,
        DecompilerOptions => t.PanelDecompilerOptions,
        DisplayOptions => t.PanelDisplayOptions,
        Exports => t.PanelExports,
        _ => t.PanelSearch,
    };

    /// <summary>The view's icon in <paramref name="icons"/>, on its tab and in the menu that opens it.</summary>
    public static UiIcon Icon(AppIcons icons, string panel) => new(Glyph(icons, panel));

    private static IconGlyph Glyph(AppIcons icons, string panel) => panel switch
    {
        Assemblies => icons.PanelAssemblies,
        Bookmarks => icons.PanelBookmarks,
        Analyzer => icons.PanelAnalyzer,
        DecompilerOptions => icons.PanelDecompilerOptions,
        DisplayOptions => icons.PanelDisplayOptions,
        Exports => icons.PanelExports,
        _ => icons.PanelSearch,
    };

    /// <summary>A copy of the saved layout with only its views — documents do not outlive a run — and
    /// <paramref name="document"/> in the place of the documents: in the first
    /// group that held one, so the views docked around them stay where they were. The assemblies
    /// alone when none was saved. <paramref name="placed"/> tells whether the document has a place.</summary>
    public static DockLayout Restore(DockLayout? saved, string document, out bool placed)
    {
        var layout = CopyOrStart(saved);
        placed = PlaceDocument(layout, document);
        layout.Retain(panel => IsView(panel) || panel == document);
        return layout;
    }

    private static DockLayout CopyOrStart(DockLayout? saved)
    {
        if (saved == null)
            return new DockLayout { Root = DockLayout.Group(Assemblies) };
        return saved.CopyArrangement();
    }

    private static bool PlaceDocument(DockLayout layout, string document)
    {
        foreach (var group in layout.AllGroups())
        {
            var index = group.Panels.FindIndex(static p => !IsView(p));
            if (index < 0)
                continue;
            group.Panels[index] = document;
            if (!HasActiveView(group))
                group.Active = document;
            return true;
        }
        return false;
    }

    private static bool HasActiveView(DockGroup group) => group.Active != null && IsView(group.Active);
}
