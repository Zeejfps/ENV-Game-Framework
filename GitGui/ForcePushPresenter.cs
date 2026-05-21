using ZGF.Observable;

namespace GitGui;

internal sealed class ForcePushPresenter : IDisposable
{
    private readonly IForcePushView _view;
    private readonly Repo _repo;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;

    private bool _isRunning;

    public ForcePushPresenter(
        IForcePushView view,
        Repo repo,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _view = view;
        _repo = repo;
        _gitService = gitService;
        _dispatcher = dispatcher;
        _bus = bus;

        _view.ForcePushRequested += OnForcePushRequested;
        _view.ForcePushEnabled = true;
    }

    public void Dispose()
    {
        _view.ForcePushRequested -= OnForcePushRequested;
    }

    private void OnForcePushRequested()
    {
        if (_isRunning) return;
        _isRunning = true;
        _view.ForcePushEnabled = false;
        _view.ErrorMessage = null;

        var repo = _repo;
        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;
        var view = _view;

        Task.Run(() =>
        {
            PushOutcome outcome;
            try { outcome = service.Push(repo, force: true); }
            catch (Exception ex) { outcome = new PushOutcome(false, ex.Message); }

            dispatcher.Post(() =>
            {
                _isRunning = false;
                if (!outcome.Success)
                {
                    view.ErrorMessage = outcome.ErrorMessage ?? "Force push failed.";
                    view.ForcePushEnabled = true;
                    return;
                }
                view.Close();
                bus.Broadcast(new RefsChangedMessage(repo.Id));
            });
        });
    }
}
