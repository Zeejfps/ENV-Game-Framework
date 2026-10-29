using System.Diagnostics;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Comparison;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>A comparison's entries as the tree view reads them, with the view's expansion and selection:
/// while <see cref="OnlyDifferences"/>, only the entries that differ or hold one that does, else all of
/// them. Each row is colored by how its entry changed, and says so, and its tooltip shows how the entry is
/// declared on each side. A comparison shown anew opens with its namespaces that differ expanded.</summary>
public sealed class ComparisonRows : ITreeModel<ApiEntry>
{
    private readonly Dictionary<ApiEntry, TreeRow> _rows = new(ReferenceEqualityComparer.Instance);
    private UiTheme _theme = UiTheme.Dark;
    private NodeIconPalette _iconColors = BuiltInThemes.Dark.Icons;
    private AppIcons _icons = BuiltInIconThemes.Reference.Icons;
    private Strings _strings = Strings.For(Locale.En);
    private ApiDiff? _shown;
    private IReadOnlyList<ApiEntry> _roots = [];

    public TreeViewState<ApiEntry> View { get; } = new(ReferenceEqualityComparer.Instance);

    public IReadOnlyList<ApiEntry> Roots => _roots;

    public bool OnlyDifferences { get; private set; } = true;

    /// <summary>Counts up whenever the rows start or stop showing only what differs; it never counts down.</summary>
    public int Changes { get; private set; }

    /// <summary>Draws the rows in <paramref name="theme"/>, <paramref name="icons"/> and <paramref name="strings"/>,
    /// building them again when any of them changed.</summary>
    public void SetStyle(UiTheme theme, NodeIconPalette iconColors, AppIcons icons, Strings strings)
    {
        if (IsStyledAs(theme, iconColors, icons, strings))
            return;
        _theme = theme;
        _iconColors = iconColors;
        _icons = icons;
        _strings = strings;
        _rows.Clear();
    }

    private bool IsStyledAs(UiTheme theme, NodeIconPalette iconColors, AppIcons icons, Strings strings)
    {
        var isStyled = theme == _theme && iconColors == _iconColors;
        if (!isStyled)
            return false;
        return icons == _icons && strings == _strings;
    }

    /// <summary>Shows <paramref name="diff"/>, listing the rows anew when it is not the one shown.</summary>
    public void Follow(ApiDiff? diff)
    {
        if (diff == _shown)
            return;
        _shown = diff;
        _rows.Clear();
        ListRoots();
        ExpandChangedNamespaces();
    }

    /// <summary>Shows only the entries that differ when <paramref name="only"/>, else all of them.</summary>
    public void ShowOnlyDifferences(bool only)
    {
        if (only == OnlyDifferences)
            return;
        OnlyDifferences = only;
        Changes++;
        ListRoots();
    }

    public ComparisonViewRecord Save() => new(OnlyDifferences);

    /// <summary>Shows the rows as <paramref name="saved"/> says they were shown.</summary>
    public void Restore(ComparisonViewRecord saved)
    {
        ShowOnlyDifferences(saved.OnlyDifferences);
    }

    private void ListRoots()
    {
        _roots = ListShown(Namespaces());
        View.Refresh();
    }

    private IReadOnlyList<ApiEntry> Namespaces()
    {
        if (_shown == null)
            return [];
        return _shown.Namespaces;
    }

    private void ExpandChangedNamespaces()
    {
        foreach (var root in _roots)
        {
            if (root.HasChanges)
                View.SetExpanded(root, true);
        }
        View.Refresh();
    }

    public void ExpandAll()
    {
        foreach (var root in _roots)
            ExpandWithChildren(root);
        View.Refresh();
    }

    private void ExpandWithChildren(ApiEntry entry)
    {
        if (!HasChildren(entry))
            return;
        View.SetExpanded(entry, true);
        foreach (var child in GetChildren(entry))
            ExpandWithChildren(child);
    }

    public void CollapseAll() => View.CollapseAll();

    public bool HasChildren(ApiEntry entry)
    {
        if (!OnlyDifferences)
            return entry.Children.Count > 0;
        return HoldsChanges(entry);
    }

    /// <summary>Whether a child of <paramref name="entry"/> differs or holds one that does. Allocates nothing.</summary>
    private static bool HoldsChanges(ApiEntry entry)
    {
        foreach (var child in entry.Children)
        {
            if (child.HasChanges)
                return true;
        }
        return false;
    }

    public IReadOnlyList<ApiEntry> GetChildren(ApiEntry entry) => ListShown(entry.Children);

    TreeChildren<ApiEntry> ITreeModel<ApiEntry>.GetChildren(ApiEntry entry) => TreeChildren<ApiEntry>.Of(GetChildren(entry));

    private IReadOnlyList<ApiEntry> ListShown(IReadOnlyList<ApiEntry> entries)
    {
        if (!OnlyDifferences)
            return entries;
        return entries
            .Where(static e => e.HasChanges)
            .ToList();
    }

    public TreeRow Row(ApiEntry entry)
    {
        if (_rows.TryGetValue(entry, out var row))
            return row;
        row = CreateRow(entry);
        _rows.Add(entry, row);
        return row;
    }

    private TreeRow CreateRow(ApiEntry entry)
    {
        var icon = NodeIcons.Icon(NodeIcons.Of(entry.Shape), entry.Accessibility, _iconColors, _icons);
        var color = ColorOf(entry);
        var tooltip = DescribeDeclarations(entry);
        var change = NameChange(entry.Change);
        return new TreeRow(entry.Text, icon, color, tooltip, change);
    }

    /// <summary>The color of what was added, removed or changed; muted for what is the same throughout,
    /// and the theme's text color for what holds a difference.</summary>
    private uint ColorOf(ApiEntry entry) =>
        entry.Change switch
        {
            ApiChange.Added => _theme.Success,
            ApiChange.Removed => _theme.Danger,
            ApiChange.Changed => _theme.Warning,
            _ => ColorOfUnchanged(entry),
        };

    private uint ColorOfUnchanged(ApiEntry entry)
    {
        if (entry.HasChanges)
            return 0;
        return _theme.TextMuted;
    }

    /// <summary>How the entry is declared: on both sides when it changed, else on the side it is on;
    /// null for a namespace.</summary>
    private string? DescribeDeclarations(ApiEntry entry) =>
        entry switch
        {
            ApiEntry.Namespace => null,
            ApiEntry.Entity entity => DescribeDeclarations(entity.Sides),
            _ => throw new UnreachableException(),
        };

    private string DescribeDeclarations(ApiSides sides) =>
        sides switch
        {
            ApiSides.LeftOnly left => left.Left.Declaration,
            ApiSides.RightOnly right => right.Right.Declaration,
            ApiSides.Both both => DescribeDeclarations(both),
            _ => throw new UnreachableException(),
        };

    private string DescribeDeclarations(ApiSides.Both sides)
    {
        if (sides.Left.Declaration == sides.Right.Declaration)
            return sides.Right.Declaration;
        var left = _strings.ComparisonLeftDeclaration(sides.Left.Declaration);
        var right = _strings.ComparisonRightDeclaration(sides.Right.Declaration);
        return left + Environment.NewLine + right;
    }

    private string? NameChange(ApiChange change) =>
        change switch
        {
            ApiChange.Added => _strings.ComparisonAdded,
            ApiChange.Removed => _strings.ComparisonRemoved,
            ApiChange.Changed => _strings.ComparisonChanged,
            _ => null,
        };
}
