using ZGF.Gui;
using ZGF.Observable;

namespace GitGui;

public record DiffTarget(string Path, DiffSide Side, string? CommitSha = null);

public abstract record DiffRenderState
{
    public sealed record Placeholder(string Text) : DiffRenderState;
    public sealed record Loaded(DiffResult Result) : DiffRenderState;
}

public sealed class DiffViewModel : IDisposable
{
    private const string EmptyPlaceholder = "Select a file to view diff.";
    private const string LoadingPlaceholder = "Loading…";

    private readonly IReadable<DiffTarget?> _target;
    private readonly IRepoRegistry _registry;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;
    private readonly GenerationGuard _loadGen = new();
    private readonly State<DiffRenderState> _renderState =
        new(new DiffRenderState.Placeholder(EmptyPlaceholder));
    private readonly State<string?> _opError = new(null);
    private readonly IDisposable _targetSubscription;
    private readonly IDisposable _workingTreeSubscription;

    public IReadable<DiffRenderState> RenderState => _renderState;
    public IReadable<string?> OpError => _opError;

    public DiffViewModel(
        IReadable<DiffTarget?> target,
        IRepoRegistry registry,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _target = target;
        _registry = registry;
        _gitService = gitService;
        _dispatcher = dispatcher;
        _bus = bus;
        _targetSubscription = _target.Subscribe(_ => StartLoad());
        _workingTreeSubscription = _bus.SubscribeScoped<WorkingTreeChangedMessage>(OnWorkingTreeChanged);
    }

    private void OnWorkingTreeChanged(WorkingTreeChangedMessage msg)
    {
        if (_target.Value == null) return;
        var active = _registry.Active.Value;
        if (active == null || active.Id != msg.RepoId) return;
        StartLoad();
    }

    public void Dispose()
    {
        _loadGen.Bump();
        _targetSubscription.Dispose();
        _workingTreeSubscription.Dispose();
        _renderState.Dispose();
        _opError.Dispose();
    }

    public void StageHunk(int hunkIndex) => ApplyHunk(hunkIndex, cached: true, reverse: false);

    public void UnstageHunk(int hunkIndex) => ApplyHunk(hunkIndex, cached: true, reverse: true);

    public void RequestDiscardHunk(int hunkIndex)
    {
        if (!TryGetPatchContext(hunkIndex, out var repo, out var diff)) return;
        var patch = HunkPatchBuilder.Build(diff, hunkIndex);
        _bus.Broadcast(new ShowDialogMessage(onClose => new DiscardHunkDialog(repo, diff.Path, patch, onClose)));
    }

    private void ApplyHunk(int hunkIndex, bool cached, bool reverse)
    {
        if (!TryGetPatchContext(hunkIndex, out var repo, out var diff)) return;
        var patch = HunkPatchBuilder.Build(diff, hunkIndex);
        var service = _gitService;
        var bus = _bus;
        var dispatcher = _dispatcher;
        var repoId = repo.Id;
        Task.Run(() =>
        {
            string? error;
            try { error = service.ApplyPatch(repo, patch, cached, reverse); }
            catch (Exception ex) { error = ex.Message; }

            dispatcher.Post(() =>
            {
                if (error != null) { _opError.Value = error; return; }
                _opError.Value = null;
                bus.Broadcast(new WorkingTreeChangedMessage(repoId));
            });
        });
    }

    private bool TryGetPatchContext(int hunkIndex, out Repo repo, out DiffResult diff)
    {
        repo = null!;
        diff = null!;
        var active = _registry.Active.Value;
        if (active == null) return false;
        if (_renderState.Value is not DiffRenderState.Loaded loaded) return false;
        if (!HunkPatchBuilder.CanPatchHunk(loaded.Result)) return false;
        if (hunkIndex < 0 || hunkIndex >= loaded.Result.Hunks.Count) return false;
        repo = active;
        diff = loaded.Result;
        return true;
    }

    private void StartLoad()
    {
        var gen = _loadGen.Bump();
        var target = _target.Value;

        if (target == null)
        {
            _renderState.Value = new DiffRenderState.Placeholder(EmptyPlaceholder);
            return;
        }

        var repo = _registry.Active.Value;
        if (repo == null) return;

        if (_renderState.Value is not DiffRenderState.Loaded)
            _renderState.Value = new DiffRenderState.Placeholder(LoadingPlaceholder);

        var service = _gitService;
        var dispatcher = _dispatcher;
        var path = target.Path;
        var side = target.Side;
        var commitSha = target.CommitSha;
        Task.Run(() =>
        {
            DiffRenderState result;
            try
            {
                var diff = service.GetDiff(repo, path, side, commitSha);
                result = new DiffRenderState.Loaded(diff);
            }
            catch (Exception ex)
            {
                result = new DiffRenderState.Placeholder(ex.Message);
            }

            dispatcher.Post(() =>
            {
                if (_loadGen.IsStale(gen)) return;
                _renderState.Value = result;
            });
        });
    }
}
