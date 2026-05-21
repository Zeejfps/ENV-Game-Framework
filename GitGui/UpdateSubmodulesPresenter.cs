using ZGF.Observable;

namespace GitGui;

internal sealed class UpdateSubmodulesPresenter : IDisposable
{
    private readonly IUpdateSubmodulesView _view;
    private readonly UpdateSubmodulesViewRequest _request;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;

    private bool _isRunning;

    public UpdateSubmodulesPresenter(
        IUpdateSubmodulesView view,
        UpdateSubmodulesViewRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _view = view;
        _request = request;
        _gitService = gitService;
        _dispatcher = dispatcher;
        _bus = bus;

        _view.UpdateRequested += TryUpdate;
        _view.UpdateEnabled = true;
    }

    public void Dispose()
    {
        _view.UpdateRequested -= TryUpdate;
    }

    private void TryUpdate()
    {
        if (_isRunning) return;

        _isRunning = true;
        _view.UpdateEnabled = false;
        _view.ErrorMessage = null;

        var primary = _request.Primary;
        var primaryId = primary.Id;
        var target = _request.TargetSubmodule;
        var init = _view.Init;
        var recursive = _view.Recursive;
        var mode = _view.Mode;
        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;
        var view = _view;

        var req = new SubmoduleUpdateRequest(
            // `git submodule update -- <path>` matches against the .gitmodules path
            // (relative to the parent root); Repo.Path is absolute.
            Paths: target is null ? null : new[] { ToRelative(primary.Path, target.Path) },
            Init: init,
            Recursive: recursive,
            Mode: mode);

        Task.Run(() =>
        {
            SubmoduleUpdateOutcome outcome;
            try { outcome = service.UpdateSubmodules(primary, req); }
            catch (Exception ex) { outcome = new SubmoduleUpdateOutcome(false, ex.Message); }

            dispatcher.Post(() =>
            {
                _isRunning = false;
                if (!outcome.Success)
                {
                    // On a conflict we still close the dialog — the OperationStateBanner
                    // will pick up MERGE_HEAD / rebase-apply and offer the right Abort. The
                    // user has more affordance there than inside the dialog.
                    if (outcome.HasConflicts)
                    {
                        view.Close();
                        bus.Broadcast(new SubmodulesChangedMessage(primaryId));
                        bus.Broadcast(new RefsChangedMessage(primaryId));
                        return;
                    }
                    view.ErrorMessage = outcome.ErrorMessage ?? "Update submodules failed.";
                    view.UpdateEnabled = true;
                    return;
                }
                view.Close();
                bus.Broadcast(new SubmodulesChangedMessage(primaryId));
                bus.Broadcast(new RefsChangedMessage(primaryId));
            });
        });
    }

    private static string ToRelative(string parentRoot, string submoduleAbs)
    {
        try
        {
            var rel = System.IO.Path.GetRelativePath(parentRoot, submoduleAbs);
            return rel.Replace('\\', '/').TrimEnd('/');
        }
        catch
        {
            return submoduleAbs;
        }
    }
}
