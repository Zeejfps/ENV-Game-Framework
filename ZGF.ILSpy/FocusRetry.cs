namespace ZGF.ILSpy;

/// <summary>When to try focusing another tab while none is active: once each time the layout or the
/// number of open tabs changes, so a try that found none is not repeated every frame.</summary>
public sealed class FocusRetry
{
    private Tried _last = new(-1, -1);

    /// <summary>Whether a try is due with <paramref name="layoutChanges"/> and <paramref name="tabCount"/>;
    /// records it as tried when it is.</summary>
    public bool TakeTry(int layoutChanges, int tabCount)
    {
        var tried = new Tried(layoutChanges, tabCount);
        if (tried == _last)
            return false;
        _last = tried;
        return true;
    }

    /// <summary>What a try saw: the layout's change count and how many tabs were open.</summary>
    private readonly record struct Tried(int LayoutChanges, int TabCount);
}
