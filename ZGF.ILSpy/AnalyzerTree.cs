using System.Diagnostics;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Analysis;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>A row of the analyzer: a symbol, an analysis of one, or a placeholder.</summary>
public abstract class AnalyzerNode;

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
}

/// <summary>One analysis of a symbol, run in the background the first time it is expanded.</summary>
public sealed class AnalyzerKindNode(AnalysisSymbol subject, AnalysisKind kind) : AnalyzerNode
{
    internal readonly List<AnalyzerNode> Results = new();
    internal CancellationTokenSource? Work;

    public AnalysisSymbol Subject { get; } = subject;
    public AnalysisKind Kind { get; } = kind;
    public bool IsStarted { get; internal set; }
    public bool IsDone { get; internal set; }
    public IReadOnlyList<AnalyzerNode> Found => Results;
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
    private readonly Action<Action> _post;
    private readonly Func<AssemblyList> _assemblies;
    private readonly List<AnalyzerSymbolNode> _roots = new();
    private readonly AnalyzerMessageNode _loading = new();
    private UiTheme _theme = UiTheme.Dark;
    private NodeIconPalette _icons = NodeIconPalette.Dark;
    private Strings _strings = Strings.For(Locale.En);

    /// <param name="post">Runs an action on the UI thread.</param>
    /// <param name="assemblies">The open assemblies analyses run over.</param>
    public AnalyzerTree(Action<Action> post, Func<AssemblyList> assemblies)
    {
        _post = post;
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
        Cancel(root);
        View.ClearSelection();
        View.Refresh();
    }

    public void Clear()
    {
        foreach (var root in _roots)
            Cancel(root);
        _roots.Clear();
        Revision++;
        View.ClearSelection();
        View.Refresh();
    }

    public bool HasChildren(AnalyzerNode node) => node switch
    {
        AnalyzerSymbolNode symbol => symbol.Children.Count > 0,
        AnalyzerKindNode kind => !kind.IsDone || kind.Results.Count > 0,
        _ => false,
    };

    public IReadOnlyList<AnalyzerNode> GetChildren(AnalyzerNode node)
    {
        switch (node)
        {
            case AnalyzerSymbolNode symbol:
                return symbol.Children;
            case AnalyzerKindNode kind:
                if (!kind.IsStarted)
                    Start(kind);
                return kind.IsDone ? kind.Results : [..kind.Results, _loading];
            default:
                return [];
        }
    }

    public TreeRow Row(AnalyzerNode node) => node switch
    {
        AnalyzerSymbolNode { Symbol: var s } => new TreeRow(s.Label, NodeIcons.Icon(s.Kind, s.Accessibility, _icons), Tooltip: s.Location,
            Secondary: s.Location),
        AnalyzerKindNode kind => new TreeRow(kind.IsDone ? $"{AnalyzerText.Header(_strings, kind.Kind)} ({kind.Results.Count})" : AnalyzerText.Header(_strings, kind.Kind),
            new UiIcon(LucideIcons.Search, LucideIcons.Family, _theme.TextMuted)),
        AnalyzerMessageNode { Error: { } error } => new TreeRow(error, new UiIcon(LucideIcons.AlertCircle, LucideIcons.Family, _theme.Danger), _theme.Danger),
        _ => new TreeRow(_strings.AnalyzerLoading, new UiIcon(LucideIcons.Loader2, LucideIcons.Family, _theme.TextMuted), _theme.TextMuted),
    };

    private void Start(AnalyzerKindNode kind)
    {
        kind.IsStarted = true;
        var work = new CancellationTokenSource();
        kind.Work = work;
        var assemblies = _assemblies();
        var token = work.Token;
        Task.Run(() =>
        {
            var batch = new List<AnalyzerNode>();
            var lastPost = Stopwatch.StartNew();
            try
            {
                foreach (var result in Analysis.Run(assemblies, kind.Subject, kind.Kind, token))
                {
                    batch.Add(new AnalyzerSymbolNode(result));
                    if (lastPost.ElapsedMilliseconds < 100) continue;
                    Deliver(kind, work, batch, done: false);
                    batch = new List<AnalyzerNode>();
                    lastPost.Restart();
                }
                Deliver(kind, work, batch, done: true);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception e)
            {
                batch.Add(new AnalyzerMessageNode(e.Message));
                Deliver(kind, work, batch, done: true);
            }
        }, token);
    }

    private void Deliver(AnalyzerKindNode kind, CancellationTokenSource work, List<AnalyzerNode> found, bool done) =>
        _post(() =>
        {
            if (kind.Work != work) return;
            kind.Results.AddRange(found);
            if (done)
            {
                kind.IsDone = true;
                kind.Work = null;
                work.Dispose();
            }
            View.Refresh();
        });

    private static void Cancel(AnalyzerNode node)
    {
        if (node is AnalyzerKindNode { Work: { } work } kind)
        {
            kind.Work = null;
            work.Cancel();
        }
        if (node is AnalyzerSymbolNode symbol)
        {
            foreach (var child in symbol.Children)
                Cancel(child);
        }
        else if (node is AnalyzerKindNode results)
        {
            foreach (var child in results.Results)
                Cancel(child);
        }
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
