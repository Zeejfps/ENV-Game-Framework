using ZGF.Geometry;
using ZGF.Gui;
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
    private const float RowHeight = 22f;
    private const float RowPaddingLeft = 14f;
    private const float RowPaddingRight = 14f;
    private const float BadgeGap = 8f;

    private readonly string _title;
    private readonly DiffSide _side;
    private readonly IReadable<Selection> _selection;
    private readonly Action<DiffTarget, InputModifiers> _onRowClick;
    private readonly Action<DiffTarget>? _onRowActivated;
    private readonly Action? _onEmptyAreaClicked;
    private readonly TextView _headerText;
    private readonly TextView _emptyPlaceholder;
    private readonly RectView _bodyContainer;
    private readonly VirtualRowListView _list;
    private readonly VerticalScrollBarView _scrollBar;
    private readonly HorizontalScrollBarView _hScrollBar;

    private IReadOnlyList<FileChange> _files = Array.Empty<FileChange>();

    private readonly TextStyle _badgeGlyphStyle = new()
    {
        TextColor = FileChangesPalette.BadgeText,
        FontSize = 11f,
        HorizontalAlignment = TextAlignment.Center,
        VerticalAlignment = TextAlignment.Center,
    };
    private readonly TextStyle _pathTextStyle = new()
    {
        TextColor = DialogPalette.RowText,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Start,
    };
    private readonly TextStyle _pathTextActiveStyle = new()
    {
        TextColor = DialogPalette.RowTextActive,
        VerticalAlignment = TextAlignment.Center,
        HorizontalAlignment = TextAlignment.Start,
    };

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
        Action? onEmptyAreaClicked = null)
    {
        _title = title;
        _side = side;
        _selection = selection;
        _onRowClick = onRowClick;
        _onRowActivated = onRowActivated;
        _onEmptyAreaClicked = onEmptyAreaClicked;

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
            RowHeight = RowHeight,
            ItemBuilder = DrawFileRowAt,
        };
        _list.RowClicked += OnRowClicked;
        if (onRowActivated != null) _list.RowActivated += OnRowActivated;
        _list.ScrollChanged += NotifyScrollChanged;

        // Empty placeholder swaps in as the body when there are no files; the widget swaps
        // back in when files arrive. Keeps the layout (header / center / scrollbars) intact.
        _bodyContainer = new RectView();
        _bodyContainer.Children.Add(_emptyPlaceholder);

        _scrollBar = ScrollBarStyles.CreateVertical();
        _hScrollBar = ScrollBarStyles.CreateHorizontal();

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

        this.UseController(_ => new ScrollSyncController(this, _scrollBar, _hScrollBar));
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

    private void DrawFileRowAt(ICanvas c, RectF rowRect, int rowIndex, RowRenderState state, int z)
    {
        if (rowIndex < 0 || rowIndex >= _files.Count) return;
        if (Context == null) return;

        var file = _files[rowIndex];
        var path = file.Path;
        var isSelected = _selection.Value.Contains(path, _side);

        var bg = isSelected
            ? DialogPalette.RowActive
            : (state.IsHovered ? DialogPalette.RowHover : (uint?)null);
        if (bg != null)
        {
            c.DrawRect(new DrawRectInputs
            {
                Position = rowRect,
                Style = new RectStyle
                {
                    BackgroundColor = bg.Value,
                    BorderRadius = BorderRadiusStyle.All(3),
                },
                ZIndex = z,
            });
        }

        var badgeSize = FileChangesUI.BadgeSize;
        var badgeLeft = rowRect.Left + RowPaddingLeft;
        var badgeBottom = rowRect.Bottom + (RowHeight - badgeSize) * 0.5f;
        c.DrawRect(new DrawRectInputs
        {
            Position = new RectF(badgeLeft, badgeBottom, badgeSize, badgeSize),
            Style = new RectStyle
            {
                BackgroundColor = FileChangesPalette.StatusColor(file.Status),
                BorderRadius = BorderRadiusStyle.All(3),
            },
            ZIndex = z + 1,
        });
        c.DrawText(new DrawTextInputs
        {
            Position = new RectF(badgeLeft, badgeBottom, badgeSize, badgeSize),
            Text = FileChangesPalette.StatusGlyph(file.Status),
            Style = _badgeGlyphStyle,
            ZIndex = z + 2,
        });

        var textLeft = badgeLeft + badgeSize + BadgeGap;
        var textRight = rowRect.Right - RowPaddingRight;
        var textWidth = Math.Max(0f, textRight - textLeft);
        if (textWidth <= 0f) return;

        var pathStyle = isSelected ? _pathTextActiveStyle : _pathTextStyle;
        var pathText = FileChangesPalette.FormatPath(file);
        var rendered = TextMeasure.TruncateToFit(pathText, pathStyle, textWidth, Context.Canvas);
        c.DrawText(new DrawTextInputs
        {
            Position = new RectF(textLeft, rowRect.Bottom, textWidth, RowHeight),
            Text = rendered,
            Style = pathStyle,
            ZIndex = z + 2,
        });
    }

    // ---- IScrollableContent ----
    //
    // Horizontal scroll is intentionally inert here: the path text truncates to fit and the
    // status badge has fixed width, so the row never exceeds the viewport. We still wire
    // the bar so the layout slot stays consistent with the rest of the GitGui panels; the
    // bar collapses (PreferredHeight = 0) because Scale is always 1.

    public void SetVerticalNormalizedScrollPosition(float normalized)
    {
        var contentHeight = _files.Count * RowHeight;
        var bodyHeight = _list.Position.Height;
        var range = contentHeight - bodyHeight;
        _list.SetScrollY(range <= 0 ? 0f : Math.Clamp(normalized, 0f, 1f) * range);
    }

    public void SetHorizontalNormalizedScrollPosition(float normalized) { /* no-op */ }

    private void NotifyScrollChanged()
    {
        var contentHeight = _files.Count * RowHeight;
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
