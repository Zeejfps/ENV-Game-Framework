using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Inline warning banner — bordered box with red-on-amber text. Self-managing: setting
/// <see cref="Message"/> to null toggles <see cref="View.IsVisible"/> off, so the bar is
/// skipped by layout (no residual gap in Flex/Column/Row containers).
/// </summary>
internal sealed class ErrorBarView : MultiChildView
{
    public State<string?> Message { get; } = new(null);

    public ErrorBarView(int verticalPadding = 4)
    {
        this.BindIsVisible(Message, m => m != null);

        var text = new TextView
        {
            TextColor = CommitsPalette.WarningText,
            VerticalTextAlignment = TextAlignment.Center,
        };
        text.BindText(Message);

        AddChildToSelf(new RectView
        {
            BackgroundColor = CommitsPalette.WarningBg,
            BorderColor = BorderColorStyle.All(CommitsPalette.WarningBorder),
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(3),
            Padding = new PaddingStyle
            {
                Left = 8,
                Right = 8,
                Top = verticalPadding,
                Bottom = verticalPadding,
            },
            Children = { text },
        });
    }
}
