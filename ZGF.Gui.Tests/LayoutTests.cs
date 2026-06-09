using ZGF.Gui;
using ZGF.Gui.Views;

namespace ZGF.Gui.Tests;

/// <summary>
/// Geometry-level tests for the <see cref="View"/> layout pass. These pin the resolved
/// <see cref="View.Position"/> of representative containers so the dirty-propagation and
/// measure-memoization optimizations can be verified to preserve behavior — covering the
/// initial pass, an incremental single-node change, and a no-op idle pass.
/// </summary>
public class LayoutTests
{
    private static RectView Root(float w, float h, params View[] children)
    {
        var root = new RectView { Width = w, Height = h };
        foreach (var child in children)
            root.Children.Add(child);
        return root;
    }

    private static void AssertRect(View view, float left, float bottom, float width, float height)
    {
        Assert.Equal(left, view.Position.Left, 3);
        Assert.Equal(bottom, view.Position.Bottom, 3);
        Assert.Equal(width, view.Position.Width, 3);
        Assert.Equal(height, view.Position.Height, 3);
    }

    [Fact]
    public void PaddingView_InsetsChild()
    {
        var leaf = new RectView();
        var padding = new PaddingView { Padding = PaddingStyle.All(10) };
        padding.Children.Add(leaf);
        var root = Root(100f, 80f, padding);

        root.LayoutSelf();

        AssertRect(leaf, 10f, 10f, 80f, 60f);
    }

    [Fact]
    public void VerticalFlex_StacksChildrenFromTop()
    {
        var a = new RectView { Height = 30f };
        var b = new RectView { Height = 30f };
        var flex = new FlexView { Axis = Axis.Vertical, CrossAxisAlignment = CrossAxisAlignment.Stretch };
        flex.Children.Add(a);
        flex.Children.Add(b);
        var root = Root(100f, 100f, flex);

        root.LayoutSelf();

        // Y-up: first child sits at the top, second directly below it; both stretched to full width.
        AssertRect(a, 0f, 70f, 100f, 30f);
        AssertRect(b, 0f, 40f, 100f, 30f);
    }

    [Fact]
    public void IncrementalChange_RepositionsThroughCleanAncestor()
    {
        var leaf = new RectView();
        var padding = new PaddingView { Padding = PaddingStyle.All(10) };
        padding.Children.Add(leaf);
        var root = Root(100f, 80f, padding);
        root.LayoutSelf();
        AssertRect(leaf, 10f, 10f, 80f, 60f);

        // Only the PaddingView changes; the root is not self-dirty. The propagated children-dirty
        // bit must still drive a re-layout that reaches the leaf through the clean root.
        padding.Padding = PaddingStyle.All(5);
        root.LayoutSelf();

        AssertRect(leaf, 5f, 5f, 90f, 70f);
    }

    [Fact]
    public void IdlePass_LeavesGeometryUnchanged()
    {
        var leaf = new RectView();
        var padding = new PaddingView { Padding = PaddingStyle.All(10) };
        padding.Children.Add(leaf);
        var root = Root(100f, 80f, padding);

        root.LayoutSelf();
        var first = leaf.Position;

        // A second pass with nothing dirty must be a no-op, not corrupt the resolved geometry.
        root.LayoutSelf();

        Assert.Equal(first, leaf.Position);
    }

    [Fact]
    public void RootResize_CascadesToDescendants()
    {
        var leaf = new RectView();
        var padding = new PaddingView { Padding = PaddingStyle.All(10) };
        padding.Children.Add(leaf);
        var root = Root(100f, 80f, padding);
        root.LayoutSelf();

        root.Width = 200f;
        root.LayoutSelf();

        AssertRect(leaf, 10f, 10f, 180f, 60f);
    }
}
