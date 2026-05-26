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

    private ActionsToolbarViewModel? _vm;

    public ActionsToolbar()
    {
        PreferredHeight = ToolbarHeight;

        _pushButton = new ActionButton(LucideIcons.Push, "Push", () => _vm?.Push(),
            badgeColor: AheadBadgeColor);
        _pullButton = new ActionButton(LucideIcons.Pull, "Pull", () => _vm?.Pull(),
            badgeColor: BehindBadgeColor);
        _fetchButton = new ActionButton(LucideIcons.Fetch, "Fetch", () => _vm?.Fetch());
        _branchButton = new ActionButton(LucideIcons.Branch, "Branch", () => _vm?.Branch());
        _stashButton = new ActionButton(LucideIcons.Stash, "Stash", () => _vm?.Stash());
        _openFolderButton = new ActionButton(LucideIcons.FolderOpen, () => _vm?.OpenFolder(),
            tooltip: "Open in file explorer");
        _openTerminalButton = new ActionButton(LucideIcons.SquareTerminal, () => _vm?.OpenTerminal(),
            tooltip: "Open in terminal");

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
        _vm = vm;

        _pushButton.IsEnabled.BindTo(vm.PushEnabled);
        _pullButton.IsEnabled.BindTo(vm.PullEnabled);
        _fetchButton.IsEnabled.BindTo(vm.FetchEnabled);
        _branchButton.IsEnabled.BindTo(vm.RepoActionsEnabled);
        _openFolderButton.IsEnabled.BindTo(vm.RepoActionsEnabled);
        _openTerminalButton.IsEnabled.BindTo(vm.RepoActionsEnabled);
        _stashButton.IsEnabled.BindTo(vm.StashEnabled);

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
