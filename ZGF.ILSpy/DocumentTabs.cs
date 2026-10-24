using System.Collections;

namespace ZGF.ILSpy;

/// <summary>The open document tabs and the one the user last worked in, which is always one of them.</summary>
public sealed class DocumentTabs : IReadOnlyList<DocumentTab>
{
    private readonly List<DocumentTab> _tabs = new();

    /// <summary>The tab the user last worked in, where the tree and navigation show what they pick.</summary>
    public DocumentTab? Active { get; private set; }

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
    }

    public void MakeActive(DocumentTab tab)
    {
        if (!_tabs.Contains(tab))
            throw new InvalidOperationException("Only an open tab can be active.");
        Active = tab;
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
