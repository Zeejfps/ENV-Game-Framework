using System.Collections;

namespace ZGF.ILSpy;

/// <summary>The open document tabs and the one the user last worked in, which is always one of them.
/// A removed tab's image is kept until the view takes it to free.</summary>
public sealed class DocumentTabs : IReadOnlyList<DocumentTab>
{
    private readonly List<DocumentTab> _tabs = new();
    private readonly List<string> _releasedImages = new();

    /// <summary>The tab the user last worked in, where the tree and navigation show what they pick.</summary>
    public DocumentTab? Active { get; private set; }

    public int Count => _tabs.Count;

    public DocumentTab this[int index] => _tabs[index];

    /// <summary>The panel of the active tab, if there is one.</summary>
    public string? ActivePanel
    {
        get
        {
            if (Active == null)
                return null;
            return Active.Panel;
        }
    }

    public void Add(DocumentTab tab)
    {
        _tabs.Add(tab);
    }

    /// <summary>Cancels the tab's work, closes it and keeps its image to free. The tab stops being
    /// active if it was.</summary>
    public void Remove(DocumentTab tab)
    {
        if (!_tabs.Remove(tab))
            return;
        tab.Cancel();
        _releasedImages.Add(tab.ImageId);
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

    /// <summary>The tab whose panel is <paramref name="panel"/>.</summary>
    public DocumentTab? Find(string? panel)
    {
        foreach (var tab in _tabs)
        {
            if (tab.Panel == panel)
                return tab;
        }
        return null;
    }

    /// <summary><paramref name="tab"/> if it is still open, else null.</summary>
    public DocumentTab? KeepIfOpen(DocumentTab? tab)
    {
        if (tab == null)
            return null;
        if (!_tabs.Contains(tab))
            return null;
        return tab;
    }

    /// <summary>The images of tabs closed since the last call, for the view to free.</summary>
    public IEnumerable<string> TakeReleasedImages()
    {
        if (_releasedImages.Count == 0)
            return Array.Empty<string>();
        var released = _releasedImages.ToArray();
        _releasedImages.Clear();
        return released;
    }

    public IEnumerator<DocumentTab> GetEnumerator() => _tabs.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
