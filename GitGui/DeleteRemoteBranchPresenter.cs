using ZGF.Observable;

namespace GitGui;

internal sealed class DeleteRemoteBranchPresenter : IDisposable
{
    private readonly IDeleteRemoteBranchView _view;
    private readonly DeleteRemoteBranchRequest _request;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;

    private bool _isRunning;

    public DeleteRemoteBranchPresenter(
        IDeleteRemoteBranchView view,
        DeleteRemoteBranchRequest request,
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

        var request = _request;
        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;
        var view = _view;

        Task.Run(() =>
        {
            DeleteRemoteBranchOutcome outcome;
            try
            {
                outcome = service.DeleteRemoteBranch(request.Repo, request.RemoteName, request.BranchName);
            }
            catch (Exception ex)
            {
                outcome = new DeleteRemoteBranchOutcome(false, ex.Message);
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
                bus.Broadcast(new RefsChangedMessage(request.Repo.Id));
            });
        });
    }
}
