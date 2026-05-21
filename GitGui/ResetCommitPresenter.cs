using ZGF.Observable;

namespace GitGui;

internal sealed class ResetCommitPresenter : IDisposable
{
    private readonly IResetCommitView _view;
    private readonly ResetCommitRequest _request;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly OperationRunner _runner;

    public ResetCommitPresenter(
        IResetCommitView view,
        ResetCommitRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _view = view;
        _request = request;
        _gitService = gitService;
        _bus = bus;
        _runner = new OperationRunner(dispatcher);

        _view.ResetRequested += OnResetRequested;
    }

    public void Dispose()
    {
        _view.ResetRequested -= OnResetRequested;
    }

    private void OnResetRequested(ResetMode mode)
    {
        if (_runner.IsRunning) return;

        var repoId = _request.Repo.Id;

        _view.ButtonsEnabled = false;
        _view.ErrorMessage = null;

        _runner.Run(
            () => _gitService.ResetCurrent(_request.Repo, _request.Sha, mode),
            ex => new ResetOutcome(false, ex.Message),
            outcome =>
            {
                if (!outcome.Success)
                {
                    _view.ErrorMessage = outcome.ErrorMessage ?? "Reset failed.";
                    _view.ButtonsEnabled = true;
                    return;
                }
                _view.Close();
                _bus.Broadcast(new RefsChangedMessage(repoId));
                _bus.Broadcast(new WorkingTreeChangedMessage(repoId));
            });
    }
}

public readonly record struct ResetCommitRequest(Repo Repo, string Sha);
