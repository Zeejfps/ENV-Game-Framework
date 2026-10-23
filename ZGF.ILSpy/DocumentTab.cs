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

    private readonly Func<Strings> _strings;
    private readonly Func<DecompilerOptions> _options;
    private readonly Action<string, string> _reportError;
    private CancellationTokenSource? _work;
    private CodeViewPosition? _restore;
    private Bookmark? _target;
    private int _row = MetadataGridState.None;
    private string? _title;

    /// <param name="strings">The interface's strings in the current language.</param>
    /// <param name="options">How C# is decompiled now.</param>
    /// <param name="reportError">Tells the user that decompiling failed: a title and details.</param>
    public DocumentTab(Func<Strings> strings, Func<DecompilerOptions> options, Action<string, string> reportError)
    {
        _strings = strings;
        _options = options;
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

    /// <summary>The options <see cref="Document"/> was decompiled with.</summary>
    public DecompilerOptions Options { get; private set; } = DecompilerOptions.Default;
    public DecompiledDocument Document { get; private set; } = DecompiledDocument.Empty;
    public CodeViewState View { get; } = new();

    /// <summary>The rows of a node that shows a grid, once read; shown instead of <see cref="Document"/>.</summary>
    public MetadataGrid? Grid { get; private set; }

    public MetadataGridState GridView { get; } = new();

    /// <summary>The picture of a node that shows one, once decoded; shown instead of <see cref="Document"/>.</summary>
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
    /// <paramref name="node"/> in <paramref name="language"/> with the current options, only does the latter.</summary>
    public void Show(BrowseNode node, DecompilationLanguage language, CodeViewPosition? position = null, Bookmark? target = null,
        int row = MetadataGridState.None)
    {
        Version++;
        if (ReferenceEquals(node, Node) && (IsCurrent(language) || !node.DependsOnLanguage) && !IsBusy)
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
        Options = _options();
        Grid = null;
        Image = null;
        Document = DecompiledDocument.FromText(node.Text, $"// {_strings().DocumentDecompiling}", language);

        Load(work, node, language);
    }

    private async void Load(CancellationTokenSource work, BrowseNode node, DecompilationLanguage language)
    {
        var loading = node.LoadContentAsync(language, Options, work.Token);
        await Background.Wait(loading);
        Complete(work, node, loading);
    }

    /// <summary>Decompiles the shown node again if it was shown in another language than
    /// <paramref name="language"/>, or with other options, then keeping the view where it was.</summary>
    public void EnsureCurrent(DecompilationLanguage language)
    {
        if (Node is not { DependsOnLanguage: true } node || IsCurrent(language)) return;
        Show(node, language, Language == language ? Position : null);
    }

    private bool IsCurrent(DecompilationLanguage language)
    {
        if (language != Language)
            return false;
        return Options.ProducesSameCode(_options(), language);
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

    private void Complete(CancellationTokenSource work, BrowseNode node, Task<NodeContent> task)
    {
        work.Dispose();
        if (_work != work) return;
        _work = null;
        if (task.IsCanceled) return;

        var t = _strings();
        var error = task.Exception?.GetBaseException();
        switch (error == null ? task.Result : null)
        {
            case GridContent { Grid: var grid }:
                Grid = grid;
                GridView.Show(grid);
                break;
            case ImageContent { Image: var image }:
                Image = image;
                break;
            case DocumentContent { Document: var document }:
                Document = document;
                break;
            default:
                Document = DecompiledDocument.FromText(node.Text, $"// {t.DocumentDecompilationFailed(error!.Message)}", Language);
                break;
        }
        var failed = error != null || Document.IsError;
        if (failed)
            _reportError(t.ErrorDecompile(node.FullName), error?.ToString() ?? Document.GetText());
        Arrive(failed ? null : _restore, failed ? null : _target, failed ? MetadataGridState.None : _row);
        _restore = null;
        _target = null;
        _row = MetadataGridState.None;
    }
}
