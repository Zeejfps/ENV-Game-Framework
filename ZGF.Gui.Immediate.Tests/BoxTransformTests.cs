using ZGF.Gui.Testing;

namespace ZGF.Gui.Immediate.Tests;

/// <summary>
/// A box's <see cref="Box.Transform"/> moves and scales how it, its children and what floats in it
/// are drawn, and nothing else: its rect, its hit testing and the layout around it stay put.
/// </summary>
public sealed class BoxTransformTests
{
    private const uint Blue = 0xFF0000FF;
    private const uint Red = 0xFFFF0000;
    private const uint Green = 0xFF00FF00;
    private const float Tolerance = 1e-3f;
    private const string Card = "card";

    private static readonly UiTransform Lifted = new(offsetY: -6f, scale: 1.04f);

    private static UiFrames<DefaultUiState> FramesOf(Action<Ui<DefaultUiState>> body) =>
        new(new DefaultUiState(), body: body);

    private static void CardOf(Ui<DefaultUiState> ui, UiTransform? transform)
    {
        using (ui.Box(ui.GetId(Card), new Box { Width = 100f, Height = 50f, Background = Blue, Transform = transform }))
        {
            using (ui.Box(new Box { Width = 20f, Height = 10f, Background = Red }))
            {
            }
        }
    }

    /// <summary>Where a recorded rect lands on screen once its transform is applied, in UI points.</summary>
    private static UiRect DrawnAt(UiFrames<DefaultUiState> frames, RecordedRect rect)
    {
        var p = rect.Inputs.Position;
        var left = p.Left * rect.ScaleX + rect.TranslationX;
        var top = p.Top * rect.ScaleY + rect.TranslationY;
        return new UiRect(left, frames.Height - top, p.Width * rect.ScaleX, p.Height * rect.ScaleY);
    }

    private static RecordedRect Filled(UiFrames<DefaultUiState> frames, uint color) =>
        frames.Canvas.Rects.Single(r => r.Inputs.Style.BackgroundColor == color);

    private static void AssertNear(UiRect expected, UiRect actual)
    {
        Assert.Equal(expected.X, actual.X, Tolerance);
        Assert.Equal(expected.Y, actual.Y, Tolerance);
        Assert.Equal(expected.Width, actual.Width, Tolerance);
        Assert.Equal(expected.Height, actual.Height, Tolerance);
    }

    [Fact]
    public void A_transformed_box_is_drawn_moved_and_scaled_about_its_middle()
    {
        var frames = FramesOf(ui => CardOf(ui, Lifted));
        frames.Frame();

        // 100 by 50 at the origin, grown by 4% about its middle and lifted 6.
        AssertNear(new UiRect(-2f, -7f, 104f, 52f), DrawnAt(frames, Filled(frames, Blue)));
    }

    [Fact]
    public void Its_children_are_drawn_under_the_same_transform()
    {
        var frames = FramesOf(ui => CardOf(ui, Lifted));
        frames.Frame();

        // The child sits at the card's top-left, so it moves with that corner.
        AssertNear(new UiRect(-2f, -7f, 20.8f, 10.4f), DrawnAt(frames, Filled(frames, Red)));
    }

    [Fact]
    public void A_box_scales_about_the_origin_it_names()
    {
        var frames = FramesOf(ui => CardOf(ui, new UiTransform(scale: 2f, originX: 0f, originY: 0f)));
        frames.Frame();

        AssertNear(new UiRect(0f, 0f, 200f, 100f), DrawnAt(frames, Filled(frames, Blue)));
    }

    [Fact]
    public void Its_rect_and_hit_testing_stay_where_layout_put_it()
    {
        var far = new UiTransform(offsetX: 250f);
        var frames = FramesOf(ui => CardOf(ui, far));
        frames.Frame();

        Assert.Equal(new UiRect(0f, 0f, 100f, 50f), frames.RectOf(Card));
        var hovered = frames.MovePointer(50f, 25f).Frame(ui =>
        {
            var interaction = ui.Interact(ui.GetId(Card));
            CardOf(ui, far);
            return interaction.Hovered;
        });
        Assert.True(hovered);
    }

    [Fact]
    public void A_box_with_no_transform_or_the_identity_is_drawn_untransformed()
    {
        foreach (var transform in new UiTransform?[] { null, new UiTransform() })
        {
            var frames = FramesOf(ui => CardOf(ui, transform));
            frames.Frame();

            Assert.All(frames.Canvas.Rects, r =>
            {
                Assert.Equal((0f, 0f, 1f, 1f), (r.TranslationX, r.TranslationY, r.ScaleX, r.ScaleY));
            });
        }
    }

    [Fact]
    public void What_floats_in_a_transformed_box_moves_with_it()
    {
        var frames = FramesOf(ui =>
        {
            using (ui.Box(new Box { Width = 100f, Height = 50f, Background = Blue, Transform = Lifted }))
            {
                using (ui.Float(0f, 0f, box: new Box { Width = 100f, Height = 50f, Background = Green }, clipped: true))
                {
                }
            }
        });
        frames.Frame();

        AssertNear(DrawnAt(frames, Filled(frames, Blue)), DrawnAt(frames, Filled(frames, Green)));
    }

    [Fact]
    public void A_box_laid_out_out_of_view_is_drawn_when_its_transform_brings_it_into_view()
    {
        var frames = FramesOf(ui =>
        {
            using (ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f }))
            {
                ui.Leaf(new Box { Width = 10f, Height = 400f });
                using (ui.Box(new Box { Width = 100f, Height = 50f, Background = Green, Transform = new UiTransform(offsetY: -350f) }))
                {
                }
            }
        });
        frames.Frame();

        AssertNear(new UiRect(0f, 50f, 100f, 50f), DrawnAt(frames, Filled(frames, Green)));
    }

    [Fact]
    public void Transforms_nest()
    {
        var frames = FramesOf(ui =>
        {
            using (ui.Box(new Box { Width = 100f, Height = 50f, Background = Blue, Transform = new UiTransform(offsetX: 10f) }))
            {
                using (ui.Box(new Box { Width = 20f, Height = 10f, Background = Red, Transform = new UiTransform(scale: 2f, originX: 0f, originY: 0f) }))
                {
                }
            }
        });
        frames.Frame();

        AssertNear(new UiRect(10f, 0f, 40f, 20f), DrawnAt(frames, Filled(frames, Red)));
    }

    [Fact]
    public void A_transform_made_without_its_constructor_is_refused()
    {
        var frames = FramesOf(ui => CardOf(ui, default(UiTransform)));

        Assert.Throws<ArgumentOutOfRangeException>(() => frames.Frame());
    }

    [Fact]
    public void A_transform_cannot_scale_to_nothing()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UiTransform(scale: 0f));
    }
}
