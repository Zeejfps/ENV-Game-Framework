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
    private AnalyzerNode[]? _children;

    public AnalysisSymbol Symbol { get; } = symbol;

    internal IReadOnlyList<AnalyzerNode> Children =>
        _children ??=
        [
            ..Analysis.AccessorsOf(Symbol).Select(static a => new AnalyzerSymbolNode(a)),
            ..Analysis.KindsFor(Symbol).Select(k => new AnalyzerKindNode(Symbol, k)),
        ];

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
        var root = _roots.Find(r => Equals(r.Symbol.Reference, symbol.Reference) && symbol.Reference != null);
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

    public void Remove(AnalyzerSymbolNode root)
    {
        if (!_roots.Remove(root)) return;
        Revision++;
        root.Cancel();
        View.ClearSelection();
        View.Refresh();
    }

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
                return kind.IsDone ? kind.Found : [..kind.Found, _loading];
            default:
                return [];
        }
    }

    public TreeRow Row(AnalyzerNode node) => node switch
    {
        AnalyzerSymbolNode { Symbol: var s } => new TreeRow(s.Label, NodeIcons.Icon(s.Kind, s.Accessibility, _icons), Tooltip: s.Location,
            Secondary: s.Location),
        AnalyzerKindNode kind => new TreeRow(kind.IsDone ? $"{AnalyzerText.Header(_strings, kind.Kind)} ({kind.Found.Count})" : AnalyzerText.Header(_strings, kind.Kind),
            new UiIcon(LucideIcons.Search, LucideIcons.Family, _theme.TextMuted)),
        AnalyzerMessageNode { Error: { } error } => new TreeRow(error, new UiIcon(LucideIcons.AlertCircle, LucideIcons.Family, _theme.Danger), _theme.Danger),
        _ => new TreeRow(_strings.AnalyzerLoading, new UiIcon(LucideIcons.Loader2, LucideIcons.Family, _theme.TextMuted), _theme.TextMuted),
    };
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
