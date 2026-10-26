using System.Collections;

namespace ZGF.ILSpy;

/// <summary>The open document tabs, the one the user last worked in, and the preview tab, which shows
/// what is picked until it is kept open; each of those is always one of the open tabs.</summary>
public sealed class DocumentTabs : IReadOnlyList<DocumentTab>
{
    private readonly List<DocumentTab> _tabs = new();

    /// <summary>The tab the user last worked in, where the tree and navigation show what they pick.</summary>
    public DocumentTab? Active { get; private set; }

    /// <summary>The tab that shows what is picked, replaced by what is picked next, until it is kept
    /// open; null when no tab is one.</summary>
    public DocumentTab? Preview { get; private set; }

    public int Count => _tabs.Count;

    public DocumentTab this[int index] => _tabs[index];

    public void Add(DocumentTab tab)
    {
        _tabs.Add(tab);
    }

    /// <summary>Cancels the tab's work and closes it. The tab stops being active if it was.</summary>
    public void Remove(DocumentTab tab)
    {
        if (!_tabs.Remove(tab))
            return;
        tab.Cancel();
        if (tab == Active)
            Active = null;
        if (tab == Preview)
            Preview = null;
    }

    public void MakeActive(DocumentTab tab)
    {
        if (!_tabs.Contains(tab))
            throw new InvalidOperationException("Only an open tab can be active.");
        Active = tab;
    }

    /// <summary>Makes <paramref name="tab"/> the preview tab; the one before it stays open, kept.</summary>
    public void MakePreview(DocumentTab tab)
    {
        if (!_tabs.Contains(tab))
            throw new InvalidOperationException("Only an open tab can be the preview tab.");
        Preview = tab;
    }

    /// <summary>Keeps <paramref name="tab"/> open: it is no longer the preview tab, if it was.</summary>
    public void Keep(DocumentTab tab)
    {
        if (tab == Preview)
            Preview = null;
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
