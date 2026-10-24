namespace ZGF.ILSpy;

/// <summary>What the screen asks of the app state, which <see cref="UiState"/> queues while it draws
/// and <see cref="IlSpyApp"/> runs, in the order queued, once the frame is drawn.</summary>
public abstract record AppCommand
{
    private AppCommand()
    {
    }

    /// <summary>Open an empty tab shown in <see cref="Panel"/>, which the layout made for itself, and
    /// make it the active tab when <see cref="Focus"/>.</summary>
    public sealed record OpenTab(string Panel, bool Focus) : AppCommand;

    /// <summary>Close <see cref="Tab"/>, whose panel the layout closed.</summary>
    public sealed record CloseTab(DocumentTab Tab) : AppCommand;

    /// <summary>Make <see cref="Tab"/> the active tab and reveal what it shows in the tree.</summary>
    public sealed record FocusTab(DocumentTab Tab) : AppCommand;
}
