namespace ZGF.Core;

// Options for a real, decorated secondary top-level window (resizable, movable, with a
// native title bar). Distinct from PopupWindowOptions, which describes the undecorated,
// non-resizable, floating popups used for menus and tooltips.
public readonly struct WindowOptions
{
    public required int WidthPoints { get; init; }
    public required int HeightPoints { get; init; }
    public required string Title { get; init; }
}
