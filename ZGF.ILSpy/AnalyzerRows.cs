using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Analysis;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The analyzer's tree as the tree view reads it, with the view's expansion and selection.
/// Expanding an analysis's row the first time asks for it to run, and it shows a loading row until it
/// is done.</summary>
public sealed class AnalyzerRows : ITreeModel<AnalyzerNode>
{
    private readonly Queue<AppCommand> _appCommands;
    private readonly HashSet<AnalyzerKindNode> _asked = new();
    private readonly AnalyzerMessageNode _loading = new();
    private IReadOnlyList<AnalyzerNode> _roots = Array.Empty<AnalyzerNode>();
    private UiTheme _theme = UiTheme.Dark;
    private NodeIconPalette _iconColors = BuiltInThemes.Dark.Icons;
    private AppIcons _icons = BuiltInIconThemes.Reference.Icons;
    private Strings _strings = Strings.For(Locale.En);
    private int _seenChanges = -1;
    private int _seenFindings = -1;

    /// <param name="appCommands">Where the rows ask for an analysis to run.</param>
    public AnalyzerRows(Queue<AppCommand> appCommands)
    {
        _appCommands = appCommands;
    }

    public TreeViewState<AnalyzerNode> View { get; } = new(ReferenceEqualityComparer.Instance);

    public IReadOnlyList<AnalyzerNode> Roots => _roots;

    public void SetStyle(UiTheme theme, NodeIconPalette iconColors, AppIcons icons, Strings strings)
    {
        _theme = theme;
        _iconColors = iconColors;
        _icons = icons;
        _strings = strings;
    }

    /// <summary>Shows the roots of <paramref name="analyzer"/>, listing the rows again when the roots changed,
    /// dropping a selection whose root is gone, or when an analysis found more.</summary>
    public void Follow(AnalyzerTree analyzer)
    {
        _roots = analyzer.Roots;
        _asked.Clear();
        FollowRoots(analyzer);
        FollowFindings(analyzer);
    }

    private void FollowRoots(AnalyzerTree analyzer)
    {
        if (analyzer.Changes == _seenChanges)
            return;
        _seenChanges = analyzer.Changes;
        View.Refresh();
        DropSelectionOfRemovedRoot(analyzer);
    }

    private void DropSelectionOfRemovedRoot(AnalyzerTree analyzer)
    {
        if (!View.TryGetSelectedRoot(out var root))
            return;
        if (analyzer.Contains(root))
            return;
        View.ClearSelection();
    }

    private void FollowFindings(AnalyzerTree analyzer)
    {
        if (analyzer.Findings == _seenFindings)
            return;
        _seenFindings = analyzer.Findings;
        View.Refresh();
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
        foreach (var root in _roots)
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
                AskToRun(kind);
                return ListResults(kind);
            default:
                return [];
        }
    }

    /// <summary>Asks once for <paramref name="kind"/> to run, unless it already started.</summary>
    private void AskToRun(AnalyzerKindNode kind)
    {
        if (kind.HasStarted)
            return;
        if (!_asked.Add(kind))
            return;
        _appCommands.Enqueue(new AppCommand.RunAnalysis(kind));
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
        var icon = NodeIcons.Icon(symbol.Kind, symbol.Accessibility, _iconColors, _icons);
        return new TreeRow(symbol.Label, icon, Tooltip: symbol.Location, Secondary: symbol.Location);
    }

    private TreeRow KindRow(AnalyzerKindNode kind)
    {
        var icon = new UiIcon(_icons.AnalyzerAnalysis, _theme.TextMuted);
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
        var icon = new UiIcon(_icons.StatusError, _theme.Danger);
        return new TreeRow(message.Error, icon, _theme.Danger);
    }

    private TreeRow LoadingRow()
    {
        var icon = new UiIcon(_icons.StatusLoading, _theme.TextMuted);
        return new TreeRow(_strings.AnalyzerLoading, icon, _theme.TextMuted, Spinning: true);
    }
}
