using ZGF.Observable;

namespace GitGui;

internal sealed class DeleteLocalBranchPresenter : IDisposable
{
    private readonly IDeleteLocalBranchView _view;
    private readonly DeleteLocalBranchRequest _request;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;

    private bool _isRunning;

    public DeleteLocalBranchPresenter(
        IDeleteLocalBranchView view,
        DeleteLocalBranchRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _view = view;
        _request = request;
        _gitService = gitService;
        _dispatcher = dispatcher;
        _bus = bus;

        _view.DeleteRequested += OnDeleteRequested;
        _view.DeleteEnabled = true;
    }

    public void Dispose()
    {
        _view.DeleteRequested -= OnDeleteRequested;
    }

    private void OnDeleteRequested()
    {
        if (_isRunning) return;

        _isRunning = true;
        _view.DeleteEnabled = false;
        _view.ErrorMessage = null;

        var force = _view.Force;
        var request = _request;
        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;
        var view = _view;

        Task.Run(() =>
        {
            DeleteBranchOutcome outcome;
            try
            {
                outcome = service.DeleteBranch(request.Repo, request.BranchName, force);
            }
            catch (Exception ex)
            {
                outcome = new DeleteBranchOutcome(false, ex.Message);
            }

            dispatcher.Post(() =>
            {
                _isRunning = false;
                if (!outcome.Success)
                {
                    view.ErrorMessage = outcome.ErrorMessage ?? "Delete failed.";
                    view.DeleteEnabled = true;
                    return;
                }
                view.Close();
                // BranchesViewModel checks against the fresh listing on RefsChangedMessage
                // and drops any selection pointing at a name that no longer exists, so we
                // don't need a separate "branch deleted" signal.
                bus.Broadcast(new RefsChangedMessage(request.Repo.Id));
            });
        });
    }
}
