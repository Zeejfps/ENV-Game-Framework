using ZGF.Observable;

namespace GitGui;

internal sealed class DiscardChangesPresenter : IDisposable
{
    private readonly IDiscardChangesView _view;
    private readonly DiscardChangesRequest _request;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly OperationRunner _runner;

    public DiscardChangesPresenter(
        IDiscardChangesView view,
        DiscardChangesRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _view = view;
        _request = request;
        _gitService = gitService;
        _bus = bus;
        _runner = new OperationRunner(dispatcher);

        _view.DiscardRequested += OnDiscardRequested;
        _view.DiscardEnabled = request.Paths.Count > 0;
    }

    public void Dispose()
    {
        _view.DiscardRequested -= OnDiscardRequested;
    }

    private void OnDiscardRequested()
    {
        if (_runner.IsRunning) return;
        if (_request.Paths.Count == 0) return;

        var repoId = _request.Repo.Id;

        _view.DiscardEnabled = false;
        _view.ErrorMessage = null;

        _runner.Run(
            () => _gitService.DiscardChanges(_request.Repo, _request.Paths),
            ex => ex.Message,
            errorMsg =>
            {
                if (errorMsg != null)
                {
                    _view.ErrorMessage = errorMsg;
                    _view.DiscardEnabled = true;
                    return;
                }
                _view.Close();
                _bus.Broadcast(new WorkingTreeChangedMessage(repoId));
            });
    }
}
