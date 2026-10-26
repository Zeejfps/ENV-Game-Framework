using ZGF.Desktop;

namespace ZGF.ILSpy;

/// <summary>Where a dialog's window is on the screen, kept as it moves, and how often it came to rest
/// somewhere new, for the settings to keep.</summary>
public sealed class DialogWindow
{
    private WindowBounds? _resting;

    /// <summary>Where the window is; null opens it centered.</summary>
    public WindowBounds? Bounds;

    /// <summary>Counts each time the window comes to rest somewhere new: once for each move or resize,
    /// when the button comes up, never on the frames in between.</summary>
    public int Changes { get; private set; }

    /// <summary>Puts the window where it was left in the last run.</summary>
    public void Restore(WindowBounds? bounds)
    {
        Bounds = bounds;
        _resting = bounds;
    }

    /// <summary>Counts the window as moved once it rests where it was not before, unless
    /// <paramref name="held"/>, a button still being held, says it may move on.</summary>
    public void Settle(bool held)
    {
        if (held)
            return;
        if (Bounds == _resting)
            return;
        _resting = Bounds;
        Changes++;
    }
}
