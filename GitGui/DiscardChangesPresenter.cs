using ZGF.Observable;

namespace GitGui;

internal sealed class DiscardChangesPresenter : IDisposable
{
    private readonly IDiscardChangesView _view;
    private readonly DiscardChangesRequest _request;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;

    private bool _isRunning;

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
        _dispatcher = dispatcher;
        _bus = bus;

        _view.DiscardRequested += OnDiscardRequested;
        _view.DiscardEnabled = request.Paths.Count > 0;
    }

    public void Dispose()
    {
        _view.DiscardRequested -= OnDiscardRequested;
    }

    private void OnDiscardRequested()
    {
        if (_isRunning) return;
        if (_request.Paths.Count == 0) return;

        _isRunning = true;
        _view.DiscardEnabled = false;
        _view.ErrorMessage = null;

        var request = _request;
        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;
        var view = _view;

        Task.Run(() =>
        {
            string? errorMsg = null;
            try
            {
                errorMsg = service.DiscardChanges(request.Repo, request.Paths);
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
            }

            dispatcher.Post(() =>
            {
                _isRunning = false;
                if (errorMsg != null)
                {
                    view.ErrorMessage = errorMsg;
                    view.DiscardEnabled = true;
                    return;
                }
                view.Close();
                bus.Broadcast(new RefsChangedMessage(request.Repo.Id));
            });
        });
    }
}
