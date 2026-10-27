namespace ZGF.ILSpy;

/// <summary>A tab reopened from what the settings kept of it, <see cref="Record"/>, while it was at
/// <see cref="Version"/>.</summary>
public sealed record ReopenedTab(DocumentTab Tab, DocumentRecord Record, int Version)
{
    /// <summary>Whether the tab still waits for what it was reopened to show: it was not pointed elsewhere since.</summary>
    public bool IsWaiting => Tab.Version == Version;
}

/// <summary>The tabs reopened from the open list's saved documents that wait for what they showed to be
/// found again, each with what the settings kept of it. A tab pointed elsewhere since waits no more.</summary>
public sealed class ReopenedTabs
{
    private readonly List<ReopenedTab> _tabs = new();
    private int _finding;

    /// <summary>True while the nodes the tabs waited for are being found.</summary>
    public bool IsFinding => _finding > 0;

    /// <summary>Counts a search for the nodes the tabs wait for as started, until <see cref="StopFinding"/>.</summary>
    public void StartFinding()
    {
        _finding++;
    }

    public void StopFinding()
    {
        _finding--;
    }

    /// <summary>Has <paramref name="tab"/>, as it is now, wait for what <paramref name="record"/> says it showed.</summary>
    public ReopenedTab Wait(DocumentTab tab, DocumentRecord record)
    {
        _tabs.RemoveAll(r => r.Tab == tab);
        var reopened = new ReopenedTab(tab, record, tab.Version);
        _tabs.Add(reopened);
        return reopened;
    }

    /// <summary>What the settings kept of <paramref name="tab"/> while it still waits for it; else null.</summary>
    public DocumentRecord? Find(DocumentTab tab)
    {
        foreach (var reopened in _tabs)
        {
            if (reopened.Tab == tab && reopened.IsWaiting)
                return reopened.Record;
        }
        return null;
    }

    /// <summary>The tabs still waiting, in the order reopened.</summary>
    public List<ReopenedTab> ListWaiting()
    {
        return _tabs
            .Where(static r => r.IsWaiting)
            .ToList();
    }

    /// <summary>Stops <paramref name="reopened"/> waiting. False when it waited no more, as when its tab
    /// was pointed elsewhere or another reopening took its place, and what it found should be dropped.</summary>
    public bool Finish(ReopenedTab reopened)
    {
        if (!_tabs.Remove(reopened))
            return false;
        return reopened.IsWaiting;
    }

    public void Clear()
    {
        _tabs.Clear();
    }
}
