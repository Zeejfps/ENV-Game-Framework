using ZGF.Geometry;
using ZGF.Gui.Testing;

namespace ZGF.Gui.Immediate.Tests;

/// <summary>
/// A box that clips and has rounded corners cuts what is drawn inside it to those corners: its children, what
/// floats inside it keeping its clip, and a scroll's bar. A box with square corners cuts to its rect as before.
/// </summary>
public sealed class RoundedClipTests
{
    private const int Size = 60;
    private const float Radius = 20f;
    private const uint Red = 0xFFFF0000u;
    private static readonly (int, int, int) Black = (0, 0, 0);
    private static readonly (int, int, int) RedRgb = (255, 0, 0);

    private static byte[] Draw(Action<Ui<DefaultUiState>> body)
    {
        var frames = new RasterFrames<DefaultUiState>(new DefaultUiState(), body, Size, Size);
        frames.Frame();
        frames.Frame();
        frames.Frame();
        return frames.ToRgbaTopDown();
    }

    private static (int, int, int) PixelAt(byte[] rgba, int x, int y)
    {
        var i = (y * Size + x) * 4;
        return (rgba[i], rgba[i + 1], rgba[i + 2]);
    }

    private static void Clipping(Ui<DefaultUiState> ui, float? radius, Action inside)
    {
        using (ui.Box(new Box { Width = Size, Height = Size, Radius = radius, Clip = true }))
            inside();
    }

    [Fact]
    public void A_rounded_box_cuts_a_child_filling_it_to_its_corners()
    {
        var rgba = Draw(ui => Clipping(ui, Radius, () => ui.Leaf(new Box { Width = Size, Height = Size, Background = Red })));

        Assert.Equal(Black, PixelAt(rgba, 1, 1));
        Assert.Equal(Black, PixelAt(rgba, Size - 2, Size - 2));
        Assert.Equal(RedRgb, PixelAt(rgba, Size / 2, 1));
        Assert.Equal(RedRgb, PixelAt(rgba, Size / 2, Size / 2));
    }

    [Fact]
    public void A_square_box_cuts_a_child_only_to_its_rect()
    {
        var rgba = Draw(ui => Clipping(ui, 0f, () => ui.Leaf(new Box { Width = Size, Height = Size, Background = Red })));

        Assert.Equal(RedRgb, PixelAt(rgba, 1, 1));
        Assert.Equal(RedRgb, PixelAt(rgba, Size - 2, Size - 2));
    }

    [Fact]
    public void What_floats_inside_a_rounded_box_keeping_its_clip_is_cut_to_its_corners_too()
    {
        var rgba = Draw(ui => Clipping(ui, Radius, () =>
        {
            using (ui.Float(0f, 0f, 0f, 0f, new Box { Width = Size, Height = Size, Background = Red }, clipped: true))
            {
            }
        }));

        Assert.Equal(Black, PixelAt(rgba, 1, 1));
        Assert.Equal(RedRgb, PixelAt(rgba, Size / 2, Size / 2));
    }

    [Fact]
    public void A_rounded_clip_inside_another_cuts_to_its_own_corners()
    {
        var rgba = Draw(ui => Clipping(ui, 0f, () =>
        {
            using (ui.Box(new Box { Width = Size, Height = Size, Radius = Radius, Clip = true }))
                ui.Leaf(new Box { Width = Size, Height = Size, Background = Red });
        }));

        Assert.Equal(Black, PixelAt(rgba, 1, 1));
        Assert.Equal(RedRgb, PixelAt(rgba, Size / 2, Size / 2));
    }

    [Fact]
    public void A_scrolls_bar_inside_a_rounded_box_is_cut_where_it_meets_the_corner()
    {
        var rgba = Draw(ui => Clipping(ui, Radius, () =>
        {
            using (ui.Scroll("list", ScrollAxes.Vertical, new Box { Width = Sizing.Grow(), Height = Sizing.Grow() }))
            {
                for (var i = 0; i < 20; i++)
                    ui.Leaf(new Box { Width = 10f, Height = 40f });
            }
        }));
        var thumb = UiTheme.Dark.ScrollThumb;
        var thumbRgb = ((int)(thumb >> 16) & 0xFF, (int)(thumb >> 8) & 0xFF, (int)thumb & 0xFF);
        var barX = Size - (int)UiTheme.Dark.ScrollBarInset - (int)(UiTheme.Dark.ScrollBarWidth / 2f) - 1;

        Assert.Equal(Black, PixelAt(rgba, barX, 0));
        Assert.Equal(thumbRgb, PixelAt(rgba, barX, (int)Radius));
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
