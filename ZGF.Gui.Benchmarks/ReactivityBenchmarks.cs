using BenchmarkDotNet.Attributes;
using ZGF.Observable;

namespace ZGF.Gui.Benchmarks;

/// <summary>
/// Isolates <see cref="Derived{T}"/> change propagation. A derived value reads many sources,
/// then a single source is flipped each iteration. The cost under test is per-recompute
/// subscription churn: re-subscribing to every dependency (and the delegate/closure
/// allocations that implies) on each change, versus keeping a stable dependency set.
/// </summary>
[MemoryDiagnoser]
public class ReactivityBenchmarks
{
    private const int SourceCount = 16;
    private State<int>[] _sources = null!;
    private Derived<int> _derived = null!;
    private int _flip;

    [GlobalSetup]
    public void Setup()
    {
        _sources = new State<int>[SourceCount];
        for (var i = 0; i < SourceCount; i++) _sources[i] = new State<int>(i);
        _derived = new Derived<int>(() =>
        {
            var sum = 0;
            foreach (var s in _sources) sum += s.Value;
            return sum;
        });
        // A live subscriber mirrors a bound view, so the notify path runs each recompute.
        _derived.Subscribe(_ => { });
    }

    // One source changes -> the derived recomputes, re-reading all SourceCount dependencies.
    [Benchmark]
    public int RecomputeOnSingleSourceChange()
    {
        _flip ^= 1;
        _sources[0].Value = _flip;
        return _derived.Value;
    }
}
