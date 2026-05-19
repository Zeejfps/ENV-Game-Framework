using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

internal sealed class LocalChangesPanel : MultiChildView
{
    private const int ContentPadding = 10;

    private readonly string _title;
    private readonly TextView _headerText;
    private readonly ColumnView _rows;
    private readonly TextView _emptyPlaceholder;
    private readonly ScrollPane _scrollPane;
    private readonly VerticalScrollBarView _scrollBar;
    private readonly HorizontalScrollBarView _hScrollBar;
    private readonly State<HashSet<string>> _selection = new(new HashSet<string>());
    private IReadOnlyList<FileChange> _files = Array.Empty<FileChange>();
    private string? _anchorPath;
    private Action<string>? _onRowActivated;

    public IReadable<HashSet<string>> Selection => _selection;
    public IReadOnlyCollection<string> SelectedPaths => _selection.Value;
    public IReadOnlyList<FileChange> Files => _files;

    public LocalChangesPanel(
        string title,
        string emptyText,
        IReadOnlyList<(string Icon, Action OnClick)>? headerActions = null,
        Action<string>? onRowActivated = null,
        Action? onEmptyAreaClicked = null)
    {
        _title = title;
        _onRowActivated = onRowActivated;

        _headerText = FileChangesUI.CreateHeaderText(title);
        _rows = new ColumnView { Gap = FileChangesUI.RowGap };
        _emptyPlaceholder = FileChangesUI.CreateEmptyPlaceholder(emptyText);
        _rows.Children.Add(_emptyPlaceholder);

        View headerContent;
        if (headerActions is { Count: > 0 })
        {
            var actionRow = new FlexRowView
            {
                Gap = 2f,
                CrossAxisAlignment = CrossAxisAlignment.Center,
            };
            foreach (var (icon, onClick) in headerActions)
                actionRow.Children.Add(new LocalChangesHeaderActionButton(icon, onClick));

            headerContent = new FlexRowView
            {
                CrossAxisAlignment = CrossAxisAlignment.Center,
                Children =
                {
                    new FlexItem { Grow = 1, Child = _headerText },
                    actionRow,
                },
            };
        }
        else
        {
            headerContent = _headerText;
        }

        var headerBar = FileChangesUI.CreateHeaderBar(headerContent);

        var paddedRows = new PaddingView
        {
            Padding = new PaddingStyle
            {
                Left = ContentPadding,
                Right = ContentPadding,
                Top = ContentPadding,
                Bottom = ContentPadding,
            },
            Children = { _rows },
        };

        _scrollPane = new ScrollPane();
        _scrollPane.Children.Add(paddedRows);
        _scrollPane.UseController(_ => new ScrollPaneWheelController(_scrollPane));

        _scrollBar = ScrollBarStyles.CreateVertical();
        _hScrollBar = ScrollBarStyles.CreateHorizontal();

        // _scrollPane already carries ScrollPaneWheelController, and the InputSystem
        // only allows one controller per view — so the deselect-on-empty-click handler
        // lives on a thin wrapper that covers the same body area. Row clicks consume
        // the press before bubbling reaches the wrapper, so only clicks that hit empty
        // space inside the scroll viewport trigger the callback.
        View center = _scrollPane;
        if (onEmptyAreaClicked != null)
        {
            var bodyWrapper = new RectView { Children = { _scrollPane } };
            bodyWrapper.UseController(_ => new EmptyAreaClickController(onEmptyAreaClicked));
            center = bodyWrapper;
        }

        AddChildToSelf(new BorderLayoutView
        {
            North = headerBar,
            Center = center,
            East = _scrollBar,
            South = _hScrollBar,
        });

        this.UseController(_ => new ScrollSyncController(_scrollPane, _scrollBar, _hScrollBar));
    }

    public void SetFiles(IReadOnlyList<FileChange> files)
    {
        _files = files;
        _anchorPath = null;
        _headerText.Text = FileChangesUI.FormatHeader(_title, files.Count);
        // The path set changed; drop any selection that no longer points at a real row.
        if (_selection.Value.Count > 0)
            _selection.Value = new HashSet<string>();
        _rows.Children.Clear();
        if (files.Count == 0)
        {
            _rows.Children.Add(_emptyPlaceholder);
        }
        else
        {
            foreach (var file in files)
                _rows.Children.Add(new SelectableFileRowView(file, _selection, HandleRowClick, _onRowActivated));
        }
        _scrollPane.ScrollToOrigin();
    }

    public void ClearSelection()
    {
        _anchorPath = null;
        if (_selection.Value.Count == 0) return;
        _selection.Value = new HashSet<string>();
    }

    public void SetSelection(IReadOnlyCollection<string> paths)
    {
        if (paths.Count == 0) return;
        var available = new HashSet<string>(_files.Count);
        foreach (var f in _files) available.Add(f.Path);

        HashSet<string>? next = null;
        string? anchor = null;
        foreach (var p in paths)
        {
            if (!available.Contains(p)) continue;
            (next ??= new HashSet<string>()).Add(p);
            anchor ??= p;
        }
        if (next == null) return;

        _anchorPath = anchor;
        _selection.Value = next;
    }

    private void HandleRowClick(string path, InputModifiers modifiers)
    {
        var shift = (modifiers & InputModifiers.Shift) != 0;
        // Cmd on macOS reports as Super; Ctrl on Windows/Linux as Control. Treat both
        // as the toggle-modifier so the panel feels right on every host.
        var toggle = (modifiers & (InputModifiers.Control | InputModifiers.Super)) != 0;

        if (shift && _anchorPath != null)
        {
            var anchorIdx = IndexOfPath(_anchorPath);
            var clickIdx = IndexOfPath(path);
            if (anchorIdx >= 0 && clickIdx >= 0)
            {
                var lo = Math.Min(anchorIdx, clickIdx);
                var hi = Math.Max(anchorIdx, clickIdx);
                var next = new HashSet<string>();
                for (var i = lo; i <= hi; i++)
                    next.Add(_files[i].Path);
                _selection.Value = next;
                // Anchor intentionally stays — extending the shift-range pivots around it.
                return;
            }
        }

        if (toggle)
        {
            var next = new HashSet<string>(_selection.Value);
            if (!next.Add(path)) next.Remove(path);
            _selection.Value = next;
            _anchorPath = path;
            return;
        }

        _selection.Value = new HashSet<string> { path };
        _anchorPath = path;
    }

    private int IndexOfPath(string path)
    {
        for (var i = 0; i < _files.Count; i++)
        {
            if (_files[i].Path == path) return i;
        }
        return -1;
    }
}