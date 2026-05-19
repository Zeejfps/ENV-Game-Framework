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
    private readonly State<PanelSnapshot> _state = new(PanelSnapshot.Empty);
    private readonly Derived<HashSet<string>> _selection;
    private Action<string>? _onRowActivated;

    public IReadable<HashSet<string>> Selection => _selection;
    public IReadOnlyCollection<string> SelectedPaths => _state.Value.Selection;
    public IReadOnlyList<FileChange> Files => _state.Value.Files;

    /// <summary>
    /// Immutable bundle of the panel's three coupled fields: the file list, the
    /// selection set, and the shift-anchor. The constructor is private; the only way
    /// to obtain a snapshot is via <see cref="Create"/>, which prunes <paramref name="selection"/>
    /// and <paramref name="anchor"/> against <paramref name="files"/>. That guarantees the
    /// invariant <c>Selection ⊆ Files.Paths ∧ (Anchor is null ∨ Anchor ∈ Files.Paths)</c>
    /// at the type level — a state where selection references a path no longer in the
    /// list is unrepresentable, so a list refresh cannot silently desync the selection.
    /// </summary>
    private sealed record PanelSnapshot
    {
        public IReadOnlyList<FileChange> Files { get; }
        public HashSet<string> Selection { get; }
        public string? Anchor { get; }

        public static readonly PanelSnapshot Empty = new(
            Array.Empty<FileChange>(), new HashSet<string>(), null);

        private PanelSnapshot(IReadOnlyList<FileChange> files, HashSet<string> selection, string? anchor)
        {
            Files = files;
            Selection = selection;
            Anchor = anchor;
        }

        public static PanelSnapshot Create(
            IReadOnlyList<FileChange> files,
            IEnumerable<string> selection,
            string? anchor)
        {
            var paths = new HashSet<string>(files.Count);
            foreach (var f in files) paths.Add(f.Path);

            var nextSelection = new HashSet<string>();
            foreach (var p in selection)
                if (paths.Contains(p)) nextSelection.Add(p);

            var nextAnchor = anchor != null && paths.Contains(anchor) ? anchor : null;
            return new PanelSnapshot(files, nextSelection, nextAnchor);
        }
    }

    public LocalChangesPanel(
        string title,
        string emptyText,
        IReadOnlyList<View>? headerActions = null,
        Action<string>? onRowActivated = null,
        Action? onEmptyAreaClicked = null)
    {
        _title = title;
        _onRowActivated = onRowActivated;
        _selection = new Derived<HashSet<string>>(() => _state.Value.Selection);

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
            foreach (var action in headerActions)
                actionRow.Children.Add(action);

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
        // Carry the existing selection/anchor through — Create normalizes them against
        // the new file set, so a reload from an unrelated trigger (watcher tick, refs
        // change) can't wipe a still-valid selection out from under the diff view.
        var prev = _state.Value;
        ApplyState(files, prev.Selection, prev.Anchor);
    }

    /// <summary>
    /// Atomic equivalent of <see cref="SetFiles"/> + <see cref="SetSelection"/>: assigns
    /// the new file list AND the new selection in a single snapshot transition. Used by
    /// stage/unstage to land both the destination panel's new rows and the selection on
    /// those rows in one shot, so the diff view never observes the brief zero-selection
    /// gap that arises when files and selection arrive as separate updates.
    /// </summary>
    public void SetFilesWithSelection(IReadOnlyList<FileChange> files, IReadOnlyCollection<string> selection)
    {
        string? anchor = null;
        foreach (var p in selection) { anchor = p; break; }
        ApplyState(files, selection, anchor);
    }

    private void ApplyState(IReadOnlyList<FileChange> files, IEnumerable<string> selection, string? anchor)
    {
        _state.Value = PanelSnapshot.Create(files, selection, anchor);
        _headerText.Text = FileChangesUI.FormatHeader(_title, files.Count);
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
        var prev = _state.Value;
        if (prev.Selection.Count == 0 && prev.Anchor == null) return;
        _state.Value = PanelSnapshot.Create(prev.Files, Array.Empty<string>(), null);
    }

    public void SetSelection(IReadOnlyCollection<string> paths)
    {
        if (paths.Count == 0) return;
        var prev = _state.Value;
        string? anchor = null;
        foreach (var p in paths) { anchor = p; break; }
        var next = PanelSnapshot.Create(prev.Files, paths, anchor);
        if (next.Selection.Count == 0) return;
        _state.Value = next;
    }

    private void HandleRowClick(string path, InputModifiers modifiers)
    {
        var prev = _state.Value;
        var shift = (modifiers & InputModifiers.Shift) != 0;
        // Cmd on macOS reports as Super; Ctrl on Windows/Linux as Control. Treat both
        // as the toggle-modifier so the panel feels right on every host.
        var toggle = (modifiers & (InputModifiers.Control | InputModifiers.Super)) != 0;

        if (shift && prev.Anchor != null)
        {
            var anchorIdx = IndexOfPath(prev.Files, prev.Anchor);
            var clickIdx = IndexOfPath(prev.Files, path);
            if (anchorIdx >= 0 && clickIdx >= 0)
            {
                var lo = Math.Min(anchorIdx, clickIdx);
                var hi = Math.Max(anchorIdx, clickIdx);
                var range = new List<string>(hi - lo + 1);
                for (var i = lo; i <= hi; i++)
                    range.Add(prev.Files[i].Path);
                // Anchor intentionally stays — extending the shift-range pivots around it.
                _state.Value = PanelSnapshot.Create(prev.Files, range, prev.Anchor);
                return;
            }
        }

        if (toggle)
        {
            var next = new HashSet<string>(prev.Selection);
            if (!next.Add(path)) next.Remove(path);
            _state.Value = PanelSnapshot.Create(prev.Files, next, path);
            return;
        }

        _state.Value = PanelSnapshot.Create(prev.Files, [path], path);
    }

    private static int IndexOfPath(IReadOnlyList<FileChange> files, string path)
    {
        for (var i = 0; i < files.Count; i++)
        {
            if (files[i].Path == path) return i;
        }
        return -1;
    }
}