using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Core.Resources;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>An open tab: where it points, the node found there, and what it shows of it, which
/// arrives from the background, then commands the view where to stand in it. Mutated on the UI thread only.</summary>
public sealed class DocumentTab
{
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

    /// <param name="strings">The interface's strings in the current language, for the errors it reports.</param>
    /// <param name="options">How C# is decompiled now.</param>
    /// <param name="reportError">Tells the user that decompiling failed: a title and details.</param>
    /// <param name="viewCommands">Where the tab queues the commands that tell the view where to stand once what it shows arrives.</param>
    public DocumentTab(Func<Strings> strings, Func<DecompilerOptions> options, Action<string, string> reportError, Queue<ViewCommand> viewCommands)
    {
        _strings = strings;
        _options = options;
        _reportError = reportError;
        _viewCommands = viewCommands;
    }

    /// <summary>Where the tab points; outlives reloads of the assembly.</summary>
    public NodePath? Location { get; private set; }

    /// <summary>The node shown, or null while <see cref="Location"/> is being found again after a
    /// reload, or when it no longer exists.</summary>
    public BrowseNode? Node { get; private set; }

    public DecompilationLanguage Language { get; private set; }

    /// <summary>The options <see cref="Document"/> was decompiled with.</summary>
    public DecompilerOptions Options { get; private set; } = DecompilerOptions.Default;

    public TabContent Content { get; private set; } = new TabContent.Empty();

    /// <summary>The code, document or markup the tab shows, or an empty document while it shows none.</summary>
    public DecompiledDocument Document => Content switch
    {
        TabContent.Code code => code.Document,
        TabContent.Report report => report.Document,
        TabContent.Picture picture => picture.Source ?? DecompiledDocument.Empty,
        _ => DecompiledDocument.Empty,
    };

    /// <summary>The rows the tab shows, or null while it shows no grid.</summary>
    public MetadataGrid? Grid => (Content as TabContent.Table)?.Grid;

    /// <summary>The picture the tab shows, or null while it shows none.</summary>
    public ResourceImage? Image => (Content as TabContent.Picture)?.Image;

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

    /// <summary>The name of what the tab shows, kept while its node is away, or null for a new tab.</summary>
    public string? Title => _title;

    /// <summary>The full identity of what the tab shows, or null for a new tab.</summary>
    public string? Tooltip => Node != null ? NodeText.Tooltip(Node) : _title;

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
        _title = NodeText.Name(node);
        Language = language;
        Options = _options();
        Content = new TabContent.Loading(node);

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
        _title = title;
        Content = new TabContent.Report(document);
    }

    /// <summary>Shows nothing, as a new tab does, cancelling what was in flight.</summary>
    public void Clear()
    {
        Version++;
        Cancel();
        Node = null;
        Location = null;
        _title = null;
        Content = new TabContent.Empty();
    }

    /// <summary>Lets go of the node while its assembly reloads, keeping <see cref="Location"/> to find it again.</summary>
    public void ShowReloading()
    {
        Release(new TabContent.Reloading());
    }

    /// <summary>Lets go of the node, which the reloaded assemblies no longer hold at <paramref name="location"/>.</summary>
    public void ShowGone(NodePath location)
    {
        Release(new TabContent.Gone(location));
    }

    private void Release(TabContent content)
    {
        Version++;
        Cancel();
        Node = null;
        Content = content;
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
                Content = new TabContent.Table(grid);
                break;
            case ImageContent { Image: var image, Source: var source }:
                Content = new TabContent.Picture(image, source);
                break;
            case DocumentContent { Document: var document }:
                Content = new TabContent.Code(document);
                break;
            default:
                Content = new TabContent.Failed(error!.Message);
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
