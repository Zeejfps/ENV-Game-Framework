using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Analysis;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The analyzer's tree as the tree view reads it, with the view's expansion and selection.
/// An analysis runs in the background the first time its row is expanded, and shows a loading row
/// until it is done.</summary>
public sealed class AnalyzerRows : ITreeModel<AnalyzerNode>
{
    private readonly AnalyzerTree _analyzer;
    private readonly AssemblyList _assemblies;
    private readonly AnalyzerMessageNode _loading = new();
    private UiTheme _theme = UiTheme.Dark;
    private NodeIconPalette _icons = NodeIconPalette.Dark;
    private Strings _strings = Strings.For(Locale.En);
    private int _seenChanges = -1;

    /// <param name="analyzer">The tree the rows show.</param>
    /// <param name="assemblies">The open assemblies analyses run over.</param>
    public AnalyzerRows(AnalyzerTree analyzer, AssemblyList assemblies)
    {
        _analyzer = analyzer;
        _assemblies = assemblies;
    }

    public TreeViewState<AnalyzerNode> View { get; } = new(ReferenceEqualityComparer.Instance);

    public IReadOnlyList<AnalyzerNode> Roots => _analyzer.Roots;

    public void SetStyle(UiTheme theme, NodeIconPalette icons, Strings strings)
    {
        _theme = theme;
        _icons = icons;
        _strings = strings;
    }

    /// <summary>Lists the rows again when the roots changed, dropping a selection whose root is gone.</summary>
    public void Follow()
    {
        if (_analyzer.Changes == _seenChanges)
            return;
        _seenChanges = _analyzer.Changes;
        View.Refresh();
        DropSelectionOfRemovedRoot();
    }

    private void DropSelectionOfRemovedRoot()
    {
        if (!View.TryGetSelectedRoot(out var root))
            return;
        if (_analyzer.Contains(root))
            return;
        View.ClearSelection();
    }

    /// <summary>Analyzes <paramref name="symbol"/>, or finds the root analyzing it already, and shows that root.</summary>
    public void Analyze(AnalysisSymbol symbol)
    {
        var root = _analyzer.Analyze(symbol);
        Show(root);
    }

    /// <summary>Expands <paramref name="root"/> and selects it.</summary>
    public void Show(AnalyzerSymbolNode root)
    {
        View.SetExpanded(root, true);
        View.Refresh();
        View.Select(root);
    }

    /// <summary>The root the selection sits under, or null when nothing showing is selected.</summary>
    public AnalyzerNode? FindSelectedRoot()
    {
        if (!View.TryGetSelectedRoot(out var root))
            return null;
        return root;
    }

    /// <summary>Expands every root found and each analysis of it, running those not run yet.</summary>
    public void ExpandAll()
    {
        foreach (var root in _analyzer.Roots)
        {
            if (root is AnalyzerSymbolNode symbol)
                ExpandWithAnalyses(symbol);
        }
    }

    private void ExpandWithAnalyses(AnalyzerSymbolNode symbol)
    {
        View.SetExpanded(symbol, true);
        foreach (var child in symbol.Children)
        {
            if (child is AnalyzerSymbolNode accessor)
                ExpandWithAnalyses(accessor);
            else
                View.SetExpanded(child, true);
        }
    }

    public void CollapseAll() => View.CollapseAll();

    public bool HasChildren(AnalyzerNode node) => node switch
    {
        AnalyzerSymbolNode symbol => symbol.Children.Count > 0,
        AnalyzerKindNode kind => !kind.IsDone || kind.Found.Count > 0,
        _ => false,
    };

    public IReadOnlyList<AnalyzerNode> GetChildren(AnalyzerNode node)
    {
        switch (node)
        {
            case AnalyzerSymbolNode symbol:
                return symbol.Children;
            case AnalyzerKindNode kind:
                kind.StartOnce(_assemblies, View.Refresh);
                return ListResults(kind);
            default:
                return [];
        }
    }

    private IReadOnlyList<AnalyzerNode> ListResults(AnalyzerKindNode kind)
    {
        if (kind.IsDone)
            return kind.Found;
        var results = new List<AnalyzerNode>(kind.Found);
        results.Add(_loading);
        return results;
    }

    public TreeRow Row(AnalyzerNode node) => node switch
    {
        AnalyzerSymbolNode symbol => SymbolRow(symbol.Symbol),
        AnalyzerKindNode kind => KindRow(kind),
        AnalyzerMessageNode message => MessageRow(message),
        _ => LoadingRow(),
    };

    private TreeRow SymbolRow(AnalysisSymbol symbol)
    {
        var icon = NodeIcons.Icon(symbol.Kind, symbol.Accessibility, _icons);
        return new TreeRow(symbol.Label, icon, Tooltip: symbol.Location, Secondary: symbol.Location);
    }

    private TreeRow KindRow(AnalyzerKindNode kind)
    {
        var icon = new UiIcon(LucideIcons.Search, LucideIcons.Family, _theme.TextMuted);
        return new TreeRow(KindLabel(kind), icon);
    }

    private string KindLabel(AnalyzerKindNode kind)
    {
        var header = AnalyzerText.Header(_strings, kind.Kind);
        if (!kind.IsDone)
            return header;
        return $"{header} ({kind.Found.Count})";
    }

    private TreeRow MessageRow(AnalyzerMessageNode message)
    {
        if (message.Error == null)
            return LoadingRow();
        var icon = new UiIcon(LucideIcons.AlertCircle, LucideIcons.Family, _theme.Danger);
        return new TreeRow(message.Error, icon, _theme.Danger);
    }

    private TreeRow LoadingRow()
    {
        var icon = new UiIcon(LucideIcons.Loader2, LucideIcons.Family, _theme.TextMuted);
        return new TreeRow(_strings.AnalyzerLoading, icon, _theme.TextMuted);
    }
}
