using ZGF.Gui;

namespace ZGF.Gui.Tests;

public class SizeTests
{
    [Fact]
    public void Size_StoresWidthAndHeight()
    {
        var size = new Size { Width = 800f, Height = 600f };

        Assert.Equal(800f, size.Width);
        Assert.Equal(600f, size.Height);
    }

    [Fact]
    public void Size_ValueEquality_HoldsForEqualDimensions()
    {
        var a = new Size { Width = 1280f, Height = 720f };
        var b = new Size { Width = 1280f, Height = 720f };

        Assert.Equal(a, b);
    }
}
