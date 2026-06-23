using ZGF.Gui.Bindings;
using ZGF.Gui.Views;
using ZGF.Observable;

namespace ZGF.Gui.Tests;

/// <summary>
/// Pins the reconciliation contract of the derived-children binding (the engine behind
/// Column&lt;T&gt;): on a reseed it reuses the child views whose items are unchanged — preserving
/// their instance identity and mounted state — and creates/removes/reorders only the delta,
/// rather than tearing down and rebuilding every row.
/// </summary>
public class DerivedChildrenReconciliationTests
{
    private sealed class ItemRow : RectView
    {
        public int Item { get; }
        public int AttachCount { get; private set; }
        public int DetachCount { get; private set; }

        public ItemRow(int item)
        {
            Item = item;
            Behaviors.Add(new Tracker(this));
        }

        private sealed class Tracker(ItemRow row) : IViewBehavior
        {
            public void Attach(View view) => row.AttachCount++;
            public void Detach(View view) => row.DetachCount++;
        }
    }

    // Drives the derived-children binding from a mutable backing list plus a version State that
    // the compute reads, so Apply() rewrites the list and forces a reconcile against it.
    private sealed class Harness
    {
        public readonly RectView Parent = new();
        private readonly List<int> _items = new();
        private readonly State<int> _version = new(0);
        public int CreateCount { get; private set; }

        public Harness()
        {
            Parent.Children.BindChildren(
                () => { _ = _version.Value; return _items.ToArray(); },
                item => { CreateCount++; return new ItemRow(item); });
            Parent.Mount();
        }

        public void Apply(params int[] items)
        {
            _items.Clear();
            _items.AddRange(items);
            _version.Value++;
        }

        public ItemRow Row(int index) => (ItemRow)Parent.Children[index];

        public int[] ChildItems()
        {
            var result = new int[Parent.Children.Count];
            for (var i = 0; i < result.Length; i++) result[i] = Row(i).Item;
            return result;
        }
    }

    [Fact]
    public void InitialSeed_ProducesChildrenInOrder()
    {
        var h = new Harness();
        h.Apply(1, 2, 3);

        Assert.Equal([1, 2, 3], h.ChildItems());
        Assert.Equal(3, h.CreateCount);
    }

    [Fact]
    public void Append_ReusesExistingChildren_CreatesOnlyTheNewOne()
    {
        var h = new Harness();
        h.Apply(1, 2, 3);
        var (c1, c2, c3) = (h.Row(0), h.Row(1), h.Row(2));
        var createdBefore = h.CreateCount;

        h.Apply(1, 2, 3, 4);

        Assert.Equal([1, 2, 3, 4], h.ChildItems());
        Assert.Same(c1, h.Row(0));
        Assert.Same(c2, h.Row(1));
        Assert.Same(c3, h.Row(2));
        Assert.Equal(createdBefore + 1, h.CreateCount);
    }

    [Fact]
    public void RemoveFromMiddle_DropsTheRightChild_ReusesOthers_NoNewViews()
    {
        var h = new Harness();
        h.Apply(1, 2, 3);
        var (c1, c3) = (h.Row(0), h.Row(2));
        var createdBefore = h.CreateCount;

        h.Apply(1, 3);

        Assert.Equal([1, 3], h.ChildItems());
        Assert.Same(c1, h.Row(0));
        Assert.Same(c3, h.Row(1));
        Assert.Equal(createdBefore, h.CreateCount);
    }

    [Fact]
    public void Reorder_PreservesChildInstances_NoNewViews()
    {
        var h = new Harness();
        h.Apply(1, 2, 3);
        var (c1, c2, c3) = (h.Row(0), h.Row(1), h.Row(2));
        var createdBefore = h.CreateCount;

        h.Apply(3, 1, 2);

        Assert.Equal([3, 1, 2], h.ChildItems());
        Assert.Same(c3, h.Row(0));
        Assert.Same(c1, h.Row(1));
        Assert.Same(c2, h.Row(2));
        Assert.Equal(createdBefore, h.CreateCount);
    }

    [Fact]
    public void ReplacedItem_CreatesNewChild_DropsOldOne()
    {
        var h = new Harness();
        h.Apply(1, 2, 3);
        var c2 = h.Row(1);

        h.Apply(1, 9, 3);

        Assert.Equal([1, 9, 3], h.ChildItems());
        Assert.NotSame(c2, h.Row(1));
        Assert.False(c2.IsMounted);
    }

    [Fact]
    public void ReusedChild_IsNotRemounted_PreservingItsBehaviors()
    {
        var h = new Harness();
        h.Apply(1, 2, 3);
        var c2 = h.Row(1);
        Assert.Equal(1, c2.AttachCount);

        h.Apply(1, 2, 3, 4);
        h.Apply(2, 1, 3, 4);

        Assert.Same(c2, h.Row(0));
        Assert.Equal(1, c2.AttachCount);
        Assert.Equal(0, c2.DetachCount);
    }

    [Fact]
    public void RemovedChild_IsUnmounted_AddedChild_IsMounted()
    {
        var h = new Harness();
        h.Apply(1, 2, 3);
        var removed = h.Row(1);
        Assert.True(removed.IsMounted);

        h.Apply(1, 3, 5);

        Assert.False(removed.IsMounted);
        Assert.Equal([1, 3, 5], h.ChildItems());
        Assert.True(h.Row(2).IsMounted);
    }

    [Fact]
    public void ClearingToEmpty_RemovesAllChildren()
    {
        var h = new Harness();
        h.Apply(1, 2, 3);

        h.Apply();

        Assert.Equal(0, h.Parent.Children.Count);
    }
}
