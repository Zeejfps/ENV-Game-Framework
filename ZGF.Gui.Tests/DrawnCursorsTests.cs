using ZGF.Desktop;
using ZGF.Desktop.Backends;

namespace ZGF.Gui.Tests;

public sealed class DrawnCursorsTests
{
    [Theory]
    [InlineData(MouseCursor.Grab, 1)]
    [InlineData(MouseCursor.Grabbing, 1)]
    [InlineData(MouseCursor.Grab, 2)]
    public void AHandIsSixteenArtPixelsSquareScaledWithItsHotspotInTheMiddle(MouseCursor cursor, int scale)
    {
        var image = DrawnCursors.Draw(cursor, scale);

        Assert.Equal(16 * scale, image.Size);
        Assert.Equal(image.Size * image.Size * 4, image.Rgba.Length);
        Assert.Equal((8 * scale, 8 * scale), (image.HotspotX, image.HotspotY));
    }

    [Fact]
    public void AHandIsAnOpaqueBlackOutlineAroundWhiteOnTransparency()
    {
        var image = DrawnCursors.Draw(MouseCursor.Grab, 1);

        var colors = new HashSet<(byte, byte, byte, byte)>();
        for (var i = 0; i < image.Rgba.Length; i += 4)
            colors.Add((image.Rgba[i], image.Rgba[i + 1], image.Rgba[i + 2], image.Rgba[i + 3]));

        Assert.Equal(new HashSet<(byte, byte, byte, byte)> { (0, 0, 0, 0), (0, 0, 0, 255), (255, 255, 255, 255) }, colors);
    }

    [Fact]
    public void OnlyTheHandsAreDrawn()
    {
        Assert.True(DrawnCursors.Draws(MouseCursor.Grab));
        Assert.True(DrawnCursors.Draws(MouseCursor.Grabbing));
        Assert.False(DrawnCursors.Draws(MouseCursor.Move));
        Assert.False(DrawnCursors.Draws(MouseCursor.Hand));
    }
}
