using ZGF.Desktop;

namespace ZGF.Gui.Desktop.Input;

/// <summary>A controller that requests a specific mouse cursor while it is the active target —
/// either hovered, or capturing the pointer during a drag. Implement alongside
/// <see cref="IKeyboardMouseController"/>; the input system reads it once per frame and pushes
/// the shape to the window.</summary>
public interface IProvidesCursor
{
    MouseCursor Cursor { get; }
}
