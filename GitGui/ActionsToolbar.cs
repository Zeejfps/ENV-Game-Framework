using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public sealed class ActionsToolbar : MultiChildView, IActionsToolbarView
{
    private const float ToolbarHeight = 44f;
    private const int HorizontalPadding = 8;

    // Spacing rhythm: tight gap within a button cluster, generous breathing room around
    // the vertical-line separators that mark zone boundaries. The contrast between the two
    // is what creates the visual grouping — uniform gaps would make everything read as one
    // long row regardless of how many separators we drew. Buttons inside a cluster sit
    // almost touching; zones are walled off by ~20px (separator breathing room + line +
    // breathing room + 2× cluster gap on the outside).
    private const float WithinClusterGap = 2f;
    private const float SeparatorBreathingRoom = 9f;
    private const float SeparatorWidth = 1f;
    private const float SeparatorHeight = 18f;

    // Ahead/behind palette mirrors BranchesView so the at-a-glance vocabulary ("green = need
    // to push", "amber = need to pull") is consistent between the sidebar branch rows and
    // the toolbar buttons. Duplicated literals rather than a shared const because these
    // belong to a shared *design* concept, not a shared module — co-locating them keeps
    // each surface readable on its own.
    private const uint AheadBadgeColor = 0xFF9DD17B;
    private const uint BehindBadgeColor = 0xFFE6A85C;

    private readonly ActionButton _pushButton;
    private readonly ActionButton _pullButton;
    private readonly ActionButton _fetchButton;
    private readonly ActionButton _branchButton;
    private readonly ActionButton _stashButton;
    private readonly ActionButton _openFolderButton;
    private readonly ActionButton _openTerminalButton;
    private readonly ErrorBar _errorBar;
    private readonly FlexRowView _contentRow;

    public event Action? PushRequested;
    public event Action? PullRequested;
    public event Action? FetchRequested;
    public event Action? OpenInFolderRequested;
    public event Action? OpenInTerminalRequested;
    public event Action? BranchRequested;
    public event Action? StashRequested;

    public ActionsToolbar()
    {
        PreferredHeight = ToolbarHeight;

        _pushButton = new ActionButton(LucideIcons.Push, "Push", () => PushRequested?.Invoke(),
            badgeColor: AheadBadgeColor);
        _pullButton = new ActionButton(LucideIcons.Pull, "Pull", () => PullRequested?.Invoke(),
            badgeColor: BehindBadgeColor);
        _fetchButton = new ActionButton(LucideIcons.Fetch, "Fetch", () => FetchRequested?.Invoke());
        _branchButton = new ActionButton(LucideIcons.Branch, "Branch", () => BranchRequested?.Invoke());
        _stashButton = new ActionButton(LucideIcons.Stash, "Stash", () => StashRequested?.Invoke());
        _openFolderButton = new ActionButton(LucideIcons.FolderOpen, () => OpenInFolderRequested?.Invoke(),
            tooltip: "Open in file explorer");
        _openTerminalButton = new ActionButton(LucideIcons.SquareTerminal, () => OpenInTerminalRequested?.Invoke(),
            tooltip: "Open in terminal");

        // Layout zones, left to right:
        //   [ Mode ]  |  [ Fetch Pull Push ]  |  [ Stash Branch ]  …  [ Folder Terminal ]
        //    status        remote sync             local ops              tools
        _contentRow = new FlexRowView
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
            }
        };
        _errorBar = new ErrorBar(_contentRow, verticalPadding: 2);

        AddChildToSelf(new RectView
        {
            BackgroundColor = DialogPalette.Background,
            BorderColor = new BorderColorStyle
            {
                Bottom = DialogPalette.Border
            },
            BorderSize = new BorderSizeStyle
            {
                Bottom = 1
            },
            Padding = new PaddingStyle
            {
                Left = HorizontalPadding,
                Right = HorizontalPadding,
            },
            Children = { _contentRow },
        });

        this.UsePresenter(ctx => new ActionsToolbarPresenter(
            this,
            ctx.Require<IRepoRegistry>(),
            ctx.Require<IGitService>(),
            ctx.Require<IPlatformShell>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    public bool PushEnabled { set => _pushButton.IsEnabled.Value = value; }
    public bool PullEnabled { set => _pullButton.IsEnabled.Value = value; }
    public bool FetchEnabled { set => _fetchButton.IsEnabled.Value = value; }
    public float PushRotation { set => _pushButton.IconRotation = value; }
    public float PullRotation { set => _pullButton.IconRotation = value; }
    public float FetchRotation { set => _fetchButton.IconRotation = value; }
    public string? Error { set => _errorBar.Message = value; }

    public bool RepoActionsEnabled
    {
        set
        {
            _openFolderButton.IsEnabled.Value = value;
            _openTerminalButton.IsEnabled.Value = value;
            _branchButton.IsEnabled.Value = value;
        }
    }

    public bool StashEnabled { set => _stashButton.IsEnabled.Value = value; }

    public int? PushBadge { set => _pushButton.Badge = value; }
    public int? PullBadge { set => _pullButton.Badge = value; }

    public bool PushBusy
    {
        set
        {
            _pushButton.Icon = value ? LucideIcons.Loader : LucideIcons.Push;
            _pushButton.Label = value ? "Pushing" : "Push";
            _pushButton.IconRotation = 0f;
        }
    }

    public bool PullBusy
    {
        set
        {
            _pullButton.Icon = value ? LucideIcons.Loader : LucideIcons.Pull;
            _pullButton.Label = value ? "Pulling" : "Pull";
            _pullButton.IconRotation = 0f;
        }
    }

    public bool FetchBusy
    {
        set
        {
            _fetchButton.Icon = value ? LucideIcons.Loader : LucideIcons.Fetch;
            _fetchButton.Label = value ? "Fetching" : "Fetch";
            _fetchButton.IconRotation = 0f;
        }
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
