using ZGF.Gui;
using ZGF.Observable;

namespace GitGui;

public record DiffTarget(string Path, DiffSide Side);

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
    private readonly GenerationGuard _loadGen = new();
    private readonly State<DiffRenderState> _renderState =
        new(new DiffRenderState.Placeholder(EmptyPlaceholder));
    private readonly IDisposable _targetSubscription;

    public IReadable<DiffRenderState> RenderState => _renderState;

    public DiffViewModel(
        IReadable<DiffTarget?> target,
        IRepoRegistry registry,
        IGitService gitService,
        IUiDispatcher dispatcher)
    {
        _target = target;
        _registry = registry;
        _gitService = gitService;
        _dispatcher = dispatcher;
        _targetSubscription = _target.Subscribe(_ => StartLoad());
    }

    public void Dispose()
    {
        _loadGen.Bump();
        _targetSubscription.Dispose();
        _renderState.Dispose();
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
        Task.Run(() =>
        {
            DiffRenderState result;
            try
            {
                var diff = service.GetDiff(repo, path, side);
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
