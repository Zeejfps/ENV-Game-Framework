using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>An open tab: where it points, the node found there, and its decompiled document, which
/// arrives from a background decompilation. Mutated on the UI thread only.</summary>
public sealed class DocumentTab
{
    private static int _nextId;

    private readonly Action<Action> _post;
    private readonly Func<Strings> _strings;
    private readonly Action<string, string> _reportError;
    private CancellationTokenSource? _work;
    private CodeViewPosition? _restore;
    private Bookmark? _target;
    private string? _title;

    /// <param name="post">Runs an action on the UI thread.</param>
    /// <param name="strings">The interface's strings in the current language.</param>
    /// <param name="reportError">Tells the user that decompiling failed: a title and details.</param>
    public DocumentTab(Action<Action> post, Func<Strings> strings, Action<string, string> reportError)
    {
        _post = post;
        _strings = strings;
        _reportError = reportError;
        Panel = $"document-{Id}";
    }

    public int Id { get; } = Interlocked.Increment(ref _nextId);

    /// <summary>The tab's panel in <see cref="Workspace.Layout"/>.</summary>
    public string Panel { get; }

    /// <summary>Where the tab points; outlives reloads of the assembly.</summary>
    public NodePath? Location { get; private set; }

    /// <summary>The node shown, or null while <see cref="Location"/> is being found again after a
    /// reload, or when it no longer exists.</summary>
    public BrowseNode? Node { get; private set; }

    public DecompilationLanguage Language { get; private set; }
    public DecompiledDocument Document { get; private set; } = DecompiledDocument.Empty;
    public CodeViewState View { get; } = new();
    public bool IsBusy => _work != null;
    public string Title => Node != null ? NodeText.Name(Node) : _title ?? _strings().DocumentNewTab;

    /// <summary>The full identity of what the tab shows.</summary>
    public string Tooltip => Node != null ? NodeText.Tooltip(Node) : Title;

    /// <summary>Changes whenever the tab is pointed somewhere, or loses its node.</summary>
    public int Version { get; private set; }

    /// <summary>Starts decompiling <paramref name="node"/>, cancelling what was in flight, and shows a
    /// placeholder until it is done, then returns the view to <paramref name="position"/>, else
    /// reveals the line <paramref name="target"/> marks, else the node's definition. When the tab
    /// already shows <paramref name="node"/> in <paramref name="language"/>, only does the latter.</summary>
    public void Show(BrowseNode node, DecompilationLanguage language, CodeViewPosition? position = null, Bookmark? target = null)
    {
        Version++;
        if (ReferenceEquals(node, Node) && language == Language && !IsBusy)
        {
            Arrive(position, target);
            return;
        }
        Cancel();
        _restore = position;
        _target = target;
        var work = new CancellationTokenSource();
        _work = work;
        Node = node;
        Location = node.Path;
        _title = NodeText.Name(node);
        Language = language;
        Document = DecompiledDocument.FromText(node.Text, $"// {_strings().DocumentDecompiling}", language);

        node.DecompileAsync(language, work.Token).ContinueWith(task => _post(() => Complete(work, node, task)),
            CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
    }

    /// <summary>Decompiles the shown node again in <paramref name="language"/> if it was shown in
    /// another one.</summary>
    public void EnsureLanguage(DecompilationLanguage language)
    {
        if (Node != null && Language != language)
            Show(Node, language);
    }

    /// <summary>Lets go of the node, keeping <see cref="Location"/>, and shows
    /// <paramref name="message"/> instead of the document.</summary>
    public void Release(string message)
    {
        Version++;
        Cancel();
        Node = null;
        Document = DecompiledDocument.FromText(Title, $"// {message}", Language);
    }

    public void Cancel()
    {
        _work?.Cancel();
        _work = null;
    }

    private void Arrive(CodeViewPosition? position, Bookmark? target)
    {
        if (position is { } p)
            View.Restore(p);
        else if (Node != null && target?.FindLine(Document, Node.Path) is >= 0 and var marked)
            View.RevealLine(marked);
        else if (Node != null && Document.FindDefinitionLine(Node) is var line and >= 0)
            View.RevealLine(line);
    }

    private void Complete(CancellationTokenSource work, BrowseNode node, Task<DecompiledDocument> task)
    {
        work.Dispose();
        if (_work != work) return;
        _work = null;
        if (task.IsCanceled) return;

        var t = _strings();
        Document = task.IsFaulted
            ? DecompiledDocument.FromText(node.Text, $"// {t.DocumentDecompilationFailed(task.Exception!.GetBaseException().Message)}", Language)
            : task.Result;
        var failed = task.IsFaulted || Document.IsError;
        if (failed)
            _reportError(t.ErrorDecompile(node.FullName), task.IsFaulted ? task.Exception!.GetBaseException().ToString() : Document.GetText());
        Arrive(failed ? null : _restore, failed ? null : _target);
        _restore = null;
        _target = null;
    }
}
