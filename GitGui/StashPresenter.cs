using ZGF.Observable;

namespace GitGui;

internal sealed class StashPresenter : IDisposable
{
    private readonly IStashView _view;
    private readonly StashRequest _request;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly OperationRunner _runner;

    public StashPresenter(
        IStashView view,
        StashRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _view = view;
        _request = request;
        _gitService = gitService;
        _bus = bus;
        _runner = new OperationRunner(dispatcher);

        _view.MessageChanged += OnMessageChanged;
        _view.StashRequested += TryStash;
        _view.StashEnabled = false;
        _view.FocusMessage();
    }

    public void Dispose()
    {
        _view.MessageChanged -= OnMessageChanged;
        _view.StashRequested -= TryStash;
    }

    private void OnMessageChanged()
    {
        if (_runner.IsRunning) return;
        _view.StashEnabled = _view.Message.Length > 0;
    }

    private void TryStash()
    {
        if (_runner.IsRunning) return;
        var message = _view.Message;
        if (message.Length == 0) return;

        var includeUntracked = _view.IncludeUntracked;
        var keepIndex = _view.KeepStaged;
        var repoId = _request.Repo.Id;

        _view.StashEnabled = false;
        _view.ErrorMessage = null;

        _runner.Run(
            () => _gitService.CreateStash(_request.Repo, message, includeUntracked, keepIndex),
            ex => new StashOutcome(false, ex.Message),
            outcome =>
            {
                if (!outcome.Success)
                {
                    _view.ErrorMessage = outcome.ErrorMessage ?? "Stash failed.";
                    _view.StashEnabled = _view.Message.Length > 0;
                    return;
                }
                _view.Close();
                _bus.Broadcast(new RefsChangedMessage(repoId));
                _bus.Broadcast(new WorkingTreeChangedMessage(repoId));
            });
    }
}
