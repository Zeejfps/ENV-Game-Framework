using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class ActionsToolbar : MultiChildView
{
    private const float ToolbarHeight = 44f;
    private const int HorizontalPadding = 8;
    private const float WithinClusterGap = 2f;
    private const float SeparatorBreathingRoom = 9f;
    private const float SeparatorWidth = 1f;
    private const float SeparatorHeight = 18f;
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

        vm.PushEnabled.Subscribe(b => _pushButton.IsEnabled.Value = b);
        vm.PullEnabled.Subscribe(b => _pullButton.IsEnabled.Value = b);
        vm.FetchEnabled.Subscribe(b => _fetchButton.IsEnabled.Value = b);
        vm.RepoActionsEnabled.Subscribe(b =>
        {
            _openFolderButton.IsEnabled.Value = b;
            _openTerminalButton.IsEnabled.Value = b;
            _branchButton.IsEnabled.Value = b;
        });
        vm.StashEnabled.Subscribe(b => _stashButton.IsEnabled.Value = b);
        vm.PushBadge.Subscribe(badge => _pushButton.Badge = badge);
        vm.PullBadge.Subscribe(badge => _pullButton.Badge = badge);

        vm.IsPushing.Subscribe(b =>
        {
            _pushButton.Icon = b ? LucideIcons.Loader : LucideIcons.Push;
            _pushButton.Label = b ? "Pushing" : "Push";
            if (!b) _pushButton.IconRotation = 0f;
        });
        vm.IsPulling.Subscribe(b =>
        {
            _pullButton.Icon = b ? LucideIcons.Loader : LucideIcons.Pull;
            _pullButton.Label = b ? "Pulling" : "Pull";
            if (!b) _pullButton.IconRotation = 0f;
        });
        vm.IsFetching.Subscribe(b =>
        {
            _fetchButton.Icon = b ? LucideIcons.Loader : LucideIcons.Fetch;
            _fetchButton.Label = b ? "Fetching" : "Fetch";
            if (!b) _fetchButton.IconRotation = 0f;
        });
        vm.PushRotation.Subscribe(r => _pushButton.IconRotation = r);
        vm.PullRotation.Subscribe(r => _pullButton.IconRotation = r);
        vm.FetchRotation.Subscribe(r => _fetchButton.IconRotation = r);
        vm.Error.Subscribe(msg => _errorBar.Message = msg);
    }

    private sealed class SeparatorSpacer : MultiChildView
    {
        public SeparatorSpacer()
        {
            PreferredWidth = SeparatorWidth + SeparatorBreathingRoom * 2;
            AddChildToSelf(new FlexRowView
            {
                CrossAxisAlignment = CrossAxisAlignment.Center,
                MainAxisAlignment = MainAxisAlignment.Center,
                Children =
                {
                    new RectView
                    {
                        BackgroundColor = DialogPalette.Border,
                        PreferredWidth = SeparatorWidth,
                        PreferredHeight = SeparatorHeight,
                    },
                },
            });
        }
    }
}
