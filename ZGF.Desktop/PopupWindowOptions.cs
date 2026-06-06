namespace ZGF.Desktop;

public readonly struct PopupWindowOptions
{
    public required int WidthPoints { get; init; }
    public required int HeightPoints { get; init; }
    public required IWindow OwnerWindow { get; init; }
    public required bool MousePassThrough { get; init; }
}
