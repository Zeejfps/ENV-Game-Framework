namespace ZGF.Gui.Widgets;

/// <summary>
/// The absent branch for <see cref="Show"/>/<see cref="Switch{T}"/>: renders nothing and
/// occupies no layout space. <see cref="Show"/> uses the shared <see cref="Widget"/> instance
/// as its default <c>Else</c>; <see cref="SwapRegion{T}"/> recognises it by reference and hides
/// the host rather than mounting a zero-size child.
/// </summary>
public sealed record Empty : Widget
{
    public static readonly IWidget Widget = new Empty();

    protected override View CreateView(Context ctx) => new();
}
