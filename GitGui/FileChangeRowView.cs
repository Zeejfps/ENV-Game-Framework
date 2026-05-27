using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;

namespace GitGui;

internal static class FileChangesPalette
{
    public const uint StatusAdded = 0xFF57F287;
    public const uint StatusModified = 0xFFE9C77A;
    public const uint StatusDeleted = 0xFFED4245;
    public const uint StatusRenamed = 0xFF5DADE2;
    public const uint StatusConflicted = 0xFFED4245;
    public const uint StatusSubmodule = 0xFFB57EDC;
    public const uint StatusOther = 0xFF9B59B6;

    public const uint BadgeText = Theme.BgDeep;
    public const uint RowText = Theme.TextRow;

    public const uint HeaderBg = 0xFF222326;
    public const uint HeaderBorder = Theme.Border;
    public const uint HeaderText = Theme.TextHeader;

    public static uint StatusColor(FileChangeStatus status) => status switch
    {
        FileChangeStatus.Added => StatusAdded,
        FileChangeStatus.Modified => StatusModified,
        FileChangeStatus.Deleted => StatusDeleted,
        FileChangeStatus.Renamed => StatusRenamed,
        FileChangeStatus.Conflicted => StatusConflicted,
        FileChangeStatus.Submodule => StatusSubmodule,
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
        FileChangeStatus.Conflicted => "!",
        FileChangeStatus.Submodule => "S",
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
    public FileChangeRowView(FileChange file)
    {
        var path = new TextView
        {
            Text = FileChangesPalette.FormatPath(file),
        };
        path.BindThemedTextColor(s => s.FileChangeRow.RowText);

        AddChildToSelf(new FlexRowView
        {
            Gap = 8f,
            CrossAxisAlignment = CrossAxisAlignment.Start,
            Children =
            {
                FileChangesUI.CreateStatusBadge(file),
                new FlexItem { Grow = 1, Child = path },
            },
        });
    }
}
