using ZGF.Gui.Testing.Gpu;

namespace ZGF.Gui.Immediate.Tests;

/// <summary>
/// A rounded corner draws the same wherever it lands on the pixel grid. At 1x a corner can start in the
/// middle of a 2x2 pixel quad, where a derivative read across the quad came back undefined and left a
/// 1-pixel seam down the corner, as on a button on a 1x external monitor.
/// </summary>
[Collection(GpuCollection.Name)]
public sealed class RoundedCornerTests(GpuDeviceFixture fixture)
{
    private const int Width = 80;
    private const int Height = 40;

    private RgbaImage Draw(float left, float top, float borderWidth)
    {
        using var frames = GpuFrames.Running(fixture.Device, new DefaultUiState(), ui =>
        {
            ui.WindowBackground = 0xFF000000u;
            using (ui.Column(new Box { Width = Width, Height = Height, Padding = new Spacing(left, top, 0f, 0f) }))
                ui.Leaf(new Box
                {
                    Width = 60, Height = 24, Radius = 8f, Background = 0xFFFFFFFFu,
                    BorderColor = borderWidth > 0f ? 0xFF808080u : 0u, BorderWidth = borderWidth,
                });
        }, Width, Height, 1f, fallbackFonts: static () => []);
        return frames.Read();
    }

    [GpuFact]
    public void A_corner_starting_mid_quad_draws_as_one_starting_on_a_quad()
    {
        Moved(borderWidth: 0f);
        Moved(borderWidth: 1f);
    }

    private void Moved(float borderWidth)
    {
        var even = Draw(2f, 2f, borderWidth);

        foreach (var (dx, dy) in new[] { (1, 0), (0, 1), (1, 1) })
        {
            var odd = Draw(2f + dx, 2f + dy, borderWidth);
            for (var y = 0; y < Height - 1; y++)
            for (var x = 0; x < Width - 1; x++)
                Assert.True(even[x, y] == odd[x + dx, y + dy], $"border {borderWidth}, moved by ({dx}, {dy}), pixel ({x}, {y}): {even[x, y]} became {odd[x + dx, y + dy]}");
        }
    }
}
