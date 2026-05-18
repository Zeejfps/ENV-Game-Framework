using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public sealed class ActionsToolbar : MultiChildView
{
    private const float ToolbarHeight = 44f;
    private const int HorizontalPadding = 8;
    private const float GroupGap = 4f;
    private const float SeparatorBreathingRoom = 8f;
    private const float SeparatorWidth = 1f;
    private const float SeparatorHeight = 16f;

    private readonly ActionButton _pushButton;
    private readonly ErrorBar _errorBar;
    private readonly FlexRowView _contentRow;

    private IRepoRegistry? _registry;
    private IGitService? _gitService;
    private IMessageBus? _bus;
    private IUiDispatcher? _dispatcher;

    private readonly SubscriptionGroup _subscriptions = new();

    private readonly GenerationGuard _statusGen = new();
    private bool _isPushing;
    private PushStatus _pushStatus = new(null, HasUpstream: false, Ahead: 0, Behind: 0, IsDetached: false);
    private CancellationTokenSource? _pushAnimCts;

    public ActionsToolbar()
    {
        PreferredHeight = ToolbarHeight;

        _pushButton = new ActionButton(LucideIcons.Push, "Push", OnPushClicked);
        _pushButton.IsEnabled.Value = false;

        _contentRow = new FlexRowView
        {
            Gap = GroupGap,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children =
            {
                new ModeSwitcherView(),
                new ActionButton(LucideIcons.Fetch, "Fetch", () => { }),
                new ActionButton(LucideIcons.Pull, "Pull", () => { }),
                _pushButton,
                new SeparatorSpacer(),
                new ActionButton(LucideIcons.Stash, "Stash", () => { }),
                new ActionButton(LucideIcons.Branch, "Branch", () => { }),
            }
        };
        _errorBar = new ErrorBar(_contentRow, verticalPadding: 2);

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
            Children = { _contentRow },
        });
    }

    protected override void OnAttachedToContext(Context context)
    {
        _registry = context.Get<IRepoRegistry>();
        _gitService = context.Get<IGitService>();
        _bus = context.Get<IMessageBus>();
        _dispatcher = context.Get<IUiDispatcher>();

        _subscriptions.Add(_registry?.Active.Subscribe(_ => OnRepoOrRefsChanged()));
        _subscriptions.Add(_bus?.SubscribeScoped<CommitCreatedMessage>(_ => OnRepoOrRefsChanged()));
        _subscriptions.Add(_bus?.SubscribeScoped<RefsChangedMessage>(_ => OnRepoOrRefsChanged()));
    }

    protected override void OnDetachedFromContext(Context context)
    {
        _statusGen.Bump();
        _pushAnimCts?.Cancel();
        _pushAnimCts?.Dispose();
        _pushAnimCts = null;
        _subscriptions.Dispose();
        _registry = null;
        _gitService = null;
        _bus = null;
        _dispatcher = null;
    }

    private void OnRepoOrRefsChanged()
    {
        ShowError(null);
        ReloadPushStatus();
    }

    private void ReloadPushStatus()
    {
        var service = _gitService;
        var dispatcher = _dispatcher;
        var repo = _registry?.Active.Value;
        if (service == null || repo == null)
        {
            _pushStatus = new PushStatus(null, HasUpstream: false, Ahead: 0, Behind: 0, IsDetached: false);
            UpdatePushButton();
            return;
        }

        var gen = _statusGen.Bump();
        Task.Run(() =>
        {
            var status = service.GetPushStatus(repo);
            dispatcher?.Post(() =>
            {
                if (_statusGen.IsStale(gen)) return;
                if (_registry?.Active.Value?.Id != repo.Id) return;
                _pushStatus = status;
                UpdatePushButton();
            });
        });
    }

    private void UpdatePushButton()
    {
        var canPush = !_isPushing
            && !_pushStatus.IsDetached
            && _pushStatus.HasUpstream
            && _pushStatus.Ahead > 0;
        _pushButton.IsEnabled.Value = canPush;
    }

    private void OnPushClicked()
    {
        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;
        var repo = _registry?.Active.Value;
        if (service == null || repo == null) return;
        if (_isPushing) return;

        _isPushing = true;
        UpdatePushButton();
        ShowError(null);
        StartPushingAnimation();

        Task.Run(() =>
        {
            PushOutcome outcome;
            try { outcome = service.Push(repo); }
            catch (Exception ex) { outcome = new PushOutcome(false, ex.Message); }

            dispatcher?.Post(() =>
            {
                _isPushing = false;
                StopPushingAnimation();
                if (!outcome.Success)
                {
                    ShowError(outcome.ErrorMessage ?? "Push failed.");
                    UpdatePushButton();
                    return;
                }

                bus?.Broadcast(new RefsChangedMessage(repo.Id));
                // Broadcast also re-runs ReloadPushStatus via our own subscription, so we
                // don't call it directly here.
            });
        });
    }

    // Swaps the button into a "working" state and spins the loader glyph.
    private void StartPushingAnimation()
    {
        _pushAnimCts?.Cancel();
        _pushAnimCts = new CancellationTokenSource();
        var ct = _pushAnimCts.Token;
        var dispatcher = _dispatcher;

        _pushButton.Icon = LucideIcons.Loader;
        _pushButton.Label = "Pushing";
        _pushButton.IconRotation = 0f;

        const int TickMs = 16;
        // Clockwise on screen: negative angle delta (orthographic projection has Y up).
        const float RotationPerTick = -MathF.Tau * (TickMs / 1000f);

        Task.Run(async () =>
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(TickMs, ct).ConfigureAwait(false);
                    dispatcher?.Post(() =>
                    {
                        if (ct.IsCancellationRequested) return;
                        _pushButton.IconRotation += RotationPerTick;
                    });
                }
            }
            catch (OperationCanceledException) { /* expected */ }
        }, ct);
    }

    private void StopPushingAnimation()
    {
        _pushAnimCts?.Cancel();
        _pushAnimCts?.Dispose();
        _pushAnimCts = null;
        _pushButton.Icon = LucideIcons.Push;
        _pushButton.Label = "Push";
        _pushButton.IconRotation = 0f;
    }

    private void ShowError(string? msg) => _errorBar.Message = msg;

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
