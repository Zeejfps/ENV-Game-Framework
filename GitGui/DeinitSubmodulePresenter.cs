using ZGF.Observable;

namespace GitGui;

internal sealed class DeinitSubmodulePresenter : IDisposable
{
    private readonly IDeinitSubmoduleView _view;
    private readonly DeinitSubmoduleViewRequest _request;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly OperationRunner _runner;

    public DeinitSubmodulePresenter(
        IDeinitSubmoduleView view,
        DeinitSubmoduleViewRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _view = view;
        _request = request;
        _gitService = gitService;
        _bus = bus;
        _runner = new OperationRunner(dispatcher);

        _view.DeinitRequested += TryDeinit;
        _view.DeinitEnabled = true;
    }

    public void Dispose()
    {
        _view.DeinitRequested -= TryDeinit;
    }

    private void TryDeinit()
    {
        if (_runner.IsRunning) return;

        var primaryId = _request.Primary.Id;
        // `git submodule deinit` matches submodules by the path recorded in .gitmodules,
        // which is relative to the parent root. We store the absolute path on the Repo
        // for navigation, so relativize before handing to git.
        var submodulePath = ToRelative(_request.Primary.Path, _request.Submodule.Path);
        var force = _view.Force;

        _view.DeinitEnabled = false;
        _view.ErrorMessage = null;

        _runner.Run(
            () => _gitService.DeinitSubmodule(_request.Primary, submodulePath, force),
            ex => new SubmoduleDeinitOutcome(false, ex.Message),
            outcome =>
            {
                if (!outcome.Success)
                {
                    _view.ErrorMessage = outcome.ErrorMessage ?? "Deinit submodule failed.";
                    _view.DeinitEnabled = true;
                    return;
                }
                _view.Close();
                _bus.Broadcast(new SubmodulesChangedMessage(primaryId));
                _bus.Broadcast(new WorkingTreeChangedMessage(primaryId));
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
