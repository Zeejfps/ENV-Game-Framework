using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// One side of the Local Changes split (Unstaged or Staged). Renders a header bar with
/// action buttons, a scrollable list of file rows, and an empty-state placeholder.
/// Selection lives on the view model (one <see cref="GitGui.Selection"/> for both
/// sides) so the panel doesn't own any per-side selection state — it just hands the
/// shared <see cref="IReadable{Selection}"/> to each row so the row's highlight
/// binding is reactive, and forwards row clicks to a callback that routes into the
/// VM.
/// </summary>
internal sealed class LocalChangesPanel : MultiChildView
{
    private const int ContentPadding = 10;

    private readonly string _title;
    private readonly DiffSide _side;
    private readonly IReadable<Selection> _selection;
    private readonly Action<DiffTarget, InputModifiers> _onRowClick;
    private readonly Action<DiffTarget>? _onRowActivated;
    private readonly TextView _headerText;
    private readonly ColumnView _rows;
    private readonly TextView _emptyPlaceholder;
    private readonly ScrollPane _scrollPane;
    private readonly VerticalScrollBarView _scrollBar;
    private readonly HorizontalScrollBarView _hScrollBar;

    private IReadOnlyList<FileChange> _files = Array.Empty<FileChange>();

    public IReadOnlyList<FileChange> Files => _files;

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

    protected override void OnLayoutChildren()
    {
        base.OnLayoutChildren();
        _scrollBar.PreferredWidth = _scrollPane.VerticalScale < 1f ? ScrollBarSync.Thickness : 0f;
        _hScrollBar.PreferredHeight = _scrollPane.HorizontalScale < 1f ? ScrollBarSync.Thickness : 0f;
    }

    public void SetFiles(IReadOnlyList<FileChange> files)
    {
        _files = files;
        _headerText.Text = FileChangesUI.FormatHeader(_title, files.Count);
        _rows.Children.Clear();
        if (files.Count == 0)
        {
            _rows.Children.Add(_emptyPlaceholder);
        }
        else
        {
            foreach (var file in files)
                _rows.Children.Add(new SelectableFileRowView(file, _side, _selection, _onRowClick, _onRowActivated));
        }
        _scrollPane.ScrollToOrigin();
    }
}
