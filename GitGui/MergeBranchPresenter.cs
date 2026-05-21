using ZGF.Observable;

namespace GitGui;

internal sealed class MergeBranchPresenter : IDisposable
{
    private readonly IMergeBranchView _view;
    private readonly MergeBranchRequest _request;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;
    private readonly OperationRunner _runner;

    public MergeBranchPresenter(
        IMergeBranchView view,
        MergeBranchRequest request,
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

        _view.MergeRequested += TryMerge;
        _view.MergeEnabled = true;
        _view.PreviewState = MergePreviewState.Unknown;

        StartPreview();
    }

    public void Dispose()
    {
        _view.MergeRequested -= TryMerge;
    }

    private void StartPreview()
    {
        var repo = _request.Repo;
        var sourceRef = _request.SourceRef;
        var service = _gitService;
        var dispatcher = _dispatcher;
        var view = _view;

        Task.Run(() =>
        {
            MergePreviewResult result;
            try { result = service.PreviewMerge(repo, sourceRef); }
            catch (Exception ex) { result = new MergePreviewResult(MergePreviewState.Unknown, ex.Message); }

            dispatcher.Post(() => view.PreviewState = result.State);
        });
    }

    private void TryMerge()
    {
        if (_runner.IsRunning) return;

        var strategy = _view.Strategy;
        var repoId = _request.Repo.Id;

        _view.MergeEnabled = false;
        _view.ErrorMessage = null;

        _runner.Run(
            () => _gitService.Merge(_request.Repo, _request.SourceRef, strategy),
            ex => new MergeOutcome(false, ex.Message),
            outcome =>
            {
                if (!outcome.Success)
                {
                    _view.ErrorMessage = outcome.ErrorMessage ?? "Merge failed.";
                    _view.MergeEnabled = true;
                    return;
                }
                _view.Close();
                _bus.Broadcast(new RefsChangedMessage(repoId));
                _bus.Broadcast(new WorkingTreeChangedMessage(repoId));
            });
    }
}
