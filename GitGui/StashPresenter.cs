using ZGF.Observable;

namespace GitGui;

internal sealed class StashPresenter : IDisposable
{
    private readonly IStashView _view;
    private readonly StashRequest _request;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;

    private bool _isStashing;

    public StashPresenter(
        IStashView view,
        StashRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _view = view;
        _request = request;
        _gitService = gitService;
        _dispatcher = dispatcher;
        _bus = bus;

        _view.MessageChanged += OnMessageChanged;
        _view.StashRequested += TryStash;
        _view.StashEnabled = false;
        _view.FocusMessage();
    }

    public void Dispose()
    {
        _view.MessageChanged -= OnMessageChanged;
        _view.StashRequested -= TryStash;
    }

    private void OnMessageChanged()
    {
        if (_isStashing) return;
        _view.StashEnabled = _view.Message.Length > 0;
    }

    private void TryStash()
    {
        if (_isStashing) return;
        var message = _view.Message;
        if (message.Length == 0) return;

        var includeUntracked = _view.IncludeUntracked;
        var keepIndex = _view.KeepStaged;

        _isStashing = true;
        _view.StashEnabled = false;
        _view.ErrorMessage = null;

        var repo = _request.Repo;
        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;
        var view = _view;

        Task.Run(() =>
        {
            StashOutcome outcome;
            try
            {
                outcome = service.CreateStash(repo, message, includeUntracked, keepIndex);
            }
            catch (Exception ex)
            {
                outcome = new StashOutcome(false, ex.Message);
            }

            dispatcher.Post(() =>
            {
                _isStashing = false;
                if (!outcome.Success)
                {
                    view.ErrorMessage = outcome.ErrorMessage ?? "Stash failed.";
                    view.StashEnabled = view.Message.Length > 0;
                    return;
                }
                view.Close();
                bus.Broadcast(new RefsChangedMessage(repo.Id));
                bus.Broadcast(new WorkingTreeChangedMessage(repo.Id));
            });
        });
    }
}
