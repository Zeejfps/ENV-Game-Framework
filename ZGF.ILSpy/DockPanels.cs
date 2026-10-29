using System.Diagnostics;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>A view of the main window's dock space besides the open documents.</summary>
public enum DockView
{
    Assemblies,
    Search,
    Bookmarks,
    Analyzer,
    DecompilerOptions,
    DisplayOptions,
    Exports,
}

/// <summary>The views of the main window's dock space besides the open documents, which share it
/// with them as tabs, and the layout they start in. The layout knows every panel by name: a view by
/// the name below, a document by the name its tab was given.</summary>
public static class DockPanels
{
    public const string Assemblies = "assemblies";
    public const string Search = "search";
    public const string Bookmarks = "bookmarks";
    public const string Analyzer = "analyzer";
    public const string DecompilerOptions = "decompiler-options";
    public const string DisplayOptions = "display-options";
    public const string Exports = "exports";

    /// <summary>The name the layout knows <paramref name="view"/> by.</summary>
    public static string NameOf(DockView view) => view switch
    {
        DockView.Assemblies => Assemblies,
        DockView.Search => Search,
        DockView.Bookmarks => Bookmarks,
        DockView.Analyzer => Analyzer,
        DockView.DecompilerOptions => DecompilerOptions,
        DockView.DisplayOptions => DisplayOptions,
        DockView.Exports => Exports,
        _ => throw new UnreachableException(),
    };

    /// <summary>The view the layout knows as <paramref name="panel"/>, or null for a document.</summary>
    public static DockView? ViewOf(string panel) => panel switch
    {
        Assemblies => DockView.Assemblies,
        Search => DockView.Search,
        Bookmarks => DockView.Bookmarks,
        Analyzer => DockView.Analyzer,
        DecompilerOptions => DockView.DecompilerOptions,
        DisplayOptions => DockView.DisplayOptions,
        Exports => DockView.Exports,
        _ => null,
    };

    /// <summary>Whether <paramref name="panel"/> is one of these views rather than a document.</summary>
    public static bool IsView(string panel) => ViewOf(panel) != null;

    public static DockTab Tab(Strings t, AppIcons icons, DockView view) => new(Title(t, view), Icon: Icon(icons, view));

    private static string Title(Strings t, DockView view) => view switch
    {
        DockView.Assemblies => t.PanelAssemblies,
        DockView.Search => t.PanelSearch,
        DockView.Bookmarks => t.PanelBookmarks,
        DockView.Analyzer => t.PanelAnalyzer,
        DockView.DecompilerOptions => t.PanelDecompilerOptions,
        DockView.DisplayOptions => t.PanelDisplayOptions,
        DockView.Exports => t.PanelExports,
        _ => throw new UnreachableException(),
    };

    /// <summary>The view's icon in <paramref name="icons"/>, on its tab and in the menu that opens it.</summary>
    public static UiIcon Icon(AppIcons icons, DockView view) => new(Glyph(icons, view));

    private static IconGlyph Glyph(AppIcons icons, DockView view) => view switch
    {
        DockView.Assemblies => icons.PanelAssemblies,
        DockView.Search => icons.PanelSearch,
        DockView.Bookmarks => icons.PanelBookmarks,
        DockView.Analyzer => icons.PanelAnalyzer,
        DockView.DecompilerOptions => icons.PanelDecompilerOptions,
        DockView.DisplayOptions => icons.PanelDisplayOptions,
        DockView.Exports => icons.PanelExports,
        _ => throw new UnreachableException(),
    };

    /// <summary>A copy of the saved layout, views and documents, each panel in one place only, or the
    /// assemblies alone when none was saved.</summary>
    public static DockLayout Restore(DockLayout? saved)
    {
        var layout = CopyOrStart(saved);
        layout.Retain(IsNamed);
        return layout;
    }

    private static DockLayout CopyOrStart(DockLayout? saved)
    {
        if (saved == null)
            return new DockLayout { Root = DockLayout.Group(Assemblies) };
        return saved.CopyArrangement();
    }

    private static bool IsNamed(string panel) => panel.Length > 0;

    /// <summary>Puts <paramref name="document"/> in the place of the first document panel of
    /// <paramref name="layout"/> that <paramref name="isShown"/> rejects, in front of its group unless a
    /// view is, so the views docked around the documents stay where they were. False when there is none.</summary>
    public static bool PlaceDocument(DockLayout layout, string document, Func<string, bool> isShown)
    {
        foreach (var group in layout.AllGroups())
        {
            var index = group.Panels.FindIndex(p => IsUnshownDocument(p, isShown));
            if (index < 0)
                continue;
            group.Panels[index] = document;
            if (!HasActiveView(group))
                group.Active = document;
            return true;
        }
        return false;
    }

    private static bool IsUnshownDocument(string panel, Func<string, bool> isShown)
    {
        if (IsView(panel))
            return false;
        return !isShown(panel);
    }

    private static bool HasActiveView(DockGroup group) => group.Active != null && IsView(group.Active);

    /// <summary>Renames the panels of <paramref name="layout"/> that <paramref name="names"/> maps to new names.</summary>
    public static void Rename(DockLayout layout, IReadOnlyDictionary<string, string> names)
    {
        foreach (var group in layout.AllGroups())
            Rename(group, names);
    }

    private static void Rename(DockGroup group, IReadOnlyDictionary<string, string> names)
    {
        for (var i = 0; i < group.Panels.Count; i++)
            group.Panels[i] = NewNameOf(group.Panels[i], names);
        if (group.Active != null)
            group.Active = NewNameOf(group.Active, names);
    }

    private static string NewNameOf(string panel, IReadOnlyDictionary<string, string> names) => names.GetValueOrDefault(panel, panel);
}
