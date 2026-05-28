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
internal sealed class LocalChangesContentView : MultiChildView, IBind<LocalChangesViewModel>
{
    private readonly LocalChangesPanel _unstagedPanel;
    private readonly LocalChangesPanel _stagedPanel;
    private readonly TextView _placeholder;
    private readonly RectView _centerContainer;
    private readonly DiffView _diffView;
    private readonly VerticalSplitContainer _snapshotContainer;
    private readonly LocalChangesHeaderActionButton _discardButton;
    private readonly LocalChangesHeaderActionButton _stageSelectedButton;
    private readonly LocalChangesHeaderActionButton _stageAllButton;
    private readonly LocalChangesHeaderActionButton _unstageAllButton;
    private readonly LocalChangesHeaderActionButton _unstageSelectedButton;
    private readonly LocalChangesSubmoduleSection _submoduleSection;
    private readonly BorderLayoutView _topHalf;
    
    private readonly State<Selection> _selection = new(Selection.Empty);
    private LocalChangesViewModel? _vm;
    private LocalChangesArrowKbmController? _arrowController;

    public LocalChangesContentView()
    {
        _discardButton = new LocalChangesHeaderActionButton(
            LucideIcons.Trash, tooltip: "Discard selected changes");
        _stageSelectedButton = new LocalChangesHeaderActionButton(
            LucideIcons.ChevronRight, tooltip: "Stage selected");
        _stageAllButton = new LocalChangesHeaderActionButton(
            LucideIcons.ChevronsRight, tooltip: "Stage all");
        _unstageAllButton = new LocalChangesHeaderActionButton(
            LucideIcons.ChevronsLeft, tooltip: "Unstage all");
        _unstageSelectedButton = new LocalChangesHeaderActionButton(
            LucideIcons.ChevronLeft, tooltip: "Unstage selected");

        _unstagedPanel = new LocalChangesPanel(
            "Unstaged",
            DiffSide.Unstaged,
            "No unstaged changes.",
            _selection,
            OnRowClick,
            [_discardButton, _stageSelectedButton, _stageAllButton],
            onRowActivated: t => _vm?.Stage([t.Path]),
            onEmptyAreaClicked: () => _vm?.ClearSelection(),
            buildContextMenu: BuildUnstagedMenu);
        _stagedPanel = new LocalChangesPanel(
            "Staged",
            DiffSide.Staged,
            "No staged changes.",
            _selection,
            OnRowClick,
            [_unstageAllButton, _unstageSelectedButton],
            onRowActivated: t => _vm?.Unstage([t.Path]),
            onEmptyAreaClicked: () => _vm?.ClearSelection(),
            buildContextMenu: BuildStagedMenu);

        _placeholder = new TextView
        {
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _placeholder.BindThemedTextColor(s => s.LocalChangesContent.PlaceholderText);

        _submoduleSection = new LocalChangesSubmoduleSection(
            onStage: path => _vm?.StageSubmodulePointer(path),
            onReset: path => _vm?.ResetSubmoduleToRecorded(path));

        _diffView = new DiffView(); 
        
        var splitterHovered = new State<bool>(false);
        var splitter = new RectView();
        splitter.BindThemedBackgroundColor(s =>
            splitterHovered.Value ? s.LocalChangesContent.SplitterHover : s.LocalChangesContent.SplitterIdle);
        
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
            Children = { _snapshotContainer },
        };
        _centerContainer.BindThemedBackgroundColor(s => s.LocalChangesContent.ContentBackground);

        AddChildToSelf(_centerContainer);

        this.UseController(_ =>
        {
            _arrowController = new LocalChangesArrowKbmController(
                this,
                (delta, extend) => _vm?.MoveSelection(delta, extend));
            return _arrowController;
        });
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
        _selection.BindTo(vm.Selection);
        _diffView.Bind(vm.DiffVm);
        _snapshotContainer.BindBottomVisible(() => vm.SelectedTarget.Value != null);
        _snapshotContainer.BindBottomCollapsed(_diffView.IsCollapsed, DiffView.HeaderHeight);

        _discardButton.BindCommand(vm.Discard);
        _stageSelectedButton.BindCommand(vm.StageSelected);
        _stageAllButton.BindCommand(vm.StageAll);
        _unstageAllButton.BindCommand(vm.UnstageAll);
        _unstageSelectedButton.BindCommand(vm.UnstageSelected);

        vm.DriftedSubmodules.Subscribe(drift =>
        {
            _submoduleSection.SetDrift(drift);
            _topHalf.North = drift.Count > 0 ? _submoduleSection : null;
        });
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
        var divider = new RectView { PreferredWidth = 1 };
        divider.BindThemedBackgroundColor(s => s.LocalChangesContent.ColumnDivider);
        return new TransferListRow(_unstagedPanel, divider, _stagedPanel);
    }

    private void OnRowClick(DiffTarget target, InputModifiers modifiers)
    {
        _vm?.SelectRow(target.Path, target.Side, modifiers);
        _arrowController?.TakeFocus();
    }

    private IReadOnlyList<RepoBarContextMenu.Item> BuildUnstagedMenu(DiffTarget? target)
    {
        if (_vm == null) return [];
        var items = new List<RepoBarContextMenu.Item>();
        if (target != null)
        {
            var paths = ResolveTargetPaths(target);
            var n = paths.Count;
            items.Add(new RepoBarContextMenu.Item(
                n > 1 ? $"Stage {n} Files" : "Stage",
                () => _vm.Stage(paths),
                LucideIcons.ChevronRight));
            items.Add(new RepoBarContextMenu.Item(
                n > 1 ? $"Discard {n} Files…" : "Discard…",
                () => _vm.RequestDiscard(paths),
                LucideIcons.Trash));
            items.Add(RepoBarContextMenu.Separator);
        }
        items.Add(new RepoBarContextMenu.Item(
            "Stage All",
            () => _vm.StageAll.Execute(),
            LucideIcons.ChevronsRight,
            Enabled: _vm.StageAll.CanExecute.Value));
        items.Add(new RepoBarContextMenu.Item(
            "Discard All…",
            () => _vm.DiscardAll.Execute(),
            LucideIcons.Trash,
            Enabled: _vm.DiscardAll.CanExecute.Value));
        return items;
    }

    private IReadOnlyList<RepoBarContextMenu.Item> BuildStagedMenu(DiffTarget? target)
    {
        if (_vm == null) return [];
        var items = new List<RepoBarContextMenu.Item>();
        if (target != null)
        {
            var paths = ResolveTargetPaths(target);
            var n = paths.Count;
            items.Add(new RepoBarContextMenu.Item(
                n > 1 ? $"Unstage {n} Files" : "Unstage",
                () => _vm.Unstage(paths),
                LucideIcons.ChevronLeft));
            items.Add(RepoBarContextMenu.Separator);
        }
        items.Add(new RepoBarContextMenu.Item(
            "Unstage All",
            () => _vm.UnstageAll.Execute(),
            LucideIcons.ChevronsLeft,
            Enabled: _vm.UnstageAll.CanExecute.Value));
        return items;
    }

    // Right-clicking a row inside the current selection acts on the whole selection;
    // right-clicking outside it acts on just that row.
    private IReadOnlyList<string> ResolveTargetPaths(DiffTarget target)
    {
        var selected = _vm!.Selection.Value.PathsOn(target.Side);
        return selected.Contains(target.Path) ? selected : [target.Path];
    }

}
