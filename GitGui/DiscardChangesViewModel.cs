using ZGF.Observable;

namespace GitGui;

internal sealed class DiscardChangesViewModel : IDisposable
{
    public AsyncCommand Discard { get; }
    public event Action? CloseRequested;

    public DiscardChangesViewModel(
        DiscardChangesRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        var hasPaths = new State<bool>(request.Paths.Count > 0);

        Discard = new AsyncCommand(
            dispatcher,
            work: () =>
            {
                gitService.DiscardChanges(request.Repo, request.Paths);
                return null;
            },
            onSuccess: () =>
            {
                bus.Broadcast(new WorkingTreeChangedMessage(request.Repo.Id));
                CloseRequested?.Invoke();
            },
            gate: hasPaths);
    }

    public void Dispose() { }
}
