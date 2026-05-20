using ZGF.Observable;

namespace GitGui;

internal sealed class MergeBranchPresenter : IDisposable
{
    private readonly IMergeBranchView _view;
    private readonly MergeBranchRequest _request;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;

    private bool _isMerging;

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
        if (_isMerging) return;

        _isMerging = true;
        _view.MergeEnabled = false;
        _view.ErrorMessage = null;

        var repo = _request.Repo;
        var sourceRef = _request.SourceRef;
        var strategy = _view.Strategy;
        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;
        var view = _view;

        Task.Run(() =>
        {
            MergeOutcome outcome;
            try { outcome = service.Merge(repo, sourceRef, strategy); }
            catch (Exception ex) { outcome = new MergeOutcome(false, ex.Message); }

            dispatcher.Post(() =>
            {
                _isMerging = false;
                if (!outcome.Success)
                {
                    view.ErrorMessage = outcome.ErrorMessage ?? "Merge failed.";
                    view.MergeEnabled = true;
                    return;
                }
                view.Close();
                bus.Broadcast(new RefsChangedMessage(repo.Id));
                bus.Broadcast(new WorkingTreeChangedMessage(repo.Id));
            });
        });
    }
}
