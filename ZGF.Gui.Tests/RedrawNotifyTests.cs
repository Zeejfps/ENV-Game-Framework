using ZGF.Gui.Views;

namespace ZGF.Gui.Tests;

/// <summary>
/// Pins the damage-gating contract of <see cref="View.OnRedrawNeeded"/>: any change that
/// dirties the tree must notify the root hook (windows repaint only when asked), and a
/// dirty batch between two layout passes must notify at most once per clean-to-dirty
/// transition rather than per change.
/// </summary>
public class RedrawNotifyTests
{
    private static (RectView Root, PaddingView Mid, RectView Leaf) Tree()
    {
        var leaf = new RectView();
        var mid = new PaddingView { Padding = PaddingStyle.All(10) };
        mid.Children.Add(leaf);
        var root = new RectView { Width = 100f, Height = 80f };
        root.Children.Add(mid);
        return (root, mid, leaf);
    }

    // The first pass assigns child constraints, which re-dirties the tree; the second converges.
    private static void LayoutToSteadyState(View root)
    {
        root.LayoutSelf();
        root.LayoutSelf();
    }

    [Fact]
    public void DeepChange_NotifiesRootHook()
    {
        var (root, _, leaf) = Tree();
        LayoutToSteadyState(root);

        var notified = 0;
        root.OnRedrawNeeded = () => notified++;

        leaf.Width = 5f;

        Assert.Equal(1, notified);
    }

    [Fact]
    public void SecondChangeInSameBatch_DoesNotNotifyAgain()
    {
        var leafA = new RectView();
        var midA = new PaddingView { Padding = PaddingStyle.All(10) };
        midA.Children.Add(leafA);
        var leafB = new RectView();
        var midB = new PaddingView { Padding = PaddingStyle.All(10) };
        midB.Children.Add(leafB);
        var root = new RectView { Width = 100f, Height = 80f };
        root.Children.Add(midA);
        root.Children.Add(midB);
        LayoutToSteadyState(root);

        var notified = 0;
        root.OnRedrawNeeded = () => notified++;

        leafA.Width = 5f;
        leafB.Width = 5f;

        Assert.Equal(1, notified);
    }

    [Fact]
    public void ChangeAfterLayout_NotifiesAgain()
    {
        var (root, _, leaf) = Tree();
        LayoutToSteadyState(root);

        var notified = 0;
        root.OnRedrawNeeded = () => notified++;

        leaf.Width = 5f;
        Assert.Equal(1, notified);

        LayoutToSteadyState(root);
        notified = 0;
        leaf.Width = 7f;

        Assert.Equal(1, notified);
    }

    [Fact]
    public void RootSelfChange_Notifies()
    {
        var (root, _, _) = Tree();
        LayoutToSteadyState(root);

        var notified = 0;
        root.OnRedrawNeeded = () => notified++;

        root.Width = 200f;

        Assert.Equal(1, notified);
    }

    [Fact]
    public void TextViewRotationChange_Notifies()
    {
        var text = new TextView(new FakeCanvas());
        var root = new RectView { Width = 100f, Height = 80f };
        root.Children.Add(text);
        LayoutToSteadyState(root);

        var notified = 0;
        root.OnRedrawNeeded = () => notified++;

        text.Rotation = 1.5f;

        Assert.Equal(1, notified);
    }

    [Fact]
    public void AttachingDirtySubtree_NotifiesThroughParent()
    {
        var (root, mid, _) = Tree();
        LayoutToSteadyState(root);

        var notified = 0;
        root.OnRedrawNeeded = () => notified++;

        var detached = new RectView { Width = 10f };
        mid.Children.Add(detached);

        Assert.Equal(1, notified);
    }
}
