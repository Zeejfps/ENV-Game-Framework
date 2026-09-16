namespace ZGF.Desktop;

// Options for a persistent secondary top-level window. Distinct from PopupWindowOptions,
// which describes the undecorated,
// non-resizable, floating popups used for menus and tooltips.
public readonly struct WindowOptions
{
    public required int WidthPoints { get; init; }
    public required int HeightPoints { get; init; }
    public required string Title { get; init; }
    public bool IsUndecorated { get; init; }
}
