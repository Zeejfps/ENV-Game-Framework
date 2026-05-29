using ZGF.Observable;

namespace GitGui;

internal sealed class UpdateSubmodulesDialogViewModel : IDisposable
{
    private readonly UpdateSubmodulesViewRequest _request;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly OperationRunner _runner;
    private readonly State<bool> _isRunning = new(false);

    public State<bool> Init { get; } = new(true);
    public State<bool> Recursive { get; } = new(false);
    public State<SubmoduleUpdateMode> Mode { get; } = new(SubmoduleUpdateMode.Checkout);

    public Command Update { get; }

    public event Action? CloseRequested;

    public UpdateSubmodulesDialogViewModel(
        UpdateSubmodulesViewRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _request = request;
        _gitService = gitService;
        _bus = bus;
        _runner = new OperationRunner(dispatcher);

        var canUpdate = new Derived<bool>(() => !_isRunning.Value);
        Update = new Command(DoUpdate, canUpdate);
    }

    private void DoUpdate()
    {
        if (_isRunning.Value) return;

        var primaryId = _request.Primary.Id;
        var target = _request.TargetSubmodule;
        var init = Init.Value;
        var recursive = Recursive.Value;
        var mode = Mode.Value;
        _isRunning.Value = true;

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
                _isRunning.Value = false;
                if (!outcome.Success)
                {
                    // On a conflict we still close the dialog — the OperationStateBanner
                    // will pick up MERGE_HEAD / rebase-apply and offer the right Abort. The
                    // user has more affordance there than inside the dialog. We DO NOT
                    // broadcast refs-changed here because the original presenter does
                    // broadcast them — preserve that exactly.
                    if (outcome.HasConflicts)
                    {
                        CloseRequested?.Invoke();
                        _bus.Broadcast(new SubmodulesChangedMessage(primaryId));
                        _bus.Broadcast(new RefsChangedMessage(primaryId));
                        return;
                    }
                    CloseRequested?.Invoke();
                    _bus.Broadcast(new ShowOperationErrorMessage(
                        "Update submodules failed",
                        outcome.ErrorMessage ?? "Update submodules failed."));
                    return;
                }
                CloseRequested?.Invoke();
                _bus.Broadcast(new SubmodulesChangedMessage(primaryId));
                _bus.Broadcast(new RefsChangedMessage(primaryId));
            });
    }

    public void Dispose() { }

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

// TargetSubmodule == null means "update every submodule under the parent".
internal readonly record struct UpdateSubmodulesViewRequest(Repo Primary, Repo? TargetSubmodule);
