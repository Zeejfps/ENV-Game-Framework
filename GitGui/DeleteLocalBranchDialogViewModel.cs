using ZGF.Gui;
using ZGF.Observable;

namespace GitGui;

internal sealed class DeleteLocalBranchDialogViewModel : IDisposable
{
    private readonly DeleteLocalBranchRequest _request;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly SpinnerAnimation _spinner;
    private readonly State<DeleteRemoteBranchOutcome?> _partialFailure = new(null);

    public State<bool> Force { get; } = new(false);
    public State<bool> DeleteRemote { get; } = new(false);
    public bool HasUpstream { get; }

    public AsyncCommand Delete { get; }
    public IReadable<bool> CancelEnabled { get; }
    public IReadable<bool> IsBusy { get; }
    public IReadable<float> BusyRotation { get; }

    public event Action? CloseRequested;

    public DeleteLocalBranchDialogViewModel(
        DeleteLocalBranchRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _request = request;
        _gitService = gitService;
        _bus = bus;
        _spinner = new SpinnerAnimation(dispatcher);

        HasUpstream = !string.IsNullOrEmpty(request.UpstreamRemote)
                      && !string.IsNullOrEmpty(request.UpstreamBranch);

        BusyRotation = _spinner.Rotation;

        Delete = new AsyncCommand(dispatcher, DoDelete, OnDeleteSucceeded);

        // The spinner reflects the AsyncCommand's running state, plus we drive Start/Stop
        // so the rotation actually animates rather than just toggling the icon.
        IsBusy = Delete.IsRunning;
        CancelEnabled = new Derived<bool>(() => !Delete.IsRunning.Value);

        Delete.IsRunning.Subscribe(b =>
        {
            if (b) _spinner.Start();
            else _spinner.Stop();
        });
    }

    private string? DoDelete()
    {
        var force = Force.Value;
        var deleteRemote = DeleteRemote.Value && HasUpstream;
        var remoteName = _request.UpstreamRemote;
        var remoteBranch = _request.UpstreamBranch;

        var local = _gitService.DeleteBranch(_request.Repo, _request.BranchName, force);
        if (!local.Success)
            return local.ErrorMessage ?? "Delete failed.";

        if (deleteRemote)
        {
            DeleteRemoteBranchOutcome remote;
            try { remote = _gitService.DeleteRemoteBranch(_request.Repo, remoteName!, remoteBranch!); }
            catch (Exception ex) { remote = new DeleteRemoteBranchOutcome(false, ex.Message); }
            // Stash the partial-failure outcome so the success handler can fire the
            // partial-failure broadcast after the close — the local delete already
            // succeeded so we still close + refresh refs.
            if (!remote.Success)
                _partialFailure.Value = remote;
        }
        return null;
    }

    private void OnDeleteSucceeded()
    {
        _bus.Broadcast(new RefsChangedMessage(_request.Repo.Id));
        CloseRequested?.Invoke();

        if (_partialFailure.Value is { } failed)
        {
            var remoteName = _request.UpstreamRemote;
            var remoteBranch = _request.UpstreamBranch;
            _bus.Broadcast(new ShowOperationErrorMessage(
                "Remote delete failed",
                failed.ErrorMessage
                    ?? $"Local branch deleted, but failed to delete '{remoteBranch}' on '{remoteName}'."));
        }
    }

    public void Dispose()
    {
        _spinner.Dispose();
    }
}

internal readonly record struct DeleteLocalBranchRequest(
    Repo Repo,
    string BranchName,
    string? UpstreamRemote = null,
    string? UpstreamBranch = null);
