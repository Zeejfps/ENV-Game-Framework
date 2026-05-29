using ZGF.Gui;
using ZGF.Observable;

namespace GitGui;

internal sealed class AbortOperationDialogViewModel : IDisposable
{
    private readonly AbortOperationRequest _request;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly OperationRunner _runner;
    private readonly SpinnerAnimation _spinner;
    private readonly State<bool> _isRunning = new(false);
    private readonly State<string?> _error = new(null);
    private readonly State<bool> _forceQuitMode = new(false);

    public IReadable<string> ConfirmButtonLabel { get; }
    public IReadable<bool> CancelEnabled { get; }
    public IReadable<bool> IsBusy { get; }
    public IReadable<float> BusyRotation { get; }
    public IReadable<string?> Error => _error;

    public Command Abort { get; }

    public event Action? CloseRequested;

    public AbortOperationDialogViewModel(
        AbortOperationRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _request = request;
        _gitService = gitService;
        _bus = bus;
        _runner = new OperationRunner(dispatcher);
        _spinner = new SpinnerAnimation(dispatcher);

        IsBusy = _spinner.IsActive;
        BusyRotation = _spinner.Rotation;
        CancelEnabled = new Derived<bool>(() => !_isRunning.Value);

        var defaultLabel = DefaultConfirmLabel(request.State);
        ConfirmButtonLabel = new Derived<string>(() => _forceQuitMode.Value ? "Force clear" : defaultLabel);

        var canAbort = new Derived<bool>(() =>
            !_isRunning.Value && request.State != RepoOperationState.None);
        Abort = new Command(DoAbort, canAbort);
    }

    private void DoAbort()
    {
        if (_isRunning.Value) return;
        if (_request.State == RepoOperationState.None) return;

        var forceQuit = _forceQuitMode.Value;
        var repoId = _request.Repo.Id;
        _isRunning.Value = true;
        _error.Value = null;
        _spinner.Start();

        _runner.Run(
            () => _gitService.AbortOperation(_request.Repo, _request.State, forceQuit),
            ex => new AbortOperationOutcome(false, ex.Message),
            outcome =>
            {
                _spinner.Stop();
                _isRunning.Value = false;
                if (!outcome.Success)
                {
                    _error.Value = outcome.ErrorMessage ?? "Abort failed.";
                    if (outcome.ForceQuitAvailable && !_forceQuitMode.Value)
                        _forceQuitMode.Value = true;
                    return;
                }
                CloseRequested?.Invoke();
                _bus.Broadcast(new RefsChangedMessage(repoId));
                _bus.Broadcast(new WorkingTreeChangedMessage(repoId));
            });
    }

    public void Dispose()
    {
        _spinner.Dispose();
    }

    private static string DefaultConfirmLabel(RepoOperationState state) => state switch
    {
        RepoOperationState.Merge => "Abort merge",
        RepoOperationState.Rebase => "Abort rebase",
        RepoOperationState.CherryPick => "Abort cherry-pick",
        RepoOperationState.Revert => "Abort revert",
        RepoOperationState.ApplyMailbox => "Abort apply",
        RepoOperationState.Bisect => "Reset bisect",
        RepoOperationState.UnmergedPaths => "Reset",
        _ => "Abort",
    };
}

internal readonly record struct AbortOperationRequest(Repo Repo, RepoOperationState State);
