using ZGF.Gui;

namespace GitGui;

/// <summary>
/// Inline warning banner — bordered box with red-on-amber text. Self-managing: setting
/// <see cref="Message"/> to null toggles <see cref="View.IsVisible"/> off, so the bar is
/// skipped by layout (no residual gap in Flex/Column/Row containers).
/// </summary>
internal sealed class ErrorBarView : MultiChildView
{
    private readonly TextView _text;

    public ErrorBarView(int verticalPadding = 4)
    {
        IsVisible = false;

        _text = new TextView
        {
            TextColor = CommitsPalette.WarningText,
            VerticalTextAlignment = TextAlignment.Center,
        };
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
            Children = { _text },
        });
    }

    /// <summary>Show with <paramref name="value"/>, or hide when null.</summary>
    public string? Message
    {
        set
        {
            if (value == null)
            {
                IsVisible = false;
                return;
            }
            _text.Text = value;
            IsVisible = true;
        }
    }
}
