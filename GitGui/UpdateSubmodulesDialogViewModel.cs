using ZGF.Observable;

namespace GitGui;

internal sealed class UpdateSubmodulesDialogViewModel : IDisposable
{
    private readonly UpdateSubmodulesViewRequest _request;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly OperationRunner _runner;
    private readonly State<bool> _isRunning = new(false);
    private readonly State<string?> _error = new(null);

    public State<bool> Init { get; } = new(true);
    public State<bool> Recursive { get; } = new(false);
    public State<SubmoduleUpdateMode> Mode { get; } = new(SubmoduleUpdateMode.Checkout);

    public Command Update { get; }
    public IReadable<string?> Error => _error;

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
        _error.Value = null;

        var req = new SubmoduleUpdateRequest(
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
                    if (outcome.HasConflicts)
                    {
                        CloseRequested?.Invoke();
                        _bus.Broadcast(new SubmodulesChangedMessage(primaryId));
                        _bus.Broadcast(new RefsChangedMessage(primaryId));
                        return;
                    }
                    _error.Value = outcome.ErrorMessage ?? "Update submodules failed.";
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

internal readonly record struct UpdateSubmodulesViewRequest(Repo Primary, Repo? TargetSubmodule);
