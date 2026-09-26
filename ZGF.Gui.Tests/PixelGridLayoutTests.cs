using ZGF.Gui.Testing;
using ZGF.Gui.Views;

namespace ZGF.Gui.Tests;

/// <summary>
/// Layout on a <see cref="PixelGrid"/>: every view's <see cref="View.Position"/> comes out on whole
/// device pixels however it was computed, so fractions from padding, centring, gaps or grow shares
/// never reach the screen, hit-testing or the next child down.
/// </summary>
public class PixelGridLayoutTests
{
    private sealed class ContentView(float width, float height) : View
    {
        protected override float MeasureWidthIntrinsic() => width;
        protected override float MeasureHeightIntrinsic(float availableWidth) => height;
    }

    private static RectView Root(float w, float h, PixelGrid? grid, params View[] children)
    {
        var root = new RectView { Width = w, Height = h, PixelGrid = grid };
        foreach (var child in children)
            root.Children.Add(child);
        return root;
    }

    private static float Device(float logical, float scale) => logical * scale;

    [Theory]
    [InlineData(1.25f, 33f, 41f)]
    [InlineData(1.5f, 33f, 50f)]
    [InlineData(1.75f, 1f, 2f)]
    public void AChosenSizeRoundsToTheNearestPixel(float scale, float width, float expectedPixels)
    {
        var view = new RectView { Width = width, Height = 10f };
        var root = Root(200f, 100f, new PixelGrid(scale), new FlexView { Children = { view } });

        root.LayoutSelf();

        Assert.Equal(expectedPixels, Device(view.Position.Width, scale), 3);
    }

    [Theory]
    [InlineData(1.25f, 50.3f, 63f)]
    [InlineData(1.5f, 50.01f, 76f)]
    [InlineData(1.25f, 40f, 50f)]
    public void ContentIsNeverRoundedBelowWhatItNeeds(float scale, float needed, float expectedPixels)
    {
        var content = new ContentView(needed, 10f);
        var root = Root(200f, 100f, new PixelGrid(scale), new FlexView { Axis = Axis.Horizontal, Children = { content } });

        root.LayoutSelf();

        Assert.Equal(expectedPixels, Device(content.MeasureWidth(), scale), 3);
        Assert.Equal(expectedPixels, Device(content.Position.Width, scale), 3);
        Assert.True(content.Position.Width >= needed);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(1.75f)]
    public void FractionsFromPaddingGapsAndCentringStayOffTheScreen(float scale)
    {
        var row = new FlexView
        {
            Axis = Axis.Horizontal,
            Gap = 2.7f,
            MainAxisAlignment = MainAxisAlignment.Center,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children = { new ContentView(10.3f, 7.1f), new ContentView(21.9f, 9.9f), new RectView { Width = 3.3f } },
        };
        var padded = new PaddingView { Padding = PaddingStyle.All(3) };
        padded.Children.Add(row);
        var root = Root(101f, 57f, new PixelGrid(scale), padded);

        root.LayoutSelf();

        Assert.Empty(PixelGridAudit.OffGrid(root));
    }

    [Theory]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(1.75f)]
    public void GrownChildrenTileTheirParentWithNoGapsOrOverlaps(float scale)
    {
        var items = Enumerable.Range(0, 3).Select(_ => new RectView()).ToArray();
        var row = new FlexView { Axis = Axis.Horizontal, CrossAxisAlignment = CrossAxisAlignment.Stretch };
        foreach (var item in items)
            row.Children.Add(new FlexItem { Grow = 1, Child = item });
        var root = Root(100f, 20f, new PixelGrid(scale), row);

        root.LayoutSelf();

        Assert.Equal(root.Position.Left, items[0].Position.Left, 3);
        for (var i = 1; i < items.Length; i++)
            Assert.Equal(items[i - 1].Position.Right, items[i].Position.Left, 3);
        Assert.Equal(root.Position.Right, items[^1].Position.Right, 3);

        var widths = items.Select(v => Device(v.Position.Width, scale)).ToArray();
        Assert.True(widths.Max() - widths.Min() <= 1.001f, string.Join(", ", widths));
    }

    [Fact]
    public void AChildAddedLaterLaysOutOnItsParentsGrid()
    {
        var grid = new PixelGrid(1.25f);
        var flex = new FlexView();
        var root = Root(100f, 100f, grid, flex);
        root.LayoutSelf();

        var late = new ContentView(10.3f, 10.3f);
        flex.Children.Add(late);
        root.LayoutSelf();

        Assert.Equal(grid, late.PixelGrid);
        Assert.Empty(PixelGridAudit.OffGrid(root));
    }

    [Fact]
    public void AScaleChangeRelaysOutTheWholeTreeOnTheNewGrid()
    {
        var leaf = new ContentView(10.3f, 10.3f);
        var root = Root(100f, 100f, new PixelGrid(1.25f), new PaddingView { Padding = PaddingStyle.All(3), Children = { leaf } });
        root.LayoutSelf();

        root.PixelGrid = new PixelGrid(1.5f);
        root.LayoutSelf();

        Assert.Equal(new PixelGrid(1.5f), leaf.PixelGrid);
        Assert.Empty(PixelGridAudit.OffGrid(root));
    }

    [Fact]
    public void WithoutAGridLayoutStaysUnrounded()
    {
        var leaf = new RectView { Width = 15f };
        var row = new FlexView { Axis = Axis.Horizontal, MainAxisAlignment = MainAxisAlignment.Center, Children = { leaf } };
        var root = Root(100f, 100f, null, row);

        root.LayoutSelf();

        Assert.Equal(42.5f, leaf.Position.Left, 3);
    }

    [Fact]
    public void TheAuditReportsAViewWithNoGrid()
    {
        var root = Root(100f, 100f, null, new RectView());
        root.LayoutSelf();

        Assert.NotEmpty(PixelGridAudit.OffGrid(root));
    }
}
