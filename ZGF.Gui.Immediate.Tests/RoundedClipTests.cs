using ZGF.Geometry;
using ZGF.Gui.Testing;
using ZGF.Gui.Testing.Gpu;

namespace ZGF.Gui.Immediate.Tests;

/// <summary>
/// A box that clips and has rounded corners cuts what is drawn inside it to those corners: its children, what
/// floats inside it keeping its clip, and a scroll's bar. A box with square corners cuts to its rect as before.
/// </summary>
[Collection(GpuCollection.Name)]
public sealed class RoundedClipTests(GpuDeviceFixture fixture)
{
    private const int Size = 60;
    private const float Radius = 20f;
    private const uint Red = 0xFFFF0000u;
    private static readonly Rgba8 Black = new(0, 0, 0, 255);
    private static readonly Rgba8 OpaqueRed = new(255, 0, 0, 255);

    private RgbaImage Draw(Action<Ui<DefaultUiState>> body)
    {
        using var frames = GpuFrames.Running(fixture.Device, new DefaultUiState(), ui =>
        {
            ui.WindowBackground = 0xFF000000u;
            body(ui);
        }, Size, Size, fallbackFonts: static () => []);
        return frames.Read();
    }

    private static void Clipping(Ui<DefaultUiState> ui, float? radius, Action inside)
    {
        using (ui.Box(new Box { Width = Size, Height = Size, Radius = radius, Clip = true }))
            inside();
    }

    [GpuFact]
    public void A_rounded_box_cuts_a_child_filling_it_to_its_corners()
    {
        var image = Draw(ui => Clipping(ui, Radius, () => ui.Leaf(new Box { Width = Size, Height = Size, Background = Red })));

        Assert.Equal(Black, image[1, 1]);
        Assert.Equal(Black, image[Size - 2, Size - 2]);
        Assert.Equal(OpaqueRed, image[Size / 2, 1]);
        Assert.Equal(OpaqueRed, image[Size / 2, Size / 2]);
        // The arc crosses the top row's pixel centres at x = 20 - sqrt(20^2 - 19.5^2) ~ 15.56, so pixel 15 is about half covered.
        var edge = image[15, 0];
        Assert.InRange((int)edge.R, 110, 140);
        Assert.Equal(new Rgba8(edge.R, 0, 0, 255), edge);
    }

    [GpuFact]
    public void A_square_box_cuts_a_child_only_to_its_rect()
    {
        var image = Draw(ui => Clipping(ui, 0f, () => ui.Leaf(new Box { Width = Size, Height = Size, Background = Red })));

        Assert.Equal(OpaqueRed, image[1, 1]);
        Assert.Equal(OpaqueRed, image[Size - 2, Size - 2]);
    }

    [GpuFact]
    public void What_floats_inside_a_rounded_box_keeping_its_clip_is_cut_to_its_corners_too()
    {
        var image = Draw(ui => Clipping(ui, Radius, () =>
        {
            using (ui.Float(0f, 0f, 0f, 0f, new Box { Width = Size, Height = Size, Background = Red }, clipped: true))
            {
            }
        }));

        Assert.Equal(Black, image[1, 1]);
        Assert.Equal(OpaqueRed, image[Size / 2, Size / 2]);
    }

    [GpuFact]
    public void A_rounded_clip_inside_another_cuts_to_its_own_corners()
    {
        var image = Draw(ui => Clipping(ui, 0f, () =>
        {
            using (ui.Box(new Box { Width = Size, Height = Size, Radius = Radius, Clip = true }))
                ui.Leaf(new Box { Width = Size, Height = Size, Background = Red });
        }));

        Assert.Equal(Black, image[1, 1]);
        Assert.Equal(OpaqueRed, image[Size / 2, Size / 2]);
    }

    [GpuFact]
    public void A_scrolls_bar_inside_a_rounded_box_is_cut_where_it_meets_the_corner()
    {
        var image = Draw(ui => Clipping(ui, Radius, () =>
        {
            using (ui.Scroll("list", ScrollAxes.Vertical, new Box { Width = Sizing.Grow(), Height = Sizing.Grow() }))
            {
                for (var i = 0; i < 20; i++)
                    ui.Leaf(new Box { Width = 10f, Height = 40f });
            }
        }));
        var thumb = UiTheme.Dark.ScrollThumb;
        var thumbPixel = new Rgba8((byte)(thumb >> 16), (byte)(thumb >> 8), (byte)thumb, 255);
        var barX = Size - (int)UiTheme.Dark.ScrollBarInset - (int)(UiTheme.Dark.ScrollBarWidth / 2f) - 1;

        Assert.Equal(Black, image[barX, 0]);
        Assert.Equal(thumbPixel, image[barX, (int)Radius]);
    }

    [Fact]
    public void A_draw_inside_a_rounded_box_is_recorded_with_its_rounded_clip()
    {
        var frames = new UiFrames<DefaultUiState>(new DefaultUiState(), width: Size, height: Size, body: ui =>
            Clipping(ui, Radius, () => ui.Leaf(new Box { Width = 10f, Height = 10f, Background = Red })));
        frames.Frame();

        var drawn = frames.Canvas.Rects.Single(r => r.Inputs.Style.BackgroundColor == Red);

        Assert.Equal(new RoundedClip(new RectF(0f, 0f, Size, Size), Radius), drawn.Round);
    }
}
