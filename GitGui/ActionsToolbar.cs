using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

internal sealed class ActionsToolbar : MultiChildView, IBind<ActionsToolbarViewModel>
{
    private const float ToolbarHeight = 44f;
    private const int HorizontalPadding = 8;
    private const float WithinClusterGap = 2f;

    private readonly ActionButton _pushButton;
    private readonly ActionButton _pullButton;
    private readonly ActionButton _fetchButton;
    private readonly ActionButton _branchButton;
    private readonly ActionButton _stashButton;
    private readonly ActionButton _openFolderButton;
    private readonly ActionButton _openTerminalButton;
    private readonly ErrorBarView _errorBar;

    public ActionsToolbar()
    {
        PreferredHeight = ToolbarHeight;

        // Badge colors are accent hints (ahead green / behind orange) — passed at construction
        // and not theme-reactive yet. ActionButton would need a State<uint> badge color to
        // pick up live theme swaps; deferred until a follow-up.
        _pushButton = new ActionButton(LucideIcons.Push, "Push", badgeColor: ThemePresets.Dark.Commits.AheadColor);
        _pullButton = new ActionButton(LucideIcons.Pull, "Pull", badgeColor: ThemePresets.Dark.Commits.BehindColor);
        _fetchButton = new ActionButton(LucideIcons.Fetch, "Fetch");
        _branchButton = new ActionButton(LucideIcons.Branch, "Branch");
        _stashButton = new ActionButton(LucideIcons.Stash, "Stash");
        _openFolderButton = new ActionButton(LucideIcons.FolderOpen, tooltip: "Open in file explorer");
        _openTerminalButton = new ActionButton(LucideIcons.SquareTerminal, tooltip: "Open in terminal");

        _errorBar = new ErrorBarView(verticalPadding: 2);
        var contentRow = new FlexRowView
        {
            Gap = WithinClusterGap,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children =
            {
                new ModeSwitcherView(),
                new SeparatorSpacer(),
                _fetchButton,
                _pullButton,
                _pushButton,
                new SeparatorSpacer(),
                _stashButton,
                _branchButton,
                new FlexItem { Grow = 1, Child = new MultiChildView() },
                _openFolderButton,
                _openTerminalButton,
                _errorBar,
            }
        };

        var frame = new RectView
        {
            BorderSize = new BorderSizeStyle { Bottom = 1 },
            Padding = new PaddingStyle
            {
                Left = HorizontalPadding,
                Right = HorizontalPadding,
            },
            Children = { contentRow },
        };
        frame.BindBackgroundColorFromTheme(t => t.Dialog.Background);
        frame.BindBorderColorFromTheme(t => new BorderColorStyle { Bottom = t.Dialog.Border });
        AddChildToSelf(frame);

        this.UseViewModel(this);
    }

    public void Bind(ActionsToolbarViewModel vm)
    {
        _pushButton.BindCommand(vm.Push);
        _pullButton.BindCommand(vm.Pull);
        _fetchButton.BindCommand(vm.Fetch);
        _branchButton.BindCommand(vm.Branch);
        _stashButton.BindCommand(vm.Stash);
        _openFolderButton.BindCommand(vm.OpenFolder);
        _openTerminalButton.BindCommand(vm.OpenTerminal);

        _pushButton.Badge.BindTo(vm.PushBadge);
        _pullButton.Badge.BindTo(vm.PullBadge);

        _pushButton.Icon.BindTo(vm.IsPushing, b => b ? LucideIcons.Loader : LucideIcons.Push);
        _pushButton.Label.BindTo(vm.IsPushing, b => b ? "Pushing" : "Push");
        _pushButton.IconRotation.BindTo(vm.PushRotation);

        _pullButton.Icon.BindTo(vm.IsPulling, b => b ? LucideIcons.Loader : LucideIcons.Pull);
        _pullButton.Label.BindTo(vm.IsPulling, b => b ? "Pulling" : "Pull");
        _pullButton.IconRotation.BindTo(vm.PullRotation);

        _fetchButton.Icon.BindTo(vm.IsFetching, b => b ? LucideIcons.Loader : LucideIcons.Fetch);
        _fetchButton.Label.BindTo(vm.IsFetching, b => b ? "Fetching" : "Fetch");
        _fetchButton.IconRotation.BindTo(vm.FetchRotation);

        _errorBar.Message.BindTo(vm.Error);
    }
}
