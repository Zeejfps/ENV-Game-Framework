using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public sealed class BranchesHeader : MultiChildView
{
    private const float HeaderHeight = 44f;
    private const int HorizontalPadding = 8;

    private readonly CurrentBranchChip _chip;
    private readonly FlexRowView _row;

    public BranchesHeader()
    {
        PreferredHeight = HeaderHeight;

        _chip = new CurrentBranchChip();
        _row = new FlexRowView
        {
            CrossAxisAlignment = CrossAxisAlignment.Center,
        };

        AddChildToSelf(new RectView
        {
            BackgroundColor = DialogPalette.Background,
            BorderColor = new BorderColorStyle { Bottom = DialogPalette.Border },
            BorderSize = new BorderSizeStyle { Bottom = 1 },
            Padding = new PaddingStyle { Left = HorizontalPadding, Right = HorizontalPadding },
            Children = { _row },
        });

        this.UsePresenter(ctx => new BranchesHeaderPresenter(
            this,
            ctx.Require<IRepoRegistry>(),
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    internal void SetBranch(string? name, bool isDetached)
    {
        var attached = _row.Children.Contains(_chip);
        if (string.IsNullOrEmpty(name))
        {
            if (attached) _row.Children.Remove(_chip);
            return;
        }
        _chip.BranchName = name;
        _chip.IsDetached = isDetached;
        if (!attached) _row.Children.Add(_chip);
    }
}

internal sealed class BranchesHeaderPresenter : IDisposable
{
    private readonly BranchesHeader _view;
    private readonly IRepoRegistry _registry;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;
    private readonly SubscriptionGroup _subscriptions = new();
    private readonly GenerationGuard _gen = new();

    public BranchesHeaderPresenter(
        BranchesHeader view,
        IRepoRegistry registry,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _view = view;
        _registry = registry;
        _gitService = gitService;
        _dispatcher = dispatcher;
        _bus = bus;

        _subscriptions.Add(_registry.Active.Subscribe(_ => Reload()));
        _subscriptions.Add(_bus.SubscribeScoped<RefsChangedMessage>(_ => Reload()));
        _subscriptions.Add(_bus.SubscribeScoped<CommitCreatedMessage>(_ => Reload()));
    }

    public void Dispose()
    {
        _gen.Bump();
        _subscriptions.Dispose();
    }

    private void Reload()
    {
        var repo = _registry.Active.Value;
        if (repo == null)
        {
            _gen.Bump();
            _view.SetBranch(null, false);
            return;
        }

        var gen = _gen.Bump();
        var service = _gitService;
        var dispatcher = _dispatcher;
        var repoId = repo.Id;

        Task.Run(() =>
        {
            PushStatus status;
            try { status = service.GetPushStatus(repo); }
            catch { status = new PushStatus(null, HasUpstream: false, Ahead: 0, Behind: 0, IsDetached: false); }

            dispatcher.Post(() =>
            {
                if (_gen.IsStale(gen)) return;
                if (_registry.Active.Value?.Id != repoId) return;
                if (status.IsDetached)
                    _view.SetBranch("(detached HEAD)", true);
                else
                    _view.SetBranch(status.CurrentBranchName, false);
            });
        });
    }
}
