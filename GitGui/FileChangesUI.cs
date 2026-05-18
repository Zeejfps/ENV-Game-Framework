using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

/// <summary>
/// Shared building blocks for the two file-change list flavors — <see cref="FileChangesSection"/>
/// (commit details) and the staged/unstaged panels in <c>LocalChangesView</c>. Both render a
/// titled header bar over a row list with the same colors, padding, and "Title (count)" format.
/// </summary>
internal static class FileChangesUI
{
    public const int HeaderPadding = 4;
    public const int RowGap = 2;
    public const float BadgeSize = 16f;

    public static string FormatHeader(string title, int count) => $"{title} ({count})";

    public static TextView CreateHeaderText(string title) => new()
    {
        Text = FormatHeader(title, 0),
        TextColor = FileChangesPalette.HeaderText,
    };

    public static TextView CreateEmptyPlaceholder(string emptyText) => new()
    {
        Text = emptyText,
        TextColor = FileChangesPalette.HeaderText,
    };

    public static RectView CreateHeaderBar(View content) => new()
    {
        BackgroundColor = FileChangesPalette.HeaderBg,
        BorderColor = new BorderColorStyle
        {
            Top = FileChangesPalette.HeaderBorder,
            Bottom = FileChangesPalette.HeaderBorder,
        },
        BorderSize = new BorderSizeStyle { Top = 1, Bottom = 1 },
        Padding = new PaddingStyle
        {
            Left = HeaderPadding,
            Right = HeaderPadding,
            Top = HeaderPadding,
            Bottom = HeaderPadding,
        },
        Children = { content },
    };

    /// <summary>Square colored badge containing the single-letter status glyph for a file.</summary>
    public static RectView CreateStatusBadge(FileChange file) => new()
    {
        PreferredWidth = BadgeSize,
        PreferredHeight = BadgeSize,
        BackgroundColor = FileChangesPalette.StatusColor(file.Status),
        BorderRadius = BorderRadiusStyle.All(3),
        Children =
        {
            new TextView
            {
                Text = FileChangesPalette.StatusGlyph(file.Status),
                TextColor = FileChangesPalette.BadgeText,
                FontSize = 11f,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
            },
        },
    };
}
