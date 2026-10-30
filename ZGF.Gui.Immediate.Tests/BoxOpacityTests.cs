using ZGF.Gui.Testing;

namespace ZGF.Gui.Immediate.Tests;

/// <summary>
/// A box's <see cref="Box.Opacity"/> fades how it, its children and what floats in it are drawn, and
/// nothing else: its rect and its hit testing stay as they are.
/// </summary>
public sealed class BoxOpacityTests
{
    private const uint Blue = 0xFF0000FF;
    private const uint Red = 0xFFFF0000;
    private const uint Green = 0xFF00FF00;
    private const float Tolerance = 1e-4f;
    private const string Card = "card";

    private static UiFrames<DefaultUiState> FramesOf(Action<Ui<DefaultUiState>> body) =>
        new(new DefaultUiState(), body: body);

    private static RecordedRect Filled(UiFrames<DefaultUiState> frames, uint color) =>
        frames.Canvas.Rects.Single(r => r.Inputs.Style.BackgroundColor == color);

    private static void CardOf(Ui<DefaultUiState> ui, float? opacity)
    {
        using (ui.Box(ui.GetId(Card), new Box { Width = 100f, Height = 50f, Background = Blue, Opacity = opacity }))
        {
            using (ui.Box(new Box { Width = 20f, Height = 10f, Background = Red }))
            {
            }
        }
    }

    [Fact]
    public void A_faded_box_and_its_children_are_drawn_at_its_opacity()
    {
        var frames = FramesOf(ui => CardOf(ui, 0.4f));
        frames.Frame();

        Assert.Equal(0.4f, Filled(frames, Blue).Opacity, Tolerance);
        Assert.Equal(0.4f, Filled(frames, Red).Opacity, Tolerance);
    }

    [Fact]
    public void A_box_with_no_opacity_is_drawn_as_it_is()
    {
        var frames = FramesOf(ui => CardOf(ui, null));
        frames.Frame();

        Assert.Equal(1f, Filled(frames, Blue).Opacity, Tolerance);
    }

    [Fact]
    public void What_follows_a_faded_box_is_drawn_as_it_is()
    {
        var frames = FramesOf(ui =>
        {
            CardOf(ui, 0.4f);
            using (ui.Box(new Box { Width = 10f, Height = 10f, Background = Green }))
            {
            }
        });
        frames.Frame();

        Assert.Equal(1f, Filled(frames, Green).Opacity, Tolerance);
    }

    [Fact]
    public void Opacities_inside_one_another_multiply()
    {
        var frames = FramesOf(ui =>
        {
            using (ui.Box(new Box { Width = 100f, Height = 50f, Opacity = 0.5f }))
                CardOf(ui, 0.4f);
        });
        frames.Frame();

        Assert.Equal(0.2f, Filled(frames, Red).Opacity, Tolerance);
    }

    [Fact]
    public void What_floats_in_a_faded_box_is_drawn_at_its_opacity_too()
    {
        var frames = FramesOf(ui =>
        {
            using (ui.Box(new Box { Width = 100f, Height = 50f, Opacity = 0.5f }))
            using (ui.Float(0f, 0f, 0f, 0f, new Box { Width = 10f, Height = 10f, Background = Green }))
            {
            }
            using (ui.Float(0f, 0f, 0f, 0f, new Box { Width = 10f, Height = 10f, Background = Red }))
            {
            }
        });
        frames.Frame();

        Assert.Equal(0.5f, Filled(frames, Green).Opacity, Tolerance);
        Assert.Equal(1f, Filled(frames, Red).Opacity, Tolerance);
    }

    [Fact]
    public void A_box_faded_out_entirely_is_still_hit_tested_where_it_is()
    {
        var clicked = false;
        var frames = FramesOf(ui =>
        {
            var id = ui.GetId(Card);
            clicked |= ui.Interact(id).Clicked;
            using (ui.Box(id, new Box { Width = 100f, Height = 50f, Background = Blue, Opacity = 0f }))
            {
            }
        });
        frames.Frame();

        frames.Click(50f, 25f);

        Assert.True(clicked);
    }
}
