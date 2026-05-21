using ZGF.Observable;

namespace GitGui;

internal sealed class RebaseBranchPresenter : IDisposable
{
    private readonly IRebaseBranchView _view;
    private readonly RebaseBranchRequest _request;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;
    private readonly OperationRunner _runner;

    public RebaseBranchPresenter(
        IRebaseBranchView view,
        RebaseBranchRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _view = view;
        _request = request;
        _gitService = gitService;
        _dispatcher = dispatcher;
        _bus = bus;
        _runner = new OperationRunner(dispatcher);

        _view.RebaseRequested += TryRebase;
        _view.RebaseEnabled = true;
        _view.PreviewState = RebasePreviewState.Unknown;

        StartPreview();
    }

    public void Dispose()
    {
        _view.RebaseRequested -= TryRebase;
    }

    private void StartPreview()
    {
        var repo = _request.Repo;
        var targetRef = _request.TargetRef;
        var service = _gitService;
        var dispatcher = _dispatcher;
        var view = _view;

        Task.Run(() =>
        {
            RebasePreviewResult result;
            try { result = service.PreviewRebase(repo, targetRef); }
            catch (Exception ex) { result = new RebasePreviewResult(RebasePreviewState.Unknown, ex.Message); }

            dispatcher.Post(() => view.PreviewState = result.State);
        });
    }

    private void TryRebase()
    {
        if (_runner.IsRunning) return;

        var autostash = _view.Autostash;
        var repoId = _request.Repo.Id;

        _view.RebaseEnabled = false;
        _view.ErrorMessage = null;

        _runner.Run(
            () => _gitService.Rebase(_request.Repo, _request.TargetRef, autostash),
            ex => new RebaseOutcome(false, ex.Message),
            outcome =>
            {
                if (!outcome.Success)
                {
                    _view.ErrorMessage = outcome.ErrorMessage ?? "Rebase failed.";
                    _view.RebaseEnabled = true;
                    return;
                }
                _view.Close();
                _bus.Broadcast(new RefsChangedMessage(repoId));
                _bus.Broadcast(new WorkingTreeChangedMessage(repoId));
            });
    }
}
