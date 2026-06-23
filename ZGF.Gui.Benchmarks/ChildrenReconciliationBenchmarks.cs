using BenchmarkDotNet.Attributes;
using ZGF.Gui.Bindings;
using ZGF.Gui.Views;
using ZGF.Observable;

namespace ZGF.Gui.Benchmarks;

/// <summary>
/// Isolates the derived-children binding (the engine behind <c>Column&lt;T&gt;</c>). A list of
/// N rows is bound; each iteration makes a minimal change (append one, drop the oldest) and the
/// binding reseeds. The cost under test is full teardown + rebuild of every row view on any
/// change, versus reusing the rows whose items are unchanged.
/// </summary>
[MemoryDiagnoser]
public class ChildrenReconciliationBenchmarks
{
    private const int RowCount = 200;
    private FlexView _host = null!;
    private List<int> _items = null!;
    private State<int> _version = null!;
    private int _next;

    [GlobalSetup]
    public void Setup()
    {
        _items = new List<int>(Enumerable.Range(0, RowCount));
        _version = new State<int>(0);
        _host = new FlexView();
        _host.Children.BindChildren(
            () => { _ = _version.Value; return _items.ToArray(); },
            (int _) => new RectView { Width = 240f, Height = 18f });
        // Mounting attaches the binding, which seeds the initial RowCount rows.
        _host.Mount();
        _next = RowCount;
    }

    // Steady-state list churn: append one, drop the oldest. Only two rows actually change, so a
    // full reseed rebuilds all RowCount views while a keyed reseed reuses RowCount-1 of them.
    [Benchmark]
    public void AppendAndDropOne()
    {
        _items.Add(_next++);
        _items.RemoveAt(0);
        _version.Value = _next;
    }
}
