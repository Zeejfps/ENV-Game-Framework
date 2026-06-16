using ZGF.Gui.Bindings;
using ZGF.Gui.Views;
using ZGF.Observable;

namespace ZGF.Gui.Tests;

/// <summary>
/// Pins the mount lifecycle contract: behaviors attach exactly while a view is part of a
/// mounted tree, children mount before a parent's behaviors attach (and detach after them),
/// tree mutations propagate mount state, and per-mount resources (view models, scoped
/// disposables, dynamic-children subscriptions) are created on mount and released on unmount.
/// </summary>
public class MountLifecycleTests
{
    private sealed class TrackingBehavior(string name, List<string>? log = null) : IViewBehavior
    {
        public int AttachCount { get; private set; }
        public int DetachCount { get; private set; }

        public void Attach(View view)
        {
            AttachCount++;
            log?.Add($"{name}:attach");
        }

        public void Detach(View view)
        {
            DetachCount++;
            log?.Add($"{name}:detach");
        }
    }

    private sealed class DisposableProbe : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    [Fact]
    public void Mount_AttachesBehaviors_ExactlyOnce()
    {
        var view = new RectView();
        var behavior = new TrackingBehavior("b");
        view.Behaviors.Add(behavior);

        view.Mount();
        view.Mount();

        Assert.True(view.IsMounted);
        Assert.Equal(1, behavior.AttachCount);
    }

    [Fact]
    public void Unmount_DetachesBehaviors_AndIsIdempotent()
    {
        var view = new RectView();
        var behavior = new TrackingBehavior("b");
        view.Behaviors.Add(behavior);

        view.Mount();
        view.Unmount();
        view.Unmount();

        Assert.False(view.IsMounted);
        Assert.Equal(1, behavior.DetachCount);
    }

    [Fact]
    public void Unmount_WithoutMount_DoesNothing()
    {
        var view = new RectView();
        var behavior = new TrackingBehavior("b");
        view.Behaviors.Add(behavior);

        view.Unmount();

        Assert.Equal(0, behavior.DetachCount);
    }

    [Fact]
    public void Mount_ChildBehaviorsAttachBeforeParents()
    {
        var log = new List<string>();
        var child = new RectView();
        child.Behaviors.Add(new TrackingBehavior("child", log));
        var parent = new RectView { Children = { child } };
        parent.Behaviors.Add(new TrackingBehavior("parent", log));

        parent.Mount();

        Assert.Equal(["child:attach", "parent:attach"], log);
    }

    [Fact]
    public void Unmount_ParentBehaviorsDetachBeforeChildren()
    {
        var log = new List<string>();
        var child = new RectView();
        child.Behaviors.Add(new TrackingBehavior("child", log));
        var parent = new RectView { Children = { child } };
        parent.Behaviors.Add(new TrackingBehavior("parent", log));

        parent.Mount();
        log.Clear();
        parent.Unmount();

        Assert.Equal(["parent:detach", "child:detach"], log);
    }

    [Fact]
    public void AddChild_ToMountedParent_MountsSubtree()
    {
        var parent = new RectView();
        parent.Mount();

        var grandchild = new RectView();
        var behavior = new TrackingBehavior("g");
        grandchild.Behaviors.Add(behavior);
        var child = new RectView { Children = { grandchild } };

        parent.Children.Add(child);

        Assert.True(child.IsMounted);
        Assert.True(grandchild.IsMounted);
        Assert.Equal(1, behavior.AttachCount);
    }

    [Fact]
    public void AddChild_ToUnmountedParent_StaysUnmounted()
    {
        var parent = new RectView();
        var child = new RectView();
        var behavior = new TrackingBehavior("c");
        child.Behaviors.Add(behavior);

        parent.Children.Add(child);

        Assert.False(child.IsMounted);
        Assert.Equal(0, behavior.AttachCount);
    }

    [Fact]
    public void RemoveChild_UnmountsSubtree()
    {
        var grandchild = new RectView();
        var behavior = new TrackingBehavior("g");
        grandchild.Behaviors.Add(behavior);
        var child = new RectView { Children = { grandchild } };
        var parent = new RectView { Children = { child } };
        parent.Mount();

        parent.Children.Remove(child);

        Assert.False(child.IsMounted);
        Assert.False(grandchild.IsMounted);
        Assert.Equal(1, behavior.DetachCount);
    }

    [Fact]
    public void Reparenting_MountedToMounted_DetachesAndReattaches()
    {
        var child = new RectView();
        var behavior = new TrackingBehavior("c");
        child.Behaviors.Add(behavior);
        var a = new RectView { Children = { child } };
        var b = new RectView();
        a.Mount();
        b.Mount();

        b.Children.Add(child);

        Assert.True(child.IsMounted);
        Assert.Same(b, child.Parent);
        Assert.Equal(2, behavior.AttachCount);
        Assert.Equal(1, behavior.DetachCount);
    }

    [Fact]
    public void Remount_ReattachesBehaviors()
    {
        var view = new RectView();
        var behavior = new TrackingBehavior("b");
        view.Behaviors.Add(behavior);

        view.Mount();
        view.Unmount();
        view.Mount();

        Assert.Equal(2, behavior.AttachCount);
        Assert.Equal(1, behavior.DetachCount);
    }

    [Fact]
    public void AddBehavior_WhileMounted_AttachesImmediately()
    {
        var view = new RectView();
        view.Mount();

        var behavior = new TrackingBehavior("b");
        view.Behaviors.Add(behavior);

        Assert.Equal(1, behavior.AttachCount);
    }

    [Fact]
    public void RemoveBehavior_WhileMounted_Detaches()
    {
        var view = new RectView();
        var behavior = new TrackingBehavior("b");
        view.Behaviors.Add(behavior);
        view.Mount();

        view.Behaviors.Remove(behavior);

        Assert.Equal(1, behavior.DetachCount);
    }

    [Fact]
    public void MoveChild_DoesNotRemount()
    {
        var a = new RectView();
        var behaviorA = new TrackingBehavior("a");
        a.Behaviors.Add(behaviorA);
        var b = new RectView();
        var parent = new RectView { Children = { a, b } };
        parent.Mount();

        parent.Children.Move(a, 1);

        Assert.Equal(1, behaviorA.AttachCount);
        Assert.Equal(0, behaviorA.DetachCount);
    }

    [Fact]
    public void ScopedBehavior_CreatesOnMount_DisposesOnUnmount()
    {
        var view = new RectView();
        DisposableProbe? created = null;
        view.Use(() => created = new DisposableProbe());

        view.Mount();
        Assert.NotNull(created);
        Assert.False(created!.IsDisposed);

        view.Unmount();
        Assert.True(created.IsDisposed);
    }

    [Fact]
    public void ViewModelBehavior_NewInstancePerMount_DisposedOnUnmount()
    {
        var view = new RectView();
        var instances = new List<DisposableProbe>();
        view.UseViewModel(
            () =>
            {
                var vm = new DisposableProbe();
                instances.Add(vm);
                return vm;
            },
            bind: _ => { });

        view.Mount();
        view.Unmount();
        view.Mount();

        Assert.Equal(2, instances.Count);
        Assert.True(instances[0].IsDisposed);
        Assert.False(instances[1].IsDisposed);
    }

    [Fact]
    public void BindChildren_AddAndRemove_MountAndUnmountRows()
    {
        var items = new ObservableList<string>();
        var parent = new RectView();
        var rowBehaviors = new Dictionary<string, TrackingBehavior>();
        parent.Children.BindChildren(items, item =>
        {
            var row = new RectView();
            var behavior = new TrackingBehavior(item);
            rowBehaviors[item] = behavior;
            row.Behaviors.Add(behavior);
            return row;
        });
        parent.Mount();

        items.Add("a");
        items.Add("b");
        Assert.Equal(2, parent.Children.Count);
        Assert.Equal(1, rowBehaviors["a"].AttachCount);

        items.RemoveAt(0);
        Assert.Equal(1, parent.Children.Count);
        Assert.Equal(1, rowBehaviors["a"].DetachCount);
        Assert.Equal(0, rowBehaviors["b"].DetachCount);
    }

    [Fact]
    public void BindChildren_SubscriptionFollowsMountedLifetime()
    {
        var items = new ObservableList<string>();
        var parent = new RectView();
        parent.Children.BindChildren(items, _ => new RectView());

        items.Add("before-mount");
        Assert.Equal(0, parent.Children.Count);

        parent.Mount();
        Assert.Equal(1, parent.Children.Count);

        parent.Unmount();
        items.Add("after-unmount");
        Assert.Equal(1, parent.Children.Count);
    }
}
