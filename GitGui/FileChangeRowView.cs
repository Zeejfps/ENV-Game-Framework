using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

/// <summary>
/// One row in a list of file changes: a colored status badge ("A", "M", "D", …) followed by
/// the file path. Renamed entries render as "old → new". Reused by both the commit details
/// panel and the local changes view.
/// </summary>
public sealed class FileChangeRowView : MultiChildView
{
    public FileChangeRowView(FileChange file)
    {
        var path = new TextView { Text = FileChangesUtil.FormatPath(file) };
        path.BindTextColorFromTheme(t => t.FileChanges.RowText);

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
