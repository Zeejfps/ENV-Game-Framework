using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Core.Resources;

namespace ZGF.ILSpy;

/// <summary>An open tab: where it points, the node found there, and what it shows of it, which arrives
/// from the background. A load started for it cancels the one before, whose result is then dropped.
/// Mutated on the UI thread only.</summary>
public sealed class DocumentTab
{
    private CancellationTokenSource? _work;
    private Arrival _arrival;
    private CodeViewPosition _viewPosition;
    private string? _title;

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

    /// <summary>Whether the tab shows <paramref name="node"/>, done loading, as <paramref name="language"/>
    /// and <paramref name="options"/> would show it.</summary>
    public bool Shows(BrowseNode node, DecompilationLanguage language, DecompilerOptions options)
    {
        if (!ReferenceEquals(node, Node))
            return false;
        if (IsBusy)
            return false;
        if (!node.DependsOnLanguage)
            return true;
        return IsCurrent(language, options);
    }

    /// <summary>Whether what the tab shows was decompiled in <paramref name="language"/> with options
    /// that produce the same code as <paramref name="options"/>.</summary>
    public bool IsCurrent(DecompilationLanguage language, DecompilerOptions options)
    {
        if (language != Language)
            return false;
        return Options.ProducesSameCode(options, language);
    }

    /// <summary>Points the tab at <paramref name="node"/>, cancelling what was in flight, and shows it
    /// loading until <see cref="FinishLoading"/>; keeps <paramref name="arrival"/> until then.</summary>
    public LoadTicket StartLoading(BrowseNode node, DecompilationLanguage language, DecompilerOptions options, Arrival arrival)
    {
        Version++;
        Cancel();
        var work = new CancellationTokenSource();
        _work = work;
        _arrival = arrival;
        Node = node;
        Location = node.Path;
        _title = NodeText.Name(node);
        Language = language;
        Options = options;
        Content = new TabContent.Loading(node);
        return new LoadTicket(work);
    }

    /// <summary>Ends the load <paramref name="load"/> was given for. False when the tab was pointed
    /// elsewhere or cancelled since, and what the load brought should be dropped.</summary>
    public bool FinishLoading(LoadTicket load)
    {
        load.Work.Dispose();
        if (_work != load.Work)
            return false;
        _work = null;
        return true;
    }

    /// <summary>Shows <paramref name="content"/>, which the finished load brought.</summary>
    public void ShowLoaded(TabContent content)
    {
        Content = content;
    }

    /// <summary>Where the view was to stand once the load arrived, given when it started; forgotten once taken.</summary>
    public Arrival TakeArrival()
    {
        var arrival = _arrival;
        _arrival = new Arrival();
        return arrival;
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
}

/// <summary>Given for one load of what a tab shows by <see cref="DocumentTab.StartLoading"/>.</summary>
public sealed class LoadTicket
{
    internal LoadTicket(CancellationTokenSource work)
    {
        Work = work;
    }

    internal CancellationTokenSource Work { get; }

    public CancellationToken Token => Work.Token;
}
