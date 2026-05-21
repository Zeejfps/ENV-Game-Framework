using ZGF.Observable;

namespace GitGui;

internal sealed class AddSubmodulePresenter : IDisposable
{
    private readonly IAddSubmoduleView _view;
    private readonly AddSubmoduleViewRequest _request;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly OperationRunner _runner;

    public AddSubmodulePresenter(
        IAddSubmoduleView view,
        AddSubmoduleViewRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _view = view;
        _request = request;
        _gitService = gitService;
        _bus = bus;
        _runner = new OperationRunner(dispatcher);

        _view.InputsChanged += OnInputsChanged;
        _view.AddRequested += TryAdd;
        _view.AddEnabled = false;
        _view.FocusUrl();
    }

    public void Dispose()
    {
        _view.InputsChanged -= OnInputsChanged;
        _view.AddRequested -= TryAdd;
    }

    private void OnInputsChanged()
    {
        if (_runner.IsRunning) return;
        _view.AddEnabled = _view.Url.Trim().Length > 0 && _view.Path.Trim().Length > 0;
    }

    private void TryAdd()
    {
        if (_runner.IsRunning) return;

        var url = _view.Url.Trim();
        var path = _view.Path.Trim();
        if (url.Length == 0 || path.Length == 0) return;

        var branch = _view.Branch.Trim();
        var force = _view.Force;
        var primaryId = _request.Primary.Id;

        _view.AddEnabled = false;
        _view.ErrorMessage = null;

        var req = new SubmoduleAddRequest(
            Url: url,
            Path: path,
            Branch: branch.Length > 0 ? branch : null,
            Force: force);

        _runner.Run(
            () => _gitService.AddSubmodule(_request.Primary, req),
            ex => new SubmoduleAddOutcome(false, ex.Message),
            outcome =>
            {
                if (!outcome.Success)
                {
                    _view.ErrorMessage = outcome.ErrorMessage ?? "Add submodule failed.";
                    _view.AddEnabled = _view.Url.Trim().Length > 0 && _view.Path.Trim().Length > 0;
                    return;
                }
                _view.Close();
                // Sync up quickly without waiting for FSW debounce. WorkingTreeChanged
                // also fires so the LocalChanges panel notices the new .gitmodules entry.
                _bus.Broadcast(new SubmodulesChangedMessage(primaryId));
                _bus.Broadcast(new WorkingTreeChangedMessage(primaryId));
            });
    }
}
