using System.Collections;

namespace ZGF.ILSpy;

/// <summary>The open document tabs, the one the user last worked in, the preview tab, which shows
/// what is picked until it is kept open, and the diagram the user last worked in; each of those is always
/// one of the open tabs.</summary>
public sealed class DocumentTabs : IReadOnlyList<DocumentTab>
{
    private readonly List<DocumentTab> _tabs = new();
    private DocumentTab? _diagram;
    private int _changes;
    private int _closedChanges;

    /// <summary>The tab the user last worked in, where the tree and navigation show what they pick.</summary>
    public DocumentTab? Active { get; private set; }

    /// <summary>The tab that shows what is picked, replaced by what is picked next, until it is kept
    /// open; null when no tab is one.</summary>
    public DocumentTab? Preview { get; private set; }

    /// <summary>The tab of the diagram the user last worked in, which types shown in a diagram are added
    /// to; null when no open tab shows a diagram the user worked in.</summary>
    public DocumentTab? Diagram
    {
        get
        {
            if (_diagram == null)
                return null;
            if (_diagram.Diagram == null)
                return null;
            return _diagram;
        }
    }

    public int Count => _tabs.Count;

    public DocumentTab this[int index] => _tabs[index];

    /// <summary>Counts up whenever a tab opens or closes, another becomes the active or the preview tab,
    /// or an open one changes as <see cref="DocumentTab.Changes"/> counts; it never counts down.
    /// Allocates nothing.</summary>
    public int Changes
    {
        get
        {
            var changes = _changes + _closedChanges;
            foreach (var tab in _tabs)
                changes += tab.Changes;
            return changes;
        }
    }

    public void Add(DocumentTab tab)
    {
        _tabs.Add(tab);
        _changes++;
    }

    /// <summary>Cancels the tab's work and closes it. The tab stops being active if it was.</summary>
    public void Remove(DocumentTab tab)
    {
        if (!_tabs.Remove(tab))
            return;
        tab.Cancel();
        _closedChanges += tab.Changes;
        _changes++;
        if (tab == Active)
            Active = null;
        if (tab == Preview)
            Preview = null;
        if (tab == _diagram)
            _diagram = null;
    }

    /// <summary>Makes <paramref name="tab"/> the active tab, and the diagram tab when it shows a diagram.</summary>
    public void MakeActive(DocumentTab tab)
    {
        if (!_tabs.Contains(tab))
            throw new InvalidOperationException("Only an open tab can be active.");
        if (tab != Active)
            _changes++;
        Active = tab;
        if (tab.Diagram != null)
            _diagram = tab;
    }

    /// <summary>Makes <paramref name="tab"/> the preview tab; the one before it stays open, kept.</summary>
    public void MakePreview(DocumentTab tab)
    {
        if (!_tabs.Contains(tab))
            throw new InvalidOperationException("Only an open tab can be the preview tab.");
        if (tab != Preview)
            _changes++;
        Preview = tab;
    }

    /// <summary>Keeps <paramref name="tab"/> open: it is no longer the preview tab, if it was.</summary>
    public void Keep(DocumentTab tab)
    {
        if (tab != Preview)
            return;
        Preview = null;
        _changes++;
    }

    public bool Contains(DocumentTab tab) => _tabs.Contains(tab);

    /// <summary><paramref name="tab"/> if it is still open, else null.</summary>
    public DocumentTab? KeepIfOpen(DocumentTab? tab)
    {
        if (tab == null)
            return null;
        if (!_tabs.Contains(tab))
            return null;
        return tab;
    }

    public IEnumerator<DocumentTab> GetEnumerator() => _tabs.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
