namespace GitGui;

/// <summary>
/// Typo-safe style class identifiers. Tag a view with <c>view.StyleClasses.Add(StyleClassNames.X)</c>
/// to opt it into sheet rules for class <c>X</c>. New entries here pair with new rules in
/// <see cref="StyleSheetBuilder"/>.
/// </summary>
public static class StyleClassNames
{
    public const string DialogHeader = "dialog-header";
    public const string DialogHeaderIcon = "dialog-header-icon";
    public const string DialogHeaderPrefix = "dialog-header-prefix";
    public const string DialogHeaderName = "dialog-header-name";

    public const string DialogButton = "dialog-button";
    public const string DialogButtonLabel = "dialog-button-label";
    public const string DialogButtonIcon = "dialog-button-icon";

    public const string Tooltip = "tooltip";
    public const string TooltipText = "tooltip-text";
}
