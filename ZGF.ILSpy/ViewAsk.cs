namespace ZGF.ILSpy;

/// <summary>What a feature running with no view on the stack asks of the view, which
/// <see cref="UiState"/> takes, in the order asked, before it draws the next frame.</summary>
public abstract record ViewAsk
{
    private ViewAsk()
    {
    }

    /// <summary>Return <see cref="Tab"/>'s view to <see cref="Position"/>.</summary>
    public sealed record RestoreView(DocumentTab Tab, CodeViewPosition Position) : ViewAsk;

    /// <summary>Scroll <see cref="Tab"/>'s code to <see cref="Line"/> and flash it.</summary>
    public sealed record RevealLine(DocumentTab Tab, int Line) : ViewAsk;

    /// <summary>Select <see cref="Tab"/>'s grid row <see cref="Row"/> and scroll to it.</summary>
    public sealed record RevealRow(DocumentTab Tab, int Row) : ViewAsk;
}
