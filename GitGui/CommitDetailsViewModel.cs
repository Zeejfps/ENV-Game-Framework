using ZGF.Gui;
using ZGF.Observable;

namespace GitGui;

public abstract record CommitDetailsRenderState
{
    public sealed record Placeholder(string Text) : CommitDetailsRenderState;
    public sealed record Loaded(CommitDetails Details) : CommitDetailsRenderState;
}

public sealed class CommitDetailsViewModel : IDisposable
{
    private const string DefaultPlaceholder = "Select a commit to view details.";
    private const string LoadingPlaceholder = "Loading…";

    private readonly IGitService _gitService;
    private readonly IRepoRegistry _registry;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;
    private readonly SubscriptionGroup _subscriptions = new();
    private readonly GenerationGuard _loadGen = new();
    private readonly State<CommitDetailsRenderState> _renderState =
        new(new CommitDetailsRenderState.Placeholder(DefaultPlaceholder));
    private readonly State<string?> _selectedPath = new(null);
    private readonly State<DiffTarget?> _selectedTarget = new(null);
    private string? _currentSha;

    public IReadable<CommitDetailsRenderState> RenderState => _renderState;
    public IReadable<string?> SelectedPath => _selectedPath;
    public IReadable<DiffTarget?> SelectedTarget => _selectedTarget;
    public DiffViewModel DiffVm { get; }

    public CommitDetailsViewModel(
        IGitService gitService,
        IRepoRegistry registry,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _gitService = gitService;
        _registry = registry;
        _dispatcher = dispatcher;
        _bus = bus;
        DiffVm = new DiffViewModel(_selectedTarget, registry, gitService, dispatcher, bus);
        _subscriptions.Add(_bus.SubscribeScoped<CommitSelectedMessage>(OnCommitSelected));
    }

    public void Dispose()
    {
        _loadGen.Bump();
        DiffVm.Dispose();
        _subscriptions.Dispose();
        _renderState.Dispose();
        _selectedPath.Dispose();
        _selectedTarget.Dispose();
    }

    public void SelectFile(string path)
    {
        if (string.IsNullOrEmpty(_currentSha)) return;
        _selectedPath.Value = path;
        _selectedTarget.Value = new DiffTarget(path, DiffSide.Commit, _currentSha);
    }

    private void OnCommitSelected(CommitSelectedMessage msg)
    {
        if (string.IsNullOrEmpty(msg.Sha))
        {
            _loadGen.Bump();
            _currentSha = null;
            _selectedPath.Value = null;
            _selectedTarget.Value = null;
            _renderState.Value = new CommitDetailsRenderState.Placeholder(DefaultPlaceholder);
            return;
        }
        StartLoad(msg.RepoId, msg.Sha);
    }

    private void StartLoad(Guid repoId, string sha)
    {
        var repo = _registry.Active.Value;
        if (repo == null || repo.Id != repoId) return;

        var gen = _loadGen.Bump();
        _currentSha = sha;
        _selectedPath.Value = null;
        _selectedTarget.Value = null;
        _renderState.Value = new CommitDetailsRenderState.Placeholder(LoadingPlaceholder);

        var service = _gitService;
        var dispatcher = _dispatcher;
        Task.Run(() =>
        {
            CommitDetailsRenderState result;
            try
            {
                var details = service.LoadDetails(repo, sha);
                if (details.ErrorMessage != null)
                {
                    result = new CommitDetailsRenderState.Placeholder(details.ErrorMessage);
                }
                else
                {
                    var pointerChanges = service.GetSubmodulePointerChanges(repo, sha);
                    if (pointerChanges.Count > 0)
                        details = MergePointerChanges(details, pointerChanges);
                    result = new CommitDetailsRenderState.Loaded(details);
                }
            }
            catch (Exception ex)
            {
                result = new CommitDetailsRenderState.Placeholder(ex.Message);
            }

            dispatcher.Post(() =>
            {
                if (_loadGen.IsStale(gen)) return;
                _renderState.Value = result;
            });
        });
    }

    private static CommitDetails MergePointerChanges(CommitDetails details, IReadOnlyList<SubmodulePointerChange> changes)
    {
        var byPath = new Dictionary<string, SubmodulePointerChange>(StringComparer.Ordinal);
        foreach (var c in changes) byPath[c.Path] = c;

        var newFiles = new List<FileChange>(details.Files.Count + changes.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var f in details.Files)
        {
            if (byPath.TryGetValue(f.Path, out var pc))
            {
                newFiles.Add(new FileChange(f.Path, f.OldPath, FileChangeStatus.Submodule)
                {
                    PointerChange = pc,
                });
                seen.Add(f.Path);
            }
            else
            {
                newFiles.Add(f);
            }
        }
        foreach (var c in changes)
        {
            if (seen.Contains(c.Path)) continue;
            newFiles.Add(new FileChange(c.Path, null, FileChangeStatus.Submodule)
            {
                PointerChange = c,
            });
        }
        return details with { Files = newFiles };
    }
}
