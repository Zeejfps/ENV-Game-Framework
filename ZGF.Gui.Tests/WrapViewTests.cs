using ZGF.Gui.Views;

namespace ZGF.Gui.Tests;

public class WrapViewTests
{
    private static (RectView Root, WrapView Wrap) Layout(float width, params View[] children)
    {
        var wrap = new WrapView { Gap = 10f, RunGap = 5f };
        foreach (var child in children)
            wrap.Children.Add(child);
        var column = new FlexView { Axis = Axis.Vertical, CrossAxisAlignment = CrossAxisAlignment.Stretch };
        column.Children.Add(wrap);
        var root = new RectView { Width = width, Height = 200f };
        root.Children.Add(column);
        root.LayoutSelf();
        return (root, wrap);
    }

    private static void AssertRect(View view, float left, float bottom, float width, float height)
    {
        Assert.Equal(left, view.Position.Left, 3);
        Assert.Equal(bottom, view.Position.Bottom, 3);
        Assert.Equal(width, view.Position.Width, 3);
        Assert.Equal(height, view.Position.Height, 3);
    }

    [Fact]
    public void ChildrenThatFit_ShareOneRun()
    {
        var a = new RectView { Width = 40f, Height = 20f };
        var b = new RectView { Width = 40f, Height = 20f };
        var (_, wrap) = Layout(100f, a, b);

        Assert.Equal(20f, wrap.Position.Height, 3);
        AssertRect(a, 0f, 180f, 40f, 20f);
        AssertRect(b, 50f, 180f, 40f, 20f);
    }

    [Fact]
    public void ChildThatOverflows_BreaksOntoNextRun()
    {
        var a = new RectView { Width = 40f, Height = 20f };
        var b = new RectView { Width = 40f, Height = 20f };
        var c = new RectView { Width = 40f, Height = 20f };
        var (_, wrap) = Layout(100f, a, b, c);

        Assert.Equal(45f, wrap.Position.Height, 3);
        AssertRect(c, 0f, 155f, 40f, 20f);
    }

    [Fact]
    public void GrowSpacer_PushesFollowingChildToRunEnd()
    {
        var a = new RectView { Width = 20f, Height = 20f };
        var spacer = new FlexItem { Grow = 1f, Child = new RectView() };
        var b = new RectView { Width = 20f, Height = 20f };
        Layout(100f, a, spacer, b);

        AssertRect(b, 80f, 180f, 20f, 20f);
    }

    [Fact]
    public void ChildWiderThanRun_IsClampedToRunWidth()
    {
        var a = new RectView();
        a.Children.Add(new RectView { Width = 150f, Height = 20f });
        Layout(100f, a);

        Assert.Equal(100f, a.Position.Width, 3);
    }
}
