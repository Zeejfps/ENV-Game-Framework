using ZGF.Observable;

namespace GitGui;

internal sealed class UpdateSubmodulesPresenter : IDisposable
{
    private readonly IUpdateSubmodulesView _view;
    private readonly UpdateSubmodulesViewRequest _request;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly OperationRunner _runner;

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
        _bus = bus;
        _runner = new OperationRunner(dispatcher);

        _view.UpdateRequested += TryUpdate;
        _view.UpdateEnabled = true;
    }

    public void Dispose()
    {
        _view.UpdateRequested -= TryUpdate;
    }

    private void TryUpdate()
    {
        if (_runner.IsRunning) return;

        var primaryId = _request.Primary.Id;
        var target = _request.TargetSubmodule;
        var init = _view.Init;
        var recursive = _view.Recursive;
        var mode = _view.Mode;

        _view.UpdateEnabled = false;
        _view.ErrorMessage = null;

        var req = new SubmoduleUpdateRequest(
            // `git submodule update -- <path>` matches against the .gitmodules path
            // (relative to the parent root); Repo.Path is absolute.
            Paths: target is null ? null : new[] { ToRelative(_request.Primary.Path, target.Path) },
            Init: init,
            Recursive: recursive,
            Mode: mode);

        _runner.Run(
            () => _gitService.UpdateSubmodules(_request.Primary, req),
            ex => new SubmoduleUpdateOutcome(false, ex.Message),
            outcome =>
            {
                if (!outcome.Success)
                {
                    // On a conflict we still close the dialog — the OperationStateBanner
                    // will pick up MERGE_HEAD / rebase-apply and offer the right Abort. The
                    // user has more affordance there than inside the dialog.
                    if (outcome.HasConflicts)
                    {
                        _view.Close();
                        _bus.Broadcast(new SubmodulesChangedMessage(primaryId));
                        _bus.Broadcast(new RefsChangedMessage(primaryId));
                        return;
                    }
                    _view.ErrorMessage = outcome.ErrorMessage ?? "Update submodules failed.";
                    _view.UpdateEnabled = true;
                    return;
                }
                _view.Close();
                _bus.Broadcast(new SubmodulesChangedMessage(primaryId));
                _bus.Broadcast(new RefsChangedMessage(primaryId));
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
