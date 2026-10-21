namespace ZGF.Gui.Immediate.Widgets;

/// <summary>Metrics for the built-in widgets. Colors come from the <see cref="UiStyle"/> palette.
/// Give a whole UI or a subtree other metrics with <c>ui.Provide(theme)</c>.</summary>
public sealed record WidgetTheme
{
    public float ControlHeight { get; init; } = 30f;
    public float ControlPaddingX { get; init; } = 12f;
    public float CornerRadius { get; init; } = 6f;
    public float PanelPadding { get; init; } = 12f;

    /// <summary>The theme provided around this point in <paramref name="ui"/>, or
    /// <see cref="Default"/>.</summary>
    public static WidgetTheme For(Ui ui) => ui.Get<WidgetTheme>() ?? Default;

    public static WidgetTheme Default { get; } = new();
}
