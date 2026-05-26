using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// A titled list of file changes: a header bar showing "Title (count)" with a colored
/// status-coded row per file. Reused by both the commit details panel and the local
/// changes view. Rebuild the contents by calling <see cref="SetFiles"/>.
///
/// Optionally selectable: pass <c>selectedPath</c> + <c>onRowClicked</c> to make regular
/// file rows highlight on hover/selection and dispatch clicks. Submodule rows keep their
/// own click behavior (jump to submodule) and are not selectable.
/// </summary>
public sealed class FileChangesSection : MultiChildView
{
    private readonly string _title;
    private readonly TextView _headerText;
    private readonly ColumnView _rows;
    private readonly TextView _emptyPlaceholder;
    private readonly IReadable<string?>? _selectedPath;
    private readonly Action<FileChange>? _onRowClicked;

    public FileChangesSection(
        string title,
        string emptyText = "(none)",
        IReadable<string?>? selectedPath = null,
        Action<FileChange>? onRowClicked = null)
    {
        _title = title;
        _selectedPath = selectedPath;
        _onRowClicked = onRowClicked;
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
            {
                _rows.Children.Add(new SubmodulePointerRowView(file));
            }
            else if (_onRowClicked != null)
            {
                _rows.Children.Add(new SelectableFileRow(file, _selectedPath, _onRowClicked));
            }
            else
            {
                _rows.Children.Add(new FileChangeRowView(file));
            }
        }
    }
}

internal sealed class SelectableFileRow : HoverableButton
{
    public SelectableFileRow(
        FileChange file,
        IReadable<string?>? selectedPath,
        Action<FileChange> onClick)
        : base(() => onClick(file))
    {
        var background = new RectView
        {
            BorderRadius = BorderRadiusStyle.All(3),
            Padding = new PaddingStyle { Left = 4, Right = 4, Top = 2, Bottom = 2 },
            Children = { new FileChangeRowView(file) },
        };
        var path = file.Path;
        background.BindBackgroundColor(() =>
        {
            if (selectedPath?.Value == path) return DialogPalette.RowActive;
            return IsHovered ? DialogPalette.RowHover : DialogPalette.RowTransparent;
        });
        SetBackground(background);
    }
}
