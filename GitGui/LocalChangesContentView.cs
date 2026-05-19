using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// The body of the Local Changes view: two file-list panels (unstaged / staged) above a
/// diff pane, with a draggable splitter between them. <see cref="Bind"/> wires the
/// panels to a <see cref="LocalChangesViewModel"/>'s observable state and forwards stage
/// / unstage clicks to its commands. Selection is mutually exclusive across the two
/// panels and lives in the view — the diff pane only appears when exactly one row is
/// selected.
/// </summary>
internal sealed class LocalChangesContentView : MultiChildView
{
    private readonly LocalChangesPanel _unstagedPanel;
    private readonly LocalChangesPanel _stagedPanel;
    private readonly TextView _placeholder;
    private readonly RectView _centerContainer;
    private readonly DiffView _diffView;
    private readonly VerticalSplitContainer _snapshotContainer;
    private LocalChangesViewModel? _vm;

    public LocalChangesContentView()
    {
        _unstagedPanel = new LocalChangesPanel(
            "Unstaged",
            "No unstaged changes.",
            [
                (LucideIcons.ChevronRight, OnStageSelected),
                (LucideIcons.ChevronsRight, OnStageAll)
            ],
            path => _vm?.Stage([path]),
            onEmptyAreaClicked: ClearAllSelections);
        _stagedPanel = new LocalChangesPanel(
            "Staged",
            "No staged changes.",
            [
                (LucideIcons.ChevronsLeft, OnUnstageAll),
                (LucideIcons.ChevronLeft, OnUnstageSelected)
            ],
            path => _vm?.Unstage([path]),
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

    public void Bind(LocalChangesViewModel vm)
    {
        _vm = vm;

        // Placeholder fires before the list states (the VM mutates them in that order on
        // a snapshot transition), so the snapshot container is re-attached before the
        // panels receive their new files — important because SelectableFileRowView
        // construction inside a detached parent leaves the rows un-attached and they
        // render blank when the container later returns.
        vm.Placeholder.Subscribe(text =>
        {
            if (text != null) ShowPlaceholder(text);
            else AttachSnapshot();
        });
        vm.Unstaged.Subscribe(list => _unstagedPanel.SetFiles(list));
        vm.Staged.Subscribe(list => _stagedPanel.SetFiles(list));

        vm.SelectionRequested += (side, paths) =>
        {
            if (side == DiffSide.Unstaged) _unstagedPanel.SetSelection(paths);
            else _stagedPanel.SetSelection(paths);
        };
    }

    private void ShowPlaceholder(string text)
    {
        _placeholder.Text = text;
        _centerContainer.Children.Clear();
        _centerContainer.Children.Add(_placeholder);
    }

    private void AttachSnapshot()
    {
        // Re-attach the snapshot container BEFORE the panel SetFiles calls fire (see
        // Bind's ordering comment).
        _centerContainer.Children.Clear();
        _centerContainer.Children.Add(_snapshotContainer);
    }

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

    private void OnStageAll() => _vm?.Stage(_unstagedPanel.Files.Select(f => f.Path).ToList());
    private void OnStageSelected() => _vm?.Stage(_unstagedPanel.SelectedPaths.ToList());
    private void OnUnstageSelected() => _vm?.Unstage(_stagedPanel.SelectedPaths.ToList());
    private void OnUnstageAll() => _vm?.Unstage(_stagedPanel.Files.Select(f => f.Path).ToList());

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
