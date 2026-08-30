namespace ZGF.Gui.Desktop;

/// <summary>
/// A pending request to close the application, which a handler may hold open.
/// </summary>
/// <remarks>
/// Cancelling does not decline the request, it defers it: whoever cancels owns asking the user and
/// calling <see cref="GuiApp.Quit"/> if they say yes. A cancelled request nobody follows up on is an
/// application that cannot be closed.
/// </remarks>
public sealed class CloseRequest
{
    public bool IsCancelled { get; private set; }

    /// <summary>Holds the application open, leaving the request for this handler to resolve.</summary>
    public void Cancel() => IsCancelled = true;
}
