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
    private const int HeaderPadding = 4;
    private const int RowGap = 2;

    private readonly string _title;
    private readonly TextView _headerText;
    private readonly ColumnView _rows;
    private readonly TextView _emptyPlaceholder;

    public FileChangesSection(string title, string emptyText = "(none)")
    {
        _title = title;
        _headerText = new TextView
        {
            Text = FormatHeader(0),
            TextColor = FileChangesPalette.HeaderText,
        };
        _rows = new ColumnView { Gap = RowGap };
        _emptyPlaceholder = new TextView
        {
            Text = emptyText,
            TextColor = FileChangesPalette.HeaderText,
        };

        var headerBar = new RectView
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
            Children = { _headerText },
        };

        AddChildToSelf(new ColumnView
        {
            Gap = 4,
            Children = { headerBar, _rows },
        });
    }

    public void SetFiles(IReadOnlyList<FileChange> files)
    {
        _headerText.Text = FormatHeader(files.Count);
        _rows.Children.Clear();
        if (files.Count == 0)
        {
            _rows.Children.Add(_emptyPlaceholder);
            return;
        }
        foreach (var file in files)
            _rows.Children.Add(new FileChangeRowView(file));
    }

    private string FormatHeader(int count) => $"{_title} ({count})";
}
