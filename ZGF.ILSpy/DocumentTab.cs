using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Core.Resources;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>An open tab: where it points, the node found there, and its decompiled document — or
/// for a metadata node its grid — which arrives from the background. Mutated on the UI thread only.</summary>
public sealed class DocumentTab
{
    private static int _nextId;

    private readonly Action<Action> _post;
    private readonly Func<Strings> _strings;
    private readonly Action<string, string> _reportError;
    private CancellationTokenSource? _work;
    private CodeViewPosition? _restore;
    private Bookmark? _target;
    private int _row = MetadataGridState.None;
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

    /// <summary>The rows of a node that <see cref="BrowseNode.ShowsGrid"/>, once read; shown
    /// instead of <see cref="Document"/>.</summary>
    public MetadataGrid? Grid { get; private set; }

    public MetadataGridState GridView { get; } = new();

    /// <summary>The picture of a node that <see cref="BrowseNode.ShowsImage"/>, once decoded; shown
    /// instead of <see cref="Document"/>.</summary>
    public ResourceImage? Image { get; private set; }

    /// <summary>The image this tab draws <see cref="Image"/> as, one per tab.</summary>
    public string ImageId => $"{Panel}-image";

    /// <summary>Where the view of the shown node stands, or null while there is none.</summary>
    public CodeViewPosition? Position => IsBusy || Node == null ? null : Grid != null ? GridView.Position : View.Position;

    public bool IsBusy => _work != null;
    public string Title => Node != null ? NodeText.Name(Node) : _title ?? _strings().DocumentNewTab;

    /// <summary>The full identity of what the tab shows.</summary>
    public string Tooltip => Node != null ? NodeText.Tooltip(Node) : Title;

    /// <summary>Changes whenever the tab is pointed somewhere, or loses its node.</summary>
    public int Version { get; private set; }

    /// <summary>Starts decompiling <paramref name="node"/>, or reading its grid, cancelling what was
    /// in flight, and shows a placeholder until it is done, then returns the view to
    /// <paramref name="position"/>, else reveals the line <paramref name="target"/> marks or the grid's
    /// <paramref name="row"/>, else the node's definition. When the tab already shows
    /// <paramref name="node"/> in <paramref name="language"/>, only does the latter.</summary>
    public void Show(BrowseNode node, DecompilationLanguage language, CodeViewPosition? position = null, Bookmark? target = null,
        int row = MetadataGridState.None)
    {
        Version++;
        if (ReferenceEquals(node, Node) && (language == Language || node.ShowsGrid || node.ShowsImage) && !IsBusy)
        {
            Arrive(position, target, row);
            return;
        }
        Cancel();
        _restore = position;
        _target = target;
        _row = row;
        var work = new CancellationTokenSource();
        _work = work;
        Node = node;
        Location = node.Path;
        _title = NodeText.Name(node);
        Language = language;
        Grid = null;
        Image = null;
        Document = DecompiledDocument.FromText(node.Text, $"// {_strings().DocumentDecompiling}", language);

        if (node.ShowsImage)
            node.LoadImageAsync(work.Token).ContinueWith(task => _post(() => CompleteImage(work, node, task)),
                CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
        else if (node.ShowsGrid)
            node.LoadGridAsync(work.Token).ContinueWith(task => _post(() => CompleteGrid(work, node, task)),
                CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
        else
            node.DecompileAsync(language, work.Token).ContinueWith(task => _post(() => Complete(work, node, task)),
                CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
    }

    /// <summary>Decompiles the shown node again in <paramref name="language"/> if it was shown in
    /// another one.</summary>
    public void EnsureLanguage(DecompilationLanguage language)
    {
        if (Node is { ShowsGrid: false, ShowsImage: false } && Language != language)
            Show(Node, language);
    }

    /// <summary>Lets go of the node, keeping <see cref="Location"/>, and shows
    /// <paramref name="message"/> instead of the document.</summary>
    public void Release(string message)
    {
        Version++;
        Cancel();
        Node = null;
        Grid = null;
        Image = null;
        Document = DecompiledDocument.FromText(Title, $"// {message}", Language);
    }

    public void Cancel()
    {
        _work?.Cancel();
        _work = null;
    }

    private void Arrive(CodeViewPosition? position, Bookmark? target, int row)
    {
        if (Grid != null)
        {
            if (position is { } at)
                GridView.Restore(at);
            else if (row >= 0)
                GridView.Reveal(row);
            return;
        }
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
        Arrive(failed ? null : _restore, failed ? null : _target, MetadataGridState.None);
        _restore = null;
        _target = null;
    }

    /// <summary>Shows the decoded picture, or when it cannot be read, the resource as a document
    /// with why first.</summary>
    private void CompleteImage(CancellationTokenSource work, BrowseNode node, Task<ResourceImage> task)
    {
        work.Dispose();
        if (_work != work) return;
        _work = null;
        if (task.IsCanceled) return;
        if (task.IsFaulted)
        {
            var reason = DecompiledDocument.FromText(node.Text, $"// {task.Exception!.GetBaseException().Message}", Language);
            var content = node.Decompile(Language);
            Document = DecompiledDocument.FromText(node.Text, reason.GetText() + "\n" + content.GetText(), Language);
            return;
        }
        Image = task.Result;
    }

    private void CompleteGrid(CancellationTokenSource work, BrowseNode node, Task<MetadataGrid> task)
    {
        work.Dispose();
        if (_work != work) return;
        _work = null;
        if (task.IsCanceled) return;

        if (task.IsFaulted)
        {
            var t = _strings();
            var error = task.Exception!.GetBaseException();
            Document = DecompiledDocument.FromText(node.Text, $"// {t.DocumentDecompilationFailed(error.Message)}", Language);
            _reportError(t.ErrorDecompile(node.FullName), error.ToString());
        }
        else
        {
            Grid = task.Result;
            GridView.Show(Grid);
            Arrive(_restore, null, _row);
        }
        _restore = null;
        _target = null;
        _row = MetadataGridState.None;
    }
}
