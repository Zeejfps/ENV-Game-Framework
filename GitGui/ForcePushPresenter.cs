using ZGF.Observable;

namespace GitGui;

internal sealed class ForcePushPresenter : IDisposable
{
    private readonly IForcePushView _view;
    private readonly Repo _repo;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly OperationRunner _runner;

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
        _bus = bus;
        _runner = new OperationRunner(dispatcher);

        _view.ForcePushRequested += OnForcePushRequested;
        _view.ForcePushEnabled = true;
    }

    public void Dispose()
    {
        _view.ForcePushRequested -= OnForcePushRequested;
    }

    private void OnForcePushRequested()
    {
        if (_runner.IsRunning) return;

        var repoId = _repo.Id;

        _view.ForcePushEnabled = false;
        _view.ErrorMessage = null;

        _runner.Run(
            () => _gitService.Push(_repo, force: true),
            ex => new PushOutcome(false, ex.Message),
            outcome =>
            {
                if (!outcome.Success)
                {
                    _view.ErrorMessage = outcome.ErrorMessage ?? "Force push failed.";
                    _view.ForcePushEnabled = true;
                    return;
                }
                _view.Close();
                _bus.Broadcast(new RefsChangedMessage(repoId));
            });
    }
}
