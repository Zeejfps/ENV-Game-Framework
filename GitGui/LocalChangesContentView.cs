using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// The body of the Local Changes view: two file-list panels (unstaged / staged) above a
/// diff pane, with a draggable splitter between them. Selection is mutually exclusive
/// across the two panels, and the diff pane only appears when exactly one row is selected.
/// Swaps in a placeholder text when there is no snapshot to show.
/// </summary>
internal sealed class LocalChangesContentView : MultiChildView
{
    private readonly LocalChangesPanel _unstagedPanel;
    private readonly LocalChangesPanel _stagedPanel;
    private readonly TextView _placeholder;
    private readonly RectView _centerContainer;
    private readonly DiffView _diffView;
    private readonly VerticalSplitContainer _snapshotContainer;

    public event Action<IReadOnlyList<string>>? StageRequested;
    public event Action<IReadOnlyList<string>>? UnstageRequested;

    public LocalChangesContentView()
    {
        _unstagedPanel = new LocalChangesPanel(
            "Unstaged",
            "No unstaged changes.",
            [
                (LucideIcons.ChevronRight, OnStageSelected),
                (LucideIcons.ChevronsRight, OnStageAll)
            ],
            path => StageRequested?.Invoke([path]),
            onEmptyAreaClicked: ClearAllSelections);
        _stagedPanel = new LocalChangesPanel(
            "Staged",
            "No staged changes.",
            [
                (LucideIcons.ChevronsLeft, OnUnstageAll),
                (LucideIcons.ChevronLeft, OnUnstageSelected)
            ],
            path => UnstageRequested?.Invoke([path]),
            onEmptyAreaClicked: ClearAllSelections);

        _placeholder = new TextView
        {
            TextColor = CommitsPalette.Placeholder,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };

        _diffView = new DiffView();

        // Initial 1:2 split (files : diff). The container tracks the split as a fraction
        // of available height so window resizes scale both halves; the user can drag the
        // splitter to pick a different ratio, which then stays fractional across resizes.
        var splitterHovered = new State<bool>(false);
        var splitter = new RectView();
        splitter.BindBackgroundColor(splitterHovered,
            h => h ? CommitsPalette.DividerHoverBg : CommitsPalette.Border);

        _snapshotContainer = new VerticalSplitContainer(BuildContentRow(), _diffView, splitter, bottomFraction: 2f / 3f);

        splitter.UseController(ctx => new SplitterController(
            ctx,
            DragAxis.Y,
            _snapshotContainer.AdjustBottomFractionByPixels,
            h => splitterHovered.Value = h));

        _centerContainer = new RectView
        {
            BackgroundColor = CommitsPalette.Background,
            Children = { _snapshotContainer },
        };

        AddChildToSelf(_centerContainer);

        // Selection is exclusive across the two panels: once a row in one side is selected,
        // any selection on the other side is cleared. The "only clear when *becoming*
        // non-empty" guard means the cleared panel's own empty-transition doesn't bounce
        // back and wipe the panel that just took focus.
        _unstagedPanel.Selection.Subscribe(sel =>
        {
            if (sel.Count > 0) _stagedPanel.ClearSelection();
            UpdateDiffVisibility();
        });
        _stagedPanel.Selection.Subscribe(sel =>
        {
            if (sel.Count > 0) _unstagedPanel.ClearSelection();
            UpdateDiffVisibility();
        });
    }

    public void ShowPlaceholder(string text)
    {
        _placeholder.Text = text;
        _centerContainer.Children.Clear();
        _centerContainer.Children.Add(_placeholder);
    }

    public void ShowSnapshot(IReadOnlyList<FileChange> unstaged, IReadOnlyList<FileChange> staged)
    {
        // Re-attach the snapshot container BEFORE populating panels. After a checkout the
        // load transitions via Placeholder("Loading…"), which swaps `_placeholder` into
        // `_centerContainer` and leaves `_snapshotContainer` (and the panels inside it) as
        // a detached subtree. Appending new row views to a detached parent leaves them
        // un-attached, and the rows render blank when the container later returns — even
        // though the header text (a string mutation on a still-live TextView) updates fine.
        // Attaching first means every SelectableFileRowView SetFiles adds is born into an
        // attached parent.
        _centerContainer.Children.Clear();
        _centerContainer.Children.Add(_snapshotContainer);
        _unstagedPanel.SetFiles(unstaged);
        _stagedPanel.SetFiles(staged);
        // SetFiles clears both panels' selections, which fires the selection subscriptions
        // and drives UpdateDiffVisibility — so the diff item collapses on its own here.
    }

    public void SetStagedFiles(IReadOnlyList<FileChange> files) => _stagedPanel.SetFiles(files);

    public void SelectUnstaged(IReadOnlyList<string> paths) => _unstagedPanel.SetSelection(paths);
    public void SelectStaged(IReadOnlyList<string> paths) => _stagedPanel.SetSelection(paths);

    private View BuildContentRow()
    {
        var divider = new RectView { PreferredWidth = 1, BackgroundColor = CommitsPalette.Border };

        // Custom layout instead of FlexRowView: with flex, each panel's content's natural
        // width (long file paths in unstaged, short placeholder in staged) leaks into the
        // distribution and the panels end up unequal. Here we measure only the center
        // divider and split the remainder strictly in half.
        return new TransferListRow(_unstagedPanel, divider, _stagedPanel);
    }

    private void ClearAllSelections()
    {
        _unstagedPanel.ClearSelection();
        _stagedPanel.ClearSelection();
    }

    private void OnStageAll() => StageRequested?.Invoke(_unstagedPanel.Files.Select(f => f.Path).ToList());
    private void OnStageSelected() => StageRequested?.Invoke(_unstagedPanel.SelectedPaths.ToList());
    private void OnUnstageSelected() => UnstageRequested?.Invoke(_stagedPanel.SelectedPaths.ToList());
    private void OnUnstageAll() => UnstageRequested?.Invoke(_stagedPanel.Files.Select(f => f.Path).ToList());

    private void UpdateDiffVisibility()
    {
        // Selections are mutually exclusive across the two panels (see constructor),
        // so the combined count is whichever panel currently holds anything.
        var unstaged = _unstagedPanel.SelectedPaths;
        var staged = _stagedPanel.SelectedPaths;
        var total = unstaged.Count + staged.Count;

        if (total == 1)
        {
            var side = unstaged.Count == 1 ? DiffSide.Unstaged : DiffSide.Staged;
            var path = unstaged.Count == 1 ? unstaged.First() : staged.First();
            _diffView.SetTarget(path, side);
            _snapshotContainer.BottomVisible = true;
        }
        else
        {
            _diffView.SetTarget(null, DiffSide.Unstaged);
            _snapshotContainer.BottomVisible = false;
        }
    }
}
