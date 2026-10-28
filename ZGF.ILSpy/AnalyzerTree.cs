using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Analysis;

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
        foreach (var accessor in Analyzers.AccessorsOf(Symbol))
            children.Add(new AnalyzerSymbolNode(accessor));
        foreach (var kind in Analyzers.KindsFor(Symbol))
            children.Add(new AnalyzerKindNode(Symbol, kind));
        return children;
    }

    /// <summary>Cancels the analyses running under it, leaving children never listed unlisted.</summary>
    internal override void Cancel()
    {
        if (_children == null)
            return;
        foreach (var child in _children)
            child.Cancel();
    }
}

/// <summary>One analysis of a symbol, run in the background the first time it is expanded.</summary>
public sealed class AnalyzerKindNode(AnalysisSymbol subject, AnalysisKind kind) : AnalyzerNode
{
    private readonly List<AnalyzerNode> _found = new();
    private CancellationTokenSource? _work;
    public AnalysisSymbol Subject { get; } = subject;
    public AnalysisKind Kind { get; } = kind;
    public bool IsDone { get; private set; }
    public bool HasStarted { get; private set; }
    public IReadOnlyList<AnalyzerNode> Found => _found;

    /// <summary>Runs the analysis over <paramref name="assemblies"/> unless it already started,
    /// calling <paramref name="changed"/> each time results come in. When it fails, it ends with a row
    /// telling why and hands the exception to <paramref name="failed"/>.</summary>
    internal void StartOnce(AssemblyList assemblies, Action changed, Action<Exception> failed)
    {
        if (HasStarted)
            return;
        HasStarted = true;
        Run(assemblies, changed, failed);
    }

    private async void Run(AssemblyList assemblies, Action changed, Action<Exception> failed)
    {
        var work = new CancellationTokenSource();
        _work = work;
        var token = work.Token;
        try
        {
            await foreach (var batch in Analyzers.RunAsync(assemblies, Subject, Kind, token))
                Deliver(work, CreateNodes(batch), changed);
            Finish(work, [], changed);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception e)
        {
            Finish(work, [new AnalyzerMessageNode(e.Message)], changed);
            failed(e);
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

/// <summary>A root whose symbol is still being found, shown as loading. It saves as the analysis it
/// was restored from, if it was.</summary>
public sealed class AnalyzerPendingNode(CodeAnchor? savedAs) : AnalyzerNode
{
    public CodeAnchor? SavedAs { get; } = savedAs;
}

/// <summary>
/// The analyzer's tree: the symbols the user asked to analyze, newest last, each standing as a
/// <see cref="AnalyzerPendingNode"/> until it is found, with the analyses under them. Results stream
/// in from the background and keep the order the analysis finds them in, or ILSpy's sorting when it
/// asks for one.
/// </summary>
public sealed class AnalyzerTree
{
    private readonly List<AnalyzerNode> _roots = new();

    public IReadOnlyList<AnalyzerNode> Roots => _roots;

    /// <summary>Changes whenever what <see cref="ListSavedAnalyses"/> lists changes.</summary>
    public int Revision { get; private set; }

    /// <summary>Changes whenever a root is added, replaced or removed.</summary>
    public int Changes { get; private set; }

    /// <summary>Changes whenever an analysis under a root finds more or ends.</summary>
    public int Findings { get; private set; }

    internal void NoteFindings()
    {
        Findings++;
    }

    /// <summary>True while a root waits for its symbol to be found.</summary>
    public bool IsFinding
    {
        get
        {
            foreach (var root in _roots)
            {
                if (root is AnalyzerPendingNode)
                    return true;
            }
            return false;
        }
    }

    /// <summary>Adds <paramref name="symbol"/> as a root, unless a root analyzes it already, and
    /// returns the root that does.</summary>
    public AnalyzerSymbolNode AddRoot(AnalysisSymbol symbol)
    {
        var existing = FindRoot(symbol);
        if (existing != null)
            return existing;
        var root = new AnalyzerSymbolNode(symbol);
        _roots.Add(root);
        Revision++;
        Changes++;
        return root;
    }

    private AnalyzerSymbolNode? FindRoot(AnalysisSymbol symbol)
    {
        if (symbol.Reference == null)
            return null;
        foreach (var root in _roots)
        {
            if (root is AnalyzerSymbolNode found && Equals(found.Symbol.Reference, symbol.Reference))
                return found;
        }
        return null;
    }

    /// <summary>Adds a root that waits for its symbol to be found, saved as <paramref name="savedAs"/>
    /// meanwhile, or not saved when it is null.</summary>
    public AnalyzerPendingNode AddPending(CodeAnchor? savedAs)
    {
        var root = new AnalyzerPendingNode(savedAs);
        _roots.Add(root);
        if (savedAs != null)
            Revision++;
        Changes++;
        return root;
    }

    /// <summary>Adds a waiting root for each of <paramref name="analyzed"/>, in order, saved as it.</summary>
    public void AddPending(IReadOnlyList<CodeAnchor> analyzed)
    {
        foreach (var anchor in analyzed)
            AddPending(anchor);
    }

    /// <summary>The roots restored from the saved analyses that are still waiting for their symbols.</summary>
    public List<AnalyzerPendingNode> ListRestoring()
    {
        return _roots
            .OfType<AnalyzerPendingNode>()
            .Where(static p => p.SavedAs != null)
            .ToList();
    }

    /// <summary>Turns the waiting <paramref name="pending"/> into the analysis of <paramref name="symbol"/>,
    /// in its place; when another root analyzes it already, <paramref name="pending"/> goes. Returns
    /// the root that analyzes <paramref name="symbol"/>, or null once <paramref name="pending"/> was removed.</summary>
    public AnalyzerSymbolNode? Resolve(AnalyzerPendingNode pending, AnalysisSymbol symbol)
    {
        var index = _roots.IndexOf(pending);
        if (index < 0)
            return null;
        var existing = FindRoot(symbol);
        if (existing != null)
        {
            Remove(pending);
            return existing;
        }
        var root = new AnalyzerSymbolNode(symbol);
        _roots[index] = root;
        if (symbol.Definition != pending.SavedAs)
            Revision++;
        Changes++;
        return root;
    }

    public bool Contains(AnalyzerNode root) => _roots.Contains(root);

    public void Remove(AnalyzerNode root)
    {
        if (!_roots.Remove(root)) return;
        if (SavedAnalysisOf(root) != null)
            Revision++;
        Changes++;
        root.Cancel();
    }

    /// <summary>The analyses the roots save as, in their order.</summary>
    public List<CodeAnchor> ListSavedAnalyses()
    {
        var saved = new List<CodeAnchor>();
        foreach (var root in _roots)
        {
            var anchor = SavedAnalysisOf(root);
            if (anchor != null)
                saved.Add(anchor.Value);
        }
        return saved;
    }

    private static CodeAnchor? SavedAnalysisOf(AnalyzerNode root) => root switch
    {
        AnalyzerSymbolNode symbol => symbol.Symbol.Definition,
        AnalyzerPendingNode pending => pending.SavedAs,
        _ => null,
    };

    public void Clear()
    {
        foreach (var root in _roots)
            root.Cancel();
        _roots.Clear();
        Revision++;
        Changes++;
    }
}
