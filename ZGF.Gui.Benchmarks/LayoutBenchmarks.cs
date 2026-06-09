using BenchmarkDotNet.Attributes;
using ZGF.Gui;
using ZGF.Gui.Views;

namespace ZGF.Gui.Benchmarks;

/// <summary>
/// Exercises the <see cref="View"/> layout pass on a deep, wide tree to isolate the two
/// hot paths we're tuning:
///
/// IdleFrame_NoChange  — the app calls <c>root.LayoutSelf()</c> every frame even when nothing
/// changed. This isolates dirty <em>detection</em>: with recursive polling it walks the whole
/// tree per frame; with a propagated bit it's O(1).
///
/// IncrementalLayout_OneLeafDirty — one leaf changes and the frame re-lays out. Dominated by
/// the cost of finding the single dirty node among many clean ones.
///
/// Relayout_FullCascade — the root resizes and the whole tree re-lays out. This is the path
/// that re-measures every node, so it isolates measurement redundancy (a flex parent measures
/// each child 2–3× per pass, and that recurses).
/// </summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 5, iterationCount: 12)]
public class LayoutBenchmarks
{
    private const float RootWidth = 1920f;
    private const float RootHeight = 1080f;
    private const int Depth = 5;
    private const int Branching = 3;

    private RectView _idleRoot = null!;
    private RectView _incrementalRoot = null!;
    private View _incrementalLeaf = null!;
    private RectView _relayoutRoot = null!;
    private int _tick;
    private float _leafSize = 20f;

    [GlobalSetup]
    public void Setup()
    {
        _idleRoot = BuildTree(out _);
        _idleRoot.LayoutSelf();

        _incrementalRoot = BuildTree(out _incrementalLeaf);
        _incrementalRoot.LayoutSelf();

        _relayoutRoot = BuildTree(out _);
        _relayoutRoot.LayoutSelf();
    }

    [Benchmark]
    public void IdleFrame_NoChange()
    {
        _idleRoot.LayoutSelf();
    }

    [Benchmark]
    public void IncrementalLayout_OneLeafDirty()
    {
        _leafSize = _leafSize == 20f ? 21f : 20f;
        _incrementalLeaf.Width = _leafSize;
        _incrementalRoot.LayoutSelf();
    }

    [Benchmark]
    public void Relayout_FullCascade()
    {
        _tick ^= 1;
        _relayoutRoot.Width = RootWidth - _tick;
        _relayoutRoot.LayoutSelf();
    }

    private static RectView BuildTree(out View deepLeaf)
    {
        var root = new RectView { Width = RootWidth, Height = RootHeight };
        View? leaf = null;
        BuildChildren(root, 0, ref leaf);
        deepLeaf = leaf!;
        return root;
    }

    private static void BuildChildren(MultiChildView parent, int depth, ref View? deepLeaf)
    {
        if (depth >= Depth)
        {
            for (var i = 0; i < Branching; i++)
            {
                var leaf = new RectView { Width = 20f, Height = 18f };
                parent.Children.Add(leaf);
                deepLeaf ??= leaf;
            }
            return;
        }

        for (var i = 0; i < Branching; i++)
        {
            var node = new FlexView { Axis = depth % 2 == 0 ? Axis.Vertical : Axis.Horizontal };
            parent.Children.Add(node);
            BuildChildren(node, depth + 1, ref deepLeaf);
        }
    }
}
