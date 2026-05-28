using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// One side of the Local Changes split (Unstaged or Staged). Renders a header bar with
/// action buttons, a virtualized list of file rows, and an empty-state placeholder.
/// Selection lives on the view model (one <see cref="GitGui.Selection"/> for both
/// sides); the panel just renders rows reactively against the shared selection and
/// forwards clicks (with modifiers) to a callback that routes into the VM.
///
/// Row scroll/hit-test/wheel/double-click plumbing lives in <see cref="VirtualRowListView"/>.
/// This view owns the per-row drawing (status badge + path text), the empty-state
/// swap, and the <see cref="IScrollableContent"/> surface for the external scroll bars.
/// </summary>
internal sealed class LocalChangesPanel : MultiChildView, IScrollableContent
{
    private readonly string _title;
    private readonly DiffSide _side;
    private readonly IReadable<Selection> _selection;
    private readonly Action<DiffTarget, InputModifiers> _onRowClick;
    private readonly Action<DiffTarget>? _onRowActivated;
    private readonly Action? _onEmptyAreaClicked;
    private readonly Func<DiffTarget?, IReadOnlyList<RepoBarContextMenu.Item>>? _buildContextMenu;
    private readonly TextView _headerText;
    private readonly TextView _emptyPlaceholder;
    private readonly RectView _bodyContainer;
    private readonly VirtualRowListView _list;
    private readonly VerticalScrollBarView _scrollBar;
    private readonly HorizontalScrollBarView _hScrollBar;

    private IReadOnlyList<FileChange> _files = Array.Empty<FileChange>();

    private readonly TextStyle _badgeGlyphStyle = new()
    {
        FontSize = 11f,
        HorizontalAlignment = TextAlignment.Center,
        VerticalAlignment = TextAlignment.Center,
    };
    private readonly TextStyle _pathTextStyle = new()
    {
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Start,
    };
    private readonly TextStyle _pathTextActiveStyle = new()
    {
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Start,
    };

    private FileChangeRowStyles _rowStyles = ThemeStyles.Dark.FileChangeRow;

    // Sentinel start so the first NotifyScrollChanged fires even when the computed scale
    // equals 1 — otherwise the scrollbar thumb's built-in 0.5 default sticks until a real
    // change forces an update. Same root cause as the fix in DiffContentView.
    private float _lastVerticalScale = -1f;
    private float _lastHorizontalScale = -1f;
    private float _lastNormalizedY;

    public IReadOnlyList<FileChange> Files => _files;

    public event Action<float>? VerticalScrollPositionChanged;
    public event Action<float>? HorizontalScrollPositionChanged;
    public float VerticalScale { get; private set; } = 1f;
    public float HorizontalScale { get; private set; } = 1f;

    public LocalChangesPanel(
        string title,
        DiffSide side,
        string emptyText,
        IReadable<Selection> selection,
        Action<DiffTarget, InputModifiers> onRowClick,
        IReadOnlyList<View>? headerActions = null,
        Action<DiffTarget>? onRowActivated = null,
        Action? onEmptyAreaClicked = null,
        Func<DiffTarget?, IReadOnlyList<RepoBarContextMenu.Item>>? buildContextMenu = null)
    {
        _title = title;
        _side = side;
        _selection = selection;
        _onRowClick = onRowClick;
        _onRowActivated = onRowActivated;
        _onEmptyAreaClicked = onEmptyAreaClicked;
        _buildContextMenu = buildContextMenu;

        _headerText = FileChangesUI.CreateHeaderText(title);
        _emptyPlaceholder = FileChangesUI.CreateEmptyPlaceholder(emptyText);

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

        _list = new VirtualRowListView
        {
            RowHeight = FileChangesUI.RowHeight,
            ItemBuilder = DrawFileRowAt,
        };
        _list.RowClicked += OnRowClicked;
        if (onRowActivated != null) _list.RowActivated += OnRowActivated;
        if (buildContextMenu != null) _list.RowContextRequested += OnRowContextRequested;
        _list.ScrollChanged += NotifyScrollChanged;

        // Empty placeholder swaps in as the body when there are no files; the widget swaps
        // back in when files arrive. Keeps the layout (header / center / scrollbars) intact.
        _bodyContainer = new RectView();
        _bodyContainer.Children.Add(_emptyPlaceholder);

        _scrollBar = ScrollBars.CreateVertical();
        _hScrollBar = ScrollBars.CreateHorizontal();

        AddChildToSelf(new BorderLayoutView
        {
            North = headerBar,
            Center = _bodyContainer,
            East = _scrollBar,
            South = _hScrollBar,
        });

        _list.UseController(_ => new VirtualRowListController(_list));

        // Selection changes only affect row visuals; a SetDirty is enough — every frame
        // redraws and the ItemBuilder reads the current selection on demand.
        selection.Subscribe(_ => SetDirty());

        this.BindThemed(s =>
        {
            _rowStyles = s.FileChangeRow;
            _badgeGlyphStyle.TextColor = _rowStyles.BadgeText;
            _pathTextStyle.TextColor = _rowStyles.RowText;
            _pathTextActiveStyle.TextColor = _rowStyles.RowTextActive;
            SetDirty();
        });

        this.UseBehavior(_ => new ScrollSyncController(this, _scrollBar, _hScrollBar));
    }

    public void SetFiles(IReadOnlyList<FileChange> files)
    {
        _files = files;
        _headerText.Text = FileChangesUI.FormatHeader(_title, files.Count);

        _bodyContainer.Children.Clear();
        if (files.Count == 0)
        {
            _bodyContainer.Children.Add(_emptyPlaceholder);
            _list.ItemCount = 0;
        }
        else
        {
            _bodyContainer.Children.Add(_list);
            _list.ItemCount = files.Count;
        }
        _list.SetScrollY(0f);
        _list.NotifyItemsChanged();
        NotifyScrollChanged();
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        // Resync every frame so layout changes (splitter drag, window resize) immediately
        // republish scale/normalized to the scrollbars. NotifyScrollChanged is dedup-protected,
        // so this is cheap when nothing actually changed.
        NotifyScrollChanged();
    }

    private void OnRowClicked(int rowIndex, InputModifiers modifiers)
    {
        if (rowIndex < 0 || rowIndex >= _files.Count)
        {
            _onEmptyAreaClicked?.Invoke();
            return;
        }
        _onRowClick(new DiffTarget(_files[rowIndex].Path, _side), modifiers);
    }

    private void OnRowActivated(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _files.Count) return;
        _onRowActivated?.Invoke(new DiffTarget(_files[rowIndex].Path, _side));
    }

    private void OnRowContextRequested(int rowIndex, PointF point)
    {
        if (Context == null || _buildContextMenu == null) return;

        var onRow = rowIndex >= 0 && rowIndex < _files.Count;
        var target = onRow ? new DiffTarget(_files[rowIndex].Path, _side) : null;

        var items = _buildContextMenu(target);
        if (items.Count == 0) return;

        if (onRow) _list.SetContextHighlight(rowIndex);
        var opened = RepoBarContextMenu.Show(Context, point, items);
        if (opened == null)
        {
            _list.SetContextHighlight(null);
            return;
        }
        opened.Closed += () => _list.SetContextHighlight(null);
    }

    private void DrawFileRowAt(ICanvas c, RectF rowRect, int rowIndex, RowRenderState state, int z)
    {
        if (rowIndex < 0 || rowIndex >= _files.Count) return;
        if (Context == null) return;

        var file = _files[rowIndex];
        var isSelected = _selection.Value.Contains(file.Path, _side);
        FileChangesUI.DrawFileRow(
            Context.Canvas,
            rowRect,
            file,
            isSelected,
            state.IsHovered,
            _rowStyles,
            _pathTextStyle,
            _pathTextActiveStyle,
            _badgeGlyphStyle,
            z);
    }

    // ---- IScrollableContent ----
    //
    // Horizontal scroll is intentionally inert here: the path text truncates to fit and the
    // status badge has fixed width, so the row never exceeds the viewport. We still wire
    // the bar so the layout slot stays consistent with the rest of the GitGui panels; the
    // bar collapses (PreferredHeight = 0) because Scale is always 1.

    public void SetVerticalNormalizedScrollPosition(float normalized)
    {
        var contentHeight = _files.Count * FileChangesUI.RowHeight;
        var bodyHeight = _list.Position.Height;
        var range = contentHeight - bodyHeight;
        _list.SetScrollY(range <= 0 ? 0f : Math.Clamp(normalized, 0f, 1f) * range);
    }

    public void SetHorizontalNormalizedScrollPosition(float normalized) { /* no-op */ }

    private void NotifyScrollChanged()
    {
        var contentHeight = _files.Count * FileChangesUI.RowHeight;
        var bodyHeight = _list.Position.Height;

        float vScale, normalizedY;
        if (contentHeight <= bodyHeight || bodyHeight <= 0)
        {
            vScale = 1f;
            normalizedY = 0f;
        }
        else
        {
            vScale = bodyHeight / contentHeight;
            var range = contentHeight - bodyHeight;
            normalizedY = Math.Clamp(_list.ScrollY / range, 0f, 1f);
        }

        VerticalScale = vScale;
        HorizontalScale = 1f;

        if (Math.Abs(vScale - _lastVerticalScale) > 0.0001f
            || Math.Abs(normalizedY - _lastNormalizedY) > 0.0001f)
        {
            _lastVerticalScale = vScale;
            _lastNormalizedY = normalizedY;
            VerticalScrollPositionChanged?.Invoke(normalizedY);
        }
        if (Math.Abs(1f - _lastHorizontalScale) > 0.0001f)
        {
            _lastHorizontalScale = 1f;
            HorizontalScrollPositionChanged?.Invoke(0f);
        }
    }
}
