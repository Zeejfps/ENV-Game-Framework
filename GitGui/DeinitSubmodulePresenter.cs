using ZGF.Observable;

namespace GitGui;

internal sealed class DeinitSubmodulePresenter : IDisposable
{
    private readonly IDeinitSubmoduleView _view;
    private readonly DeinitSubmoduleViewRequest _request;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;

    private bool _isRunning;

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
        _dispatcher = dispatcher;
        _bus = bus;

        _view.DeinitRequested += TryDeinit;
        _view.DeinitEnabled = true;
    }

    public void Dispose()
    {
        _view.DeinitRequested -= TryDeinit;
    }

    private void TryDeinit()
    {
        if (_isRunning) return;

        _isRunning = true;
        _view.DeinitEnabled = false;
        _view.ErrorMessage = null;

        var primary = _request.Primary;
        var primaryId = primary.Id;
        // `git submodule deinit` matches submodules by the path recorded in .gitmodules,
        // which is relative to the parent root. We store the absolute path on the Repo
        // for navigation, so relativize before handing to git.
        var submodulePath = ToRelative(primary.Path, _request.Submodule.Path);
        var force = _view.Force;
        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;
        var view = _view;

        Task.Run(() =>
        {
            SubmoduleDeinitOutcome outcome;
            try { outcome = service.DeinitSubmodule(primary, submodulePath, force); }
            catch (Exception ex) { outcome = new SubmoduleDeinitOutcome(false, ex.Message); }

            dispatcher.Post(() =>
            {
                _isRunning = false;
                if (!outcome.Success)
                {
                    view.ErrorMessage = outcome.ErrorMessage ?? "Deinit submodule failed.";
                    view.DeinitEnabled = true;
                    return;
                }
                view.Close();
                bus.Broadcast(new SubmodulesChangedMessage(primaryId));
                bus.Broadcast(new WorkingTreeChangedMessage(primaryId));
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
