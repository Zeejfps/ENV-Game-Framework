using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public sealed class ActionsToolbar : MultiChildView, IActionsToolbarView
{
    private const float ToolbarHeight = 44f;
    private const int HorizontalPadding = 8;
    private const float GroupGap = 4f;
    private const float SeparatorBreathingRoom = 8f;
    private const float SeparatorWidth = 1f;
    private const float SeparatorHeight = 16f;

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

        _pushButton = new ActionButton(LucideIcons.Push, "Push", () => PushRequested?.Invoke());
        _pullButton = new ActionButton(LucideIcons.Pull, "Pull", () => PullRequested?.Invoke());
        _fetchButton = new ActionButton(LucideIcons.Fetch, "Fetch", () => FetchRequested?.Invoke());
        _branchButton = new ActionButton(LucideIcons.Branch, "Branch", () => BranchRequested?.Invoke());
        _stashButton = new ActionButton(LucideIcons.Stash, "Stash", () => StashRequested?.Invoke());
        _openFolderButton = new ActionButton(LucideIcons.FolderOpen, () => OpenInFolderRequested?.Invoke(),
            tooltip: "Open in file explorer");
        _openTerminalButton = new ActionButton(LucideIcons.SquareTerminal, () => OpenInTerminalRequested?.Invoke(),
            tooltip: "Open in terminal");

        _contentRow = new FlexRowView
        {
            Gap = GroupGap,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children =
            {
                new ModeSwitcherView(),
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
            _stashButton.IsEnabled.Value = value;
        }
    }

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
