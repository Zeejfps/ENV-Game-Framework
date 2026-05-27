using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;

namespace GitGui;

internal static class FileChangeFormatting
{
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
            Text = FileChangeFormatting.FormatPath(file),
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
