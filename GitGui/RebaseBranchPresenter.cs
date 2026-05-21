using ZGF.Observable;

namespace GitGui;

internal sealed class RebaseBranchPresenter : IDisposable
{
    private readonly IRebaseBranchView _view;
    private readonly RebaseBranchRequest _request;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;

    private bool _isRebasing;

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
        if (_isRebasing) return;

        _isRebasing = true;
        _view.RebaseEnabled = false;
        _view.ErrorMessage = null;

        var repo = _request.Repo;
        var targetRef = _request.TargetRef;
        var autostash = _view.Autostash;
        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;
        var view = _view;

        Task.Run(() =>
        {
            RebaseOutcome outcome;
            try { outcome = service.Rebase(repo, targetRef, autostash); }
            catch (Exception ex) { outcome = new RebaseOutcome(false, ex.Message); }

            dispatcher.Post(() =>
            {
                _isRebasing = false;
                if (!outcome.Success)
                {
                    view.ErrorMessage = outcome.ErrorMessage ?? "Rebase failed.";
                    view.RebaseEnabled = true;
                    return;
                }
                view.Close();
                bus.Broadcast(new RefsChangedMessage(repo.Id));
                bus.Broadcast(new WorkingTreeChangedMessage(repo.Id));
            });
        });
    }
}
