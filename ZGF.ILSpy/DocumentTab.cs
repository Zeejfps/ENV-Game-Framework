using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Core.Resources;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>An open tab: where it points, the node found there, and its decompiled document — or
/// for a metadata node its grid — which arrives from the background, then commands the view where to
/// stand in it. Mutated on the UI thread only.</summary>
public sealed class DocumentTab
{
    private static int _nextId;

    private readonly Func<Strings> _strings;
    private readonly Func<DecompilerOptions> _options;
    private readonly Action<string, string> _reportError;
    private readonly Queue<ViewCommand> _viewCommands;
    private CancellationTokenSource? _work;
    private CodeViewPosition _viewPosition;
    private CodeViewPosition? _restore;
    private Bookmark? _target;
    private int _row = MetadataGridState.None;
    private string? _title;

    /// <param name="strings">The interface's strings in the current language.</param>
    /// <param name="options">How C# is decompiled now.</param>
    /// <param name="reportError">Tells the user that decompiling failed: a title and details.</param>
    /// <param name="viewCommands">Where the tab queues the commands that tell the view where to stand once what it shows arrives.</param>
    public DocumentTab(Func<Strings> strings, Func<DecompilerOptions> options, Action<string, string> reportError, Queue<ViewCommand> viewCommands)
    {
        _strings = strings;
        _options = options;
        _reportError = reportError;
        _viewCommands = viewCommands;
        Panel = $"document-{Id}";
    }

    public int Id { get; } = Interlocked.Increment(ref _nextId);

    /// <summary>The name of the tab's panel in the layout.</summary>
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

    /// <summary>The rows of a node that shows a grid, once read; shown instead of <see cref="Document"/>.</summary>
    public MetadataGrid? Grid { get; private set; }

    /// <summary>The picture of a node that shows one, once decoded; shown instead of <see cref="Document"/>.</summary>
    public ResourceImage? Image { get; private set; }

    /// <summary>Whether <see cref="Image"/> is stored as markup, such as SVG, which
    /// <see cref="Document"/> then holds.</summary>
    public bool HasSource { get; private set; }

    private void ForgetPicture()
    {
        Image = null;
        HasSource = false;
    }

    /// <summary>Where the view of the shown node stands, as the view last kept it, or null while there is none.</summary>
    public CodeViewPosition? Position
    {
        get
        {
            if (IsBusy)
                return null;
            if (Node == null)
                return null;
            return _viewPosition;
        }
    }

    /// <summary>Keeps where the view of what the tab shows stands, for <see cref="Position"/>.</summary>
    public void KeepPosition(CodeViewPosition position)
    {
        _viewPosition = position;
    }

    public bool IsBusy => _work != null;
    public string Title => Node != null ? NodeText.Name(Node) : _title ?? _strings().DocumentNewTab;

    /// <summary>The full identity of what the tab shows.</summary>
    public string Tooltip => Node != null ? NodeText.Tooltip(Node) : Title;

    /// <summary>Changes whenever the tab is pointed somewhere, or loses its node.</summary>
    public int Version { get; private set; }

    /// <summary>Starts decompiling <paramref name="node"/>, or reading its grid, cancelling what was
    /// in flight, and shows a placeholder until it is done, then commands the view to return to
    /// <paramref name="position"/>, else to reveal the line <paramref name="target"/> marks or the grid's
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
        ShowsText = false;
        _title = NodeText.Name(node);
        Language = language;
        Options = _options();
        Grid = null;
        ForgetPicture();
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

    /// <summary>Shows <paramref name="document"/>, which no node stands for, titled
    /// <paramref name="title"/>, cancelling what was in flight.</summary>
    public void ShowText(string title, DecompiledDocument document)
    {
        Version++;
        Cancel();
        Node = null;
        Location = null;
        Grid = null;
        ForgetPicture();
        _title = title;
        Document = document;
        ShowsText = true;
    }

    /// <summary>Shows nothing, as a new tab does, cancelling what was in flight.</summary>
    public void Clear()
    {
        Version++;
        Cancel();
        Node = null;
        Location = null;
        Grid = null;
        ForgetPicture();
        _title = null;
        Document = DecompiledDocument.Empty;
        ShowsText = false;
    }

    /// <summary>Whether the tab shows a document of its own, such as a report, rather than a node.</summary>
    public bool ShowsText { get; private set; }

    /// <summary>Lets go of the node, keeping <see cref="Location"/>, and shows
    /// <paramref name="message"/> instead of the document.</summary>
    public void Release(string message)
    {
        Version++;
        Cancel();
        Node = null;
        Grid = null;
        ForgetPicture();
        Document = DecompiledDocument.FromText(Title, $"// {message}", Language);
    }

    public void Cancel()
    {
        _work?.Cancel();
        _work = null;
    }

    /// <summary>Commands the view to return to <paramref name="position"/>, else to reveal the grid's
    /// <paramref name="row"/> or the line <paramref name="target"/> marks, else the node's definition.</summary>
    private void Arrive(CodeViewPosition? position, Bookmark? target, int row)
    {
        if (position != null)
        {
            _viewCommands.Enqueue(new ViewCommand.RestoreView(this, position.Value));
            return;
        }
        if (Grid != null)
        {
            RevealRow(row);
            return;
        }
        var line = FindLineToReveal(target);
        if (line < 0)
            return;
        _viewCommands.Enqueue(new ViewCommand.RevealLine(this, line));
    }

    private void RevealRow(int row)
    {
        if (row < 0)
            return;
        _viewCommands.Enqueue(new ViewCommand.RevealRow(this, row));
    }

    /// <summary>The line <paramref name="target"/> marks, else the node's definition, or -1.</summary>
    private int FindLineToReveal(Bookmark? target)
    {
        var node = Node;
        if (node == null)
            return -1;
        var marked = FindMarkedLine(target, node);
        if (marked >= 0)
            return marked;
        return Document.FindDefinitionLine(node);
    }

    private int FindMarkedLine(Bookmark? target, BrowseNode node)
    {
        if (target == null)
            return -1;
        return target.FindLine(Document, node.Path);
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
                break;
            case ImageContent { Image: var image, Source: var source }:
                ShowPicture(image, source);
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

    private void ShowPicture(ResourceImage image, DecompiledDocument? source)
    {
        Image = image;
        if (source == null)
            return;
        Document = source;
        HasSource = true;
    }
}
