using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Analysis;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>A row of the analyzer: a symbol, an analysis of one, or a placeholder.</summary>
public abstract class AnalyzerNode
{
    internal virtual void Cancel()
    {
    }
}

/// <summary>A symbol analyzed or found. Its children are its accessors, if it is a property or
/// event, then each analysis that applies to it.</summary>
public sealed class AnalyzerSymbolNode(AnalysisSymbol symbol) : AnalyzerNode
{
    private List<AnalyzerNode>? _children;

    public AnalysisSymbol Symbol { get; } = symbol;

    internal IReadOnlyList<AnalyzerNode> Children
    {
        get
        {
            if (_children == null)
                _children = ListChildren();
            return _children;
        }
    }

    private List<AnalyzerNode> ListChildren()
    {
        var children = new List<AnalyzerNode>();
        foreach (var accessor in Analysis.AccessorsOf(Symbol))
            children.Add(new AnalyzerSymbolNode(accessor));
        foreach (var kind in Analysis.KindsFor(Symbol))
            children.Add(new AnalyzerKindNode(Symbol, kind));
        return children;
    }

    internal override void Cancel()
    {
        foreach (var child in Children)
            child.Cancel();
    }
}

/// <summary>One analysis of a symbol, run in the background the first time it is expanded.</summary>
public sealed class AnalyzerKindNode(AnalysisSymbol subject, AnalysisKind kind) : AnalyzerNode
{
    private readonly List<AnalyzerNode> _found = new();
    private CancellationTokenSource? _work;
    private bool _started;

    public AnalysisSymbol Subject { get; } = subject;
    public AnalysisKind Kind { get; } = kind;
    public bool IsDone { get; private set; }
    public IReadOnlyList<AnalyzerNode> Found => _found;

    /// <summary>Runs the analysis over <paramref name="assemblies"/> unless it already started,
    /// calling <paramref name="changed"/> each time results come in.</summary>
    internal void StartOnce(AssemblyList assemblies, Action changed)
    {
        if (_started)
            return;
        _started = true;
        Run(assemblies, changed);
    }

    private async void Run(AssemblyList assemblies, Action changed)
    {
        var work = new CancellationTokenSource();
        _work = work;
        var token = work.Token;
        try
        {
            await foreach (var batch in Analysis.RunAsync(assemblies, Subject, Kind, token))
                Deliver(work, CreateNodes(batch), changed);
            Finish(work, [], changed);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception e)
        {
            Finish(work, [new AnalyzerMessageNode(e.Message)], changed);
        }
    }

    private static List<AnalyzerNode> CreateNodes(IReadOnlyList<AnalysisSymbol> symbols)
    {
        return symbols
            .Select(static s => (AnalyzerNode)new AnalyzerSymbolNode(s))
            .ToList();
    }

    private void Deliver(CancellationTokenSource work, List<AnalyzerNode> found, Action changed)
    {
        if (_work != work)
            return;
        _found.AddRange(found);
        changed();
    }

    private void Finish(CancellationTokenSource work, List<AnalyzerNode> found, Action changed)
    {
        if (_work != work)
            return;
        _found.AddRange(found);
        IsDone = true;
        _work = null;
        work.Dispose();
        changed();
    }

    internal override void Cancel()
    {
        CancelRun();
        foreach (var child in _found)
            child.Cancel();
    }

    private void CancelRun()
    {
        var work = _work;
        if (work == null)
            return;
        _work = null;
        work.Cancel();
    }
}

/// <summary>Stands in the results while an analysis runs, or tells why it failed.</summary>
public sealed class AnalyzerMessageNode(string? error = null) : AnalyzerNode
{
    public string? Error { get; } = error;
}

/// <summary>
/// The analyzer's tree: the symbols the user asked to analyze, newest last, and the model the
/// tree view reads. Results stream in from the background and keep the order the analysis finds
/// them in, or ILSpy's sorting when it asks for one.
/// </summary>
public sealed class AnalyzerTree : ITreeModel<AnalyzerNode>
{
    private readonly AssemblyList _assemblies;
    private readonly List<AnalyzerSymbolNode> _roots = new();
    private readonly AnalyzerMessageNode _loading = new();
    private UiTheme _theme = UiTheme.Dark;
    private NodeIconPalette _icons = NodeIconPalette.Dark;
    private Strings _strings = Strings.For(Locale.En);

    /// <param name="assemblies">The open assemblies analyses run over.</param>
    public AnalyzerTree(AssemblyList assemblies)
    {
        _assemblies = assemblies;
    }

    public TreeViewState<AnalyzerNode> View { get; } = new(ReferenceEqualityComparer.Instance);

    public IReadOnlyList<AnalyzerSymbolNode> Roots => _roots;

    /// <summary>Changes whenever a root is added or removed.</summary>
    public int Revision { get; private set; }

    public void SetStyle(UiTheme theme, NodeIconPalette icons, Strings strings)
    {
        _theme = theme;
        _icons = icons;
        _strings = strings;
    }

    /// <summary>Adds <paramref name="symbol"/> as a root, expanded and selected, or selects the root
    /// already analyzing it.</summary>
    public void Analyze(AnalysisSymbol symbol)
    {
        var root = FindRoot(symbol);
        if (root == null)
        {
            root = new AnalyzerSymbolNode(symbol);
            _roots.Add(root);
            Revision++;
        }
        View.SetExpanded(root, true);
        View.Refresh();
        View.Select(root);
    }

    private AnalyzerSymbolNode? FindRoot(AnalysisSymbol symbol)
    {
        if (symbol.Reference == null)
            return null;
        return _roots.Find(r => Equals(r.Symbol.Reference, symbol.Reference));
    }

    public void Remove(AnalyzerSymbolNode root)
    {
        if (!_roots.Remove(root)) return;
        Revision++;
        root.Cancel();
        View.ClearSelection();
        View.Refresh();
    }

    /// <summary>The root the selection sits under, or null when nothing showing is selected.</summary>
    public AnalyzerSymbolNode? FindSelectedRoot()
    {
        if (!View.TryGetSelectedRoot(out var root))
            return null;
        return root as AnalyzerSymbolNode;
    }

    /// <summary>The selected symbol when it is a result rather than a root, to analyze on its own.</summary>
    public AnalyzerSymbolNode? FindSelectedResult()
    {
        if (View.Selected is not AnalyzerSymbolNode symbol)
            return null;
        if (_roots.Contains(symbol))
            return null;
        return symbol;
    }

    /// <summary>Expands every root and each analysis of it, running those not run yet.</summary>
    public void ExpandAll()
    {
        foreach (var root in _roots)
            ExpandWithAnalyses(root);
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

    public void Clear()
    {
        foreach (var root in _roots)
            root.Cancel();
        _roots.Clear();
        Revision++;
        View.ClearSelection();
        View.Refresh();
    }

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

/// <summary>The analyzer's words in the interface language.</summary>
internal static class AnalyzerText
{
    public static string Header(Strings t, AnalysisKind kind) => kind.Header switch
    {
        "Applied To" => t.AnalyzerAppliedTo,
        "Implemented By" => t.AnalyzerImplementedBy,
        "Overridden By" => t.AnalyzerOverriddenBy,
        "Read By" => t.AnalyzerReadBy,
        "Assigned By" => t.AnalyzerAssignedBy,
        "Address Taken By" => t.AnalyzerAddressTakenBy,
        "Implements" => t.AnalyzerImplements,
        "Overrides" => t.AnalyzerOverrides,
        "Used By" => t.AnalyzerUsedBy,
        "Uses" => t.AnalyzerUses,
        "Exposed By" => t.AnalyzerExposedBy,
        "Extension Methods" => t.AnalyzerExtensionMethods,
        "Instantiated By" => t.AnalyzerInstantiatedBy,
        _ => kind.Header,
    };
}
