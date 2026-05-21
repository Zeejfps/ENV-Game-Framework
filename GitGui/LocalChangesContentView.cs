using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// The body of the Local Changes view: two file-list panels (unstaged / staged) above a
/// diff pane, with a draggable splitter between them. <see cref="Bind"/> wires the
/// panels to a <see cref="LocalChangesViewModel"/>'s observable state and forwards stage
/// / unstage clicks and row selection back to the VM. Selection is owned by the VM
/// (one <see cref="GitGui.Selection"/> for both sides), so the panels are stateless
/// w.r.t. selection — rows highlight reactively against the shared selection, and the
/// diff view's target tracks <c>SelectedTarget</c> directly.
/// </summary>
internal sealed class LocalChangesContentView : MultiChildView
{
    private readonly LocalChangesPanel _unstagedPanel;
    private readonly LocalChangesPanel _stagedPanel;
    private readonly TextView _placeholder;
    private readonly RectView _centerContainer;
    private readonly DiffView _diffView;
    private readonly VerticalSplitContainer _snapshotContainer;
    private readonly LocalChangesHeaderActionButton _discardButton;
    private readonly LocalChangesHeaderActionButton _stageSelectedButton;
    private readonly LocalChangesSubmoduleSection _submoduleSection;
    private readonly BorderLayoutView _topHalf;

    // View-side mirror of the VM's Selection, wired in Bind. Lives here so the panels
    // and their rows can be constructed before Bind() — at construction we hand them
    // this State; in Bind we subscribe vm.Selection so updates flow through.
    private readonly State<Selection> _selection = new(Selection.Empty);
    private LocalChangesViewModel? _vm;

    public LocalChangesContentView()
    {
        _discardButton = new LocalChangesHeaderActionButton(
            LucideIcons.Trash, OnDiscardSelected, "Discard selected changes");
        _discardButton.IsEnabled.Value = false;
        _stageSelectedButton = new LocalChangesHeaderActionButton(
            LucideIcons.ChevronRight, OnStageSelected, "Stage selected");
        _stageSelectedButton.IsEnabled.Value = false;

        _unstagedPanel = new LocalChangesPanel(
            "Unstaged",
            DiffSide.Unstaged,
            "No unstaged changes.",
            _selection,
            OnRowClick,
            [
                _discardButton,
                _stageSelectedButton,
                new LocalChangesHeaderActionButton(LucideIcons.ChevronsRight, OnStageAll, "Stage all"),
            ],
            onRowActivated: t => _vm?.Stage([t.Path]),
            onEmptyAreaClicked: () => _vm?.ClearSelection());
        _stagedPanel = new LocalChangesPanel(
            "Staged",
            DiffSide.Staged,
            "No staged changes.",
            _selection,
            OnRowClick,
            [
                new LocalChangesHeaderActionButton(LucideIcons.ChevronsLeft, OnUnstageAll, "Unstage all"),
                new LocalChangesHeaderActionButton(LucideIcons.ChevronLeft, OnUnstageSelected, "Unstage selected"),
            ],
            onRowActivated: t => _vm?.Unstage([t.Path]),
            onEmptyAreaClicked: () => _vm?.ClearSelection());

        _placeholder = new TextView
        {
            TextColor = CommitsPalette.Placeholder,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };

        _submoduleSection = new LocalChangesSubmoduleSection(
            onStage: path => _vm?.StageSubmodulePointer(path),
            onReset: path => _vm?.ResetSubmoduleToRecorded(path));

        _diffView = new DiffView();

        // Initial 1:2 split (files : diff). The container tracks the split as a fraction
        // of available height so window resizes scale both halves; the user can drag the
        // splitter to pick a different ratio, which then stays fractional across resizes.
        var splitterHovered = new State<bool>(false);
        var splitter = new RectView();
        splitter.BindBackgroundColor(splitterHovered,
            h => h ? CommitsPalette.DividerHoverBg : CommitsPalette.Border);

        // Top half of the split: optional submodule drift section (North, populated only
        // when there's drift) over the two file panels. North = null hides the section so
        // the layout collapses and the panels reclaim the full space.
        _topHalf = new BorderLayoutView
        {
            Center = BuildContentRow(),
        };
        _snapshotContainer = new VerticalSplitContainer(_topHalf, _diffView, splitter, bottomFraction: 2f / 3f);

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
    }

    public void Bind(LocalChangesViewModel vm)
    {
        _vm = vm;
        
        vm.Placeholder.Subscribe(text =>
        {
            if (text != null) ShowPlaceholder(text);
            else AttachSnapshot();
        });
        vm.Unstaged.Subscribe(list => _unstagedPanel.SetFiles(list));
        vm.Staged.Subscribe(list => _stagedPanel.SetFiles(list));
        vm.Selection.Subscribe(sel => _selection.Value = sel);
        vm.SelectedTarget.Subscribe(target =>
        {
            _diffView.SetTarget(target?.Path, target?.Side ?? DiffSide.Unstaged);
            ApplyDiffVisibility(target != null, _diffView.IsCollapsed.Value);
        });
        _diffView.IsCollapsed.Subscribe(collapsed =>
            ApplyDiffVisibility(vm.SelectedTarget.Value != null, collapsed));

        vm.DiscardEnabled.Subscribe(enabled => _discardButton.IsEnabled.Value = enabled);
        vm.StageSelectedEnabled.Subscribe(enabled => _stageSelectedButton.IsEnabled.Value = enabled);

        vm.DriftedSubmodules.Subscribe(drift =>
        {
            _submoduleSection.SetDrift(drift);
            _topHalf.North = drift.Count > 0 ? _submoduleSection : null;
        });
    }

    private void ApplyDiffVisibility(bool hasTarget, bool collapsed)
    {
        _snapshotContainer.BottomVisible = hasTarget;
        _snapshotContainer.SetBottomCollapsed(hasTarget && collapsed, DiffView.HeaderHeight);
    }

    private void ShowPlaceholder(string text)
    {
        _placeholder.Text = text;
        _centerContainer.Children.Clear();
        _centerContainer.Children.Add(_placeholder);
    }

    private void AttachSnapshot()
    {
        _centerContainer.Children.Clear();
        _centerContainer.Children.Add(_snapshotContainer);
    }

    private View BuildContentRow()
    {
        var divider = new RectView { PreferredWidth = 1, BackgroundColor = CommitsPalette.Border };
        return new TransferListRow(_unstagedPanel, divider, _stagedPanel);
    }

    private void OnRowClick(DiffTarget target, InputModifiers modifiers)
        => _vm?.SelectRow(target.Path, target.Side, modifiers);

    private void OnStageAll()
        => _vm?.Stage(_unstagedPanel.Files.Select(f => f.Path).ToList());

    private void OnUnstageAll()
        => _vm?.Unstage(_stagedPanel.Files.Select(f => f.Path).ToList());

    private void OnStageSelected()
        => _vm?.Stage(_selection.Value.PathsOn(DiffSide.Unstaged));

    private void OnUnstageSelected()
        => _vm?.Unstage(_selection.Value.PathsOn(DiffSide.Staged));

    private void OnDiscardSelected()
    {
        var paths = _selection.Value.PathsOn(DiffSide.Unstaged);
        if (paths.Count == 0) return;
        _vm?.RequestDiscard(paths);
    }
}
