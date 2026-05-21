using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

/// <summary>
/// A titled list of file changes: a header bar showing "Title (count)" with a colored
/// status-coded row per file. Reused by both the commit details panel and the local
/// changes view. Rebuild the contents by calling <see cref="SetFiles"/>.
/// </summary>
public sealed class FileChangesSection : MultiChildView
{
    private readonly string _title;
    private readonly TextView _headerText;
    private readonly ColumnView _rows;
    private readonly TextView _emptyPlaceholder;

    public FileChangesSection(string title, string emptyText = "(none)")
    {
        _title = title;
        _headerText = FileChangesUI.CreateHeaderText(title);
        _rows = new ColumnView { Gap = FileChangesUI.RowGap };
        _emptyPlaceholder = FileChangesUI.CreateEmptyPlaceholder(emptyText);

        AddChildToSelf(new ColumnView
        {
            Gap = 4,
            Children = { FileChangesUI.CreateHeaderBar(_headerText), _rows },
        });
    }

    public void SetFiles(IReadOnlyList<FileChange> files)
    {
        _headerText.Text = FileChangesUI.FormatHeader(_title, files.Count);
        _rows.Children.Clear();
        if (files.Count == 0)
        {
            _rows.Children.Add(_emptyPlaceholder);
            return;
        }
        foreach (var file in files)
        {
            if (file.Status == FileChangeStatus.Submodule)
                _rows.Children.Add(new SubmodulePointerRowView(file));
            else
                _rows.Children.Add(new FileChangeRowView(file));
        }
    }
}
