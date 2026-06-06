using ZGF.Desktop;

namespace ZGF.Gui;

/// <summary>
/// Opens real, decorated, resizable secondary top-level windows hosting a ZGF view tree.
/// Unlike <see cref="IPopupWindowFactory"/> (borderless, pooled, capture-driven popups for
/// menus/tooltips), these windows are persistent, user-movable/resizable, and closed by the
/// user via the native title-bar close button.
/// </summary>
public interface ISecondaryWindowFactory : IDisposable
{
    ISecondaryWindow Open(in SecondaryWindowRequest request);
}

public readonly struct SecondaryWindowRequest
{
    public required View Root { get; init; }
    public required string Title { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
}

public interface ISecondaryWindow
{
    IWindow Window { get; }

    /// <summary>Raised after the window has been closed (native close button) and torn down.</summary>
    event Action Closed;

    /// <summary>Programmatically close and dispose the window.</summary>
    void Close();
}
