using ZGF.Gui.Views;
using ZGF.Gui.Widgets;
using ZGF.Observable;

namespace ZGF.Gui.Tests;

/// <summary>
/// Pins the widget-layer contract: the Build/CreateView override guard, shared-prop
/// forwarding, and Each's per-item scoped contexts — items resolve from their own scope
/// (nearest-scope-wins), and scope-created singletons are disposed when the item leaves.
/// </summary>
public class WidgetTests
{
    private sealed record NoOverrideWidget : Widget;

    [Fact]
    public void Widget_WithoutBuildOrCreateView_ThrowsWithTypeName()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => new NoOverrideWidget().BuildView(new Context()));
        Assert.Contains(nameof(NoOverrideWidget), ex.Message);
    }

    private sealed record BoxScreen : Widget
    {
        protected override IWidget Build(Context ctx) => new Box();
    }

    [Fact]
    public void CompositeWidget_ForwardsSharedProps()
    {
        var view = new BoxScreen { Width = 123f, Id = "screen" }.BuildView(new Context());

        Assert.Equal(123f, view.Width.Value);
        Assert.Equal("screen", view.Id);
    }

    private sealed class Item;

    private sealed class ScopeProbe : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    private sealed record ProbeRow : Widget
    {
        protected override View CreateView(Context ctx)
        {
            ctx.Require<Item>();
            ctx.Require<ScopeProbe>();
            return new RectView();
        }
    }

    [Fact]
    public void Each_ItemResolvesFromItsOwnScope()
    {
        var items = new ObservableList<Item>();
        var seen = new List<Item>();
        var template = new SpyRow(seen);
        var root = Each.Of(items, template).BuildView(new Context());
        root.Mount();

        var a = new Item();
        var b = new Item();
        items.Add(a);
        items.Add(b);

        Assert.Equal([a, b], seen);
    }

    private sealed record SpyRow(List<Item> Seen) : Widget
    {
        protected override View CreateView(Context ctx)
        {
            Seen.Add(ctx.Require<Item>());
            return new RectView();
        }
    }

    [Fact]
    public void Each_DisposesItemScope_WhenItemRemoved()
    {
        var items = new ObservableList<Item>();
        var probes = new Dictionary<Item, ScopeProbe>();
        var each = Each.Of(items, new ProbeRow()) with
        {
            ConfigureScope = (scope, item) =>
                scope.AddSingleton(_ =>
                {
                    var probe = new ScopeProbe();
                    probes[item] = probe;
                    return probe;
                }),
        };
        var root = each.BuildView(new Context());
        root.Mount();

        var a = new Item();
        var b = new Item();
        items.Add(a);
        items.Add(b);
        Assert.False(probes[a].IsDisposed);

        items.Remove(a);
        Assert.True(probes[a].IsDisposed);
        Assert.False(probes[b].IsDisposed);
    }

    [Fact]
    public void Each_ParentServicesResolveThroughItemScope()
    {
        var items = new ObservableList<Item>();
        var parentService = new ScopeProbe();
        var ctx = new Context();
        ctx.AddService(parentService);

        ScopeProbe? resolved = null;
        var root = (Each.Of(items, new ParentServiceRow(p => resolved = p))).BuildView(ctx);
        root.Mount();
        items.Add(new Item());

        Assert.Same(parentService, resolved);
    }

    private sealed record ParentServiceRow(Action<ScopeProbe> OnResolved) : Widget
    {
        protected override View CreateView(Context ctx)
        {
            OnResolved(ctx.Require<ScopeProbe>());
            return new RectView();
        }
    }
}
