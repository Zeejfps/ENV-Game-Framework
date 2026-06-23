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

    [Fact]
    public void HorizontalFlex_Rtl_MirrorsChildrenWithinContainer()
    {
        var a = new RectView { Width = 30f };
        var b = new RectView { Width = 30f };
        var flex = new FlexView
        {
            Axis = Axis.Horizontal,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            IsRtl = true,
        };
        flex.Children.Add(a);
        flex.Children.Add(b);
        var root = Root(100f, 50f, flex);

        root.LayoutSelf();

        // LTR would place a@[0..30], b@[30..60]; mirrored within [0,100] the first child sits on
        // the right and the order reverses visually.
        AssertRect(a, 70f, 0f, 30f, 50f);
        AssertRect(b, 40f, 0f, 30f, 50f);
    }

    [Fact]
    public void HorizontalFlex_Ltr_IsUnmirrored()
    {
        var a = new RectView { Width = 30f };
        var b = new RectView { Width = 30f };
        var flex = new FlexView { Axis = Axis.Horizontal, CrossAxisAlignment = CrossAxisAlignment.Stretch };
        flex.Children.Add(a);
        flex.Children.Add(b);
        var root = Root(100f, 50f, flex);

        root.LayoutSelf();

        AssertRect(a, 0f, 0f, 30f, 50f);
        AssertRect(b, 30f, 0f, 30f, 50f);
    }

    [Fact]
    public void VerticalFlex_Rtl_MirrorsCrossAxis()
    {
        // A left-aligned (cross Start) column item moves to the right under RTL; vertical
        // (main-axis) stacking is untouched.
        var a = new RectView { Width = 20f, Height = 30f };
        var flex = new FlexView { Axis = Axis.Vertical, CrossAxisAlignment = CrossAxisAlignment.Start, IsRtl = true };
        flex.Children.Add(a);
        var root = Root(100f, 100f, flex);

        root.LayoutSelf();

        AssertRect(a, 80f, 70f, 20f, 30f);
    }

    [Fact]
    public void BorderLayout_Rtl_SwapsWestAndEast()
    {
        var west = new RectView { Width = 20f };
        var east = new RectView { Width = 10f };
        var center = new RectView();
        var bl = new BorderLayoutView { West = west, East = east, Center = center, IsRtl = true };
        var root = Root(100f, 50f, bl);

        root.LayoutSelf();

        // West (leading) is now on the right, East (trailing) on the left, Center fills the middle.
        AssertRect(east, 0f, 0f, 10f, 50f);
        AssertRect(west, 80f, 0f, 20f, 50f);
        AssertRect(center, 10f, 0f, 70f, 50f);
    }

    [Fact]
    public void ScrollPane_Rtl_RestsContentAtRightEdge()
    {
        var pane = new ScrollPane();
        var content = new RectView { Width = 200f, Height = 20f };
        pane.Children.Add(content);
        var root = Root(100f, 50f, pane);
        root.IsRtl = true;

        root.LayoutSelf();

        // Content (200) is wider than the 100 viewport; at rest its right edge sits at the viewport
        // right, so its left edge is at 100 - 200 = -100 (LTR would rest it at 0).
        Assert.Equal(-100f, content.Position.Left, 3);
    }

    [Fact]
    public void ScrollPane_Ltr_RestsContentAtLeftEdge()
    {
        var pane = new ScrollPane();
        var content = new RectView { Width = 200f, Height = 20f };
        pane.Children.Add(content);
        var root = Root(100f, 50f, pane);

        root.LayoutSelf();

        Assert.Equal(0f, content.Position.Left, 3);
    }

    [Fact]
    public void PaddingView_Rtl_SwapsInlineInsets()
    {
        // Left/Right are inline start/end: an indent (larger Left) insets from the right under RTL.
        var leaf = new RectView();
        var padding = new PaddingView { Padding = new PaddingStyle { Left = 20, Right = 5 } };
        padding.Children.Add(leaf);
        var root = Root(100f, 50f, padding);
        root.IsRtl = true;

        root.LayoutSelf();

        // LTR insets to [20, 95]; under RTL the insets swap to [5, 80].
        AssertRect(leaf, 5f, 0f, 75f, 50f);
    }

    [Fact]
    public void IsRtl_IsInheritedFromAncestor()
    {
        // A FlexView that never sets IsRtl inherits it from an ancestor, so setting direction once
        // near the root mirrors the whole tree.
        var a = new RectView { Width = 30f };
        var flex = new FlexView { Axis = Axis.Horizontal, CrossAxisAlignment = CrossAxisAlignment.Stretch };
        flex.Children.Add(a);
        var root = Root(100f, 50f, flex);
        root.IsRtl = true;

        root.LayoutSelf();

        Assert.True(flex.IsRtl);
        AssertRect(a, 70f, 0f, 30f, 50f);
    }

    [Fact]
    public void BorderLayout_Ltr_KeepsWestLeftEastRight()
    {
        var west = new RectView { Width = 20f };
        var east = new RectView { Width = 10f };
        var center = new RectView();
        var bl = new BorderLayoutView { West = west, East = east, Center = center };
        var root = Root(100f, 50f, bl);

        root.LayoutSelf();

        AssertRect(west, 0f, 0f, 20f, 50f);
        AssertRect(east, 90f, 0f, 10f, 50f);
        AssertRect(center, 20f, 0f, 70f, 50f);
    }
}
