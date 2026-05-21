using ZGF.Observable;

namespace GitGui;

internal sealed class AddSubmodulePresenter : IDisposable
{
    private readonly IAddSubmoduleView _view;
    private readonly AddSubmoduleViewRequest _request;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;

    private bool _isAdding;

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
        _dispatcher = dispatcher;
        _bus = bus;

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
        if (_isAdding) return;
        _view.AddEnabled = _view.Url.Trim().Length > 0 && _view.Path.Trim().Length > 0;
    }

    private void TryAdd()
    {
        if (_isAdding) return;

        var url = _view.Url.Trim();
        var path = _view.Path.Trim();
        if (url.Length == 0 || path.Length == 0) return;

        var branch = _view.Branch.Trim();
        var force = _view.Force;

        _isAdding = true;
        _view.AddEnabled = false;
        _view.ErrorMessage = null;

        var primary = _request.Primary;
        var primaryId = primary.Id;
        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;
        var view = _view;

        var req = new SubmoduleAddRequest(
            Url: url,
            Path: path,
            Branch: branch.Length > 0 ? branch : null,
            Force: force);

        Task.Run(() =>
        {
            SubmoduleAddOutcome outcome;
            try { outcome = service.AddSubmodule(primary, req); }
            catch (Exception ex) { outcome = new SubmoduleAddOutcome(false, ex.Message); }

            dispatcher.Post(() =>
            {
                _isAdding = false;
                if (!outcome.Success)
                {
                    view.ErrorMessage = outcome.ErrorMessage ?? "Add submodule failed.";
                    view.AddEnabled = view.Url.Trim().Length > 0 && view.Path.Trim().Length > 0;
                    return;
                }
                view.Close();
                // Sync up quickly without waiting for FSW debounce. WorkingTreeChanged
                // also fires so the LocalChanges panel notices the new .gitmodules entry.
                bus.Broadcast(new SubmodulesChangedMessage(primaryId));
                bus.Broadcast(new WorkingTreeChangedMessage(primaryId));
            });
        });
    }
}
