using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public sealed class ActionsToolbar : MultiChildView
{
    private const float ToolbarHeight = 44f;
    private const int HorizontalPadding = 8;
    private const float WithinClusterGap = 2f;
    private const uint AheadBadgeColor = 0xFF9DD17B;
    private const uint BehindBadgeColor = 0xFFE6A85C;

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

        _pushButton = new ActionButton(LucideIcons.Push, "Push", badgeColor: AheadBadgeColor);
        _pullButton = new ActionButton(LucideIcons.Pull, "Pull", badgeColor: BehindBadgeColor);
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

        AddChildToSelf(new RectView
        {
            BackgroundColor = DialogPalette.Background,
            BorderColor = new BorderColorStyle { Bottom = DialogPalette.Border },
            BorderSize = new BorderSizeStyle { Bottom = 1 },
            Padding = new PaddingStyle
            {
                Left = HorizontalPadding,
                Right = HorizontalPadding,
            },
            Children = { contentRow },
        });

        this.UseViewModel<ActionsToolbarViewModel>(Bind);
    }

    private void Bind(ActionsToolbarViewModel vm)
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
