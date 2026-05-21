using ZGF.Observable;

namespace GitGui;

internal sealed class ResetCommitPresenter : IDisposable
{
    private readonly IResetCommitView _view;
    private readonly ResetCommitRequest _request;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;

    private bool _isRunning;

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
        _dispatcher = dispatcher;
        _bus = bus;

        _view.ResetRequested += OnResetRequested;
    }

    public void Dispose()
    {
        _view.ResetRequested -= OnResetRequested;
    }

    private void OnResetRequested(ResetMode mode)
    {
        if (_isRunning) return;

        _isRunning = true;
        _view.ButtonsEnabled = false;
        _view.ErrorMessage = null;

        var request = _request;
        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;
        var view = _view;

        Task.Run(() =>
        {
            ResetOutcome outcome;
            try { outcome = service.ResetCurrent(request.Repo, request.Sha, mode); }
            catch (Exception ex) { outcome = new ResetOutcome(false, ex.Message); }

            dispatcher.Post(() =>
            {
                _isRunning = false;
                if (!outcome.Success)
                {
                    view.ErrorMessage = outcome.ErrorMessage ?? "Reset failed.";
                    view.ButtonsEnabled = true;
                    return;
                }
                view.Close();
                bus.Broadcast(new RefsChangedMessage(request.Repo.Id));
                bus.Broadcast(new WorkingTreeChangedMessage(request.Repo.Id));
            });
        });
    }
}

public readonly record struct ResetCommitRequest(Repo Repo, string Sha);
