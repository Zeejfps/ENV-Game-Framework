namespace ZGF.ILSpy;

/// <summary>Bookmarks read from a file, waiting for the user to say whether they replace the ones
/// there are or merge in beside them.</summary>
public sealed class PendingImport(List<Bookmark> bookmarks)
{
    public IReadOnlyList<Bookmark> Bookmarks { get; } = bookmarks;
}

/// <summary>The bookmark imports under way: how many are still finding their bookmarks' places, and
/// those waiting, oldest first, for the user to say how to take their bookmarks. Each waits with its
/// own bookmarks, and the user is asked about one at a time.</summary>
public sealed class BookmarkImports
{
    private readonly List<PendingImport> _waiting = new();
    private int _finding;

    /// <summary>True while any import's places are being found in the background.</summary>
    public bool IsFinding => _finding > 0;

    /// <summary>The import the user is asked about, or null.</summary>
    public PendingImport? Asking
    {
        get
        {
            if (_waiting.Count == 0)
                return null;
            return _waiting[0];
        }
    }

    public void StartFinding()
    {
        _finding++;
    }

    public void StopFinding()
    {
        _finding--;
    }

    public void Wait(PendingImport import)
    {
        _waiting.Add(import);
    }

    /// <summary>Stops waiting on <paramref name="import"/>; false when it was not waiting.</summary>
    public bool Finish(PendingImport import) => _waiting.Remove(import);
}
