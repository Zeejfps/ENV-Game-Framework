using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

internal static class FileChangesPalette
{
    public const uint StatusAdded = 0xFF57F287;
    public const uint StatusModified = 0xFFE9C77A;
    public const uint StatusDeleted = 0xFFED4245;
    public const uint StatusRenamed = 0xFF5DADE2;
    public const uint StatusOther = 0xFF9B59B6;

    public const uint BadgeText = 0xFF1A1B1E;
    public const uint RowText = 0xFFB5B9C0;

    public const uint HeaderBg = 0xFF222326;
    public const uint HeaderBorder = 0xFF313338;
    public const uint HeaderText = 0xFF96989D;

    public static uint StatusColor(FileChangeStatus status) => status switch
    {
        FileChangeStatus.Added => StatusAdded,
        FileChangeStatus.Modified => StatusModified,
        FileChangeStatus.Deleted => StatusDeleted,
        FileChangeStatus.Renamed => StatusRenamed,
        _ => StatusOther,
    };

    public static string StatusGlyph(FileChangeStatus status) => status switch
    {
        FileChangeStatus.Added => "A",
        FileChangeStatus.Modified => "M",
        FileChangeStatus.Deleted => "D",
        FileChangeStatus.Renamed => "R",
        FileChangeStatus.Copied => "C",
        FileChangeStatus.TypeChanged => "T",
        _ => "·",
    };

    public static string FormatPath(FileChange file)
    {
        if (file.Status == FileChangeStatus.Renamed && !string.IsNullOrEmpty(file.OldPath))
            return $"{file.OldPath} → {file.Path}";
        return file.Path;
    }
}

/// <summary>
/// One row in a list of file changes: a colored status badge ("A", "M", "D", …) followed by
/// the file path. Renamed entries render as "old → new". Reused by both the commit details
/// panel and the local changes view.
/// </summary>
public sealed class FileChangeRowView : MultiChildView
{
    private const float BadgeSize = 16f;

    public FileChangeRowView(FileChange file)
    {
        var badge = new RectView
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

        var path = new TextView
        {
            Text = FileChangesPalette.FormatPath(file),
            TextColor = FileChangesPalette.RowText,
        };

        AddChildToSelf(new FlexRowView
        {
            Gap = 8f,
            CrossAxisAlignment = CrossAxisAlignment.Start,
            Children =
            {
                badge,
                new FlexItem { Grow = 1, Child = path },
            },
        });
    }
}
