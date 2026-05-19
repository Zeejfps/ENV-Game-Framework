using ZGF.Observable;

namespace GitGui;

internal sealed class CreateBranchPresenter : IDisposable
{
    private readonly ICreateBranchView _view;
    private readonly CreateBranchRequest _request;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;

    private bool _isCreating;

    public CreateBranchPresenter(
        ICreateBranchView view,
        CreateBranchRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _view = view;
        _request = request;
        _gitService = gitService;
        _dispatcher = dispatcher;
        _bus = bus;

        _view.NameChanged += OnNameChanged;
        _view.CreateRequested += TryCreate;
        _view.CreateEnabled = false;
        _view.FocusName();
    }

    public void Dispose()
    {
        _view.NameChanged -= OnNameChanged;
        _view.CreateRequested -= TryCreate;
    }

    private void OnNameChanged()
    {
        if (_isCreating) return;
        _view.CreateEnabled = _view.Name.Length > 0;
    }

    private void TryCreate()
    {
        if (_isCreating) return;
        var name = _view.Name;
        if (name.Length == 0) return;

        // Empty start-point text means "branch from current HEAD" — fall back so we don't
        // pass an empty arg to git, which would treat it as a missing positional and error.
        var startPoint = _view.StartPoint;
        if (startPoint.Length == 0) startPoint = "HEAD";

        var checkout = _view.Checkout;

        _isCreating = true;
        _view.CreateEnabled = false;
        _view.ErrorMessage = null;

        var repo = _request.Repo;
        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;
        var view = _view;

        Task.Run(() =>
        {
            CreateBranchOutcome outcome;
            try
            {
                outcome = service.CreateBranch(repo, name, startPoint, checkout);
            }
            catch (Exception ex)
            {
                outcome = new CreateBranchOutcome(false, ex.Message);
            }

            dispatcher.Post(() =>
            {
                _isCreating = false;
                if (!outcome.Success)
                {
                    view.ErrorMessage = outcome.ErrorMessage ?? "Create branch failed.";
                    view.CreateEnabled = view.Name.Length > 0;
                    return;
                }
                view.Close();
                bus.Broadcast(new RefsChangedMessage(repo.Id));
            });
        });
    }
}
