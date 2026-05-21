using ZGF.Observable;

namespace GitGui;

internal sealed class CreateBranchPresenter : IDisposable
{
    private readonly ICreateBranchView _view;
    private readonly CreateBranchRequest _request;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly OperationRunner _runner;

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
        _bus = bus;
        _runner = new OperationRunner(dispatcher);

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
        if (_runner.IsRunning) return;
        _view.CreateEnabled = _view.Name.Length > 0;
    }

    private void TryCreate()
    {
        if (_runner.IsRunning) return;
        var name = _view.Name;
        if (name.Length == 0) return;

        // Empty start-point text means "branch from current HEAD" — fall back so we don't
        // pass an empty arg to git, which would treat it as a missing positional and error.
        var startPoint = _view.StartPoint;
        if (startPoint.Length == 0) startPoint = "HEAD";

        var checkout = _view.Checkout;
        var repoId = _request.Repo.Id;

        _view.CreateEnabled = false;
        _view.ErrorMessage = null;

        _runner.Run(
            () => _gitService.CreateBranch(_request.Repo, name, startPoint, checkout),
            ex => new CreateBranchOutcome(false, ex.Message),
            outcome =>
            {
                if (!outcome.Success)
                {
                    _view.ErrorMessage = outcome.ErrorMessage ?? "Create branch failed.";
                    _view.CreateEnabled = _view.Name.Length > 0;
                    return;
                }
                _view.Close();
                _bus.Broadcast(new RefsChangedMessage(repoId));
            });
    }
}
