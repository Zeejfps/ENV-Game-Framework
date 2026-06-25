using System.Linq;
using ZGF.Gui.Testing;
using ZGF.Gui.Widgets;

namespace ZGF.Gui.Tests;

/// <summary>Render-only opacity/translation: the view pushes them onto the canvas around its own
/// and its descendants' draws, composing (multiply / add) when nested, without moving layout.</summary>
public class OpacityTranslationTests
{
    [Fact]
    public void Opacity_IsAppliedToDrawnRect()
    {
        using var harness = GuiTestHarness.Create(
            ctx => new Box { Background = 0xFF112233u, Opacity = 0.5f }.BuildView(ctx),
            width: 100,
            height: 100);

        var canvas = harness.Render();

        var rect = canvas.Rects.Single(r => r.Inputs.Style.BackgroundColor == 0xFF112233u);
        Assert.Equal(0.5f, rect.Opacity);
    }

    [Fact]
    public void Translation_IsRecorded_WithoutMovingLayoutPosition()
    {
        using var harness = GuiTestHarness.Create(
            ctx => new Box { Background = 0xFF112233u, TranslationY = 7f }.BuildView(ctx),
            width: 100,
            height: 100);

        var canvas = harness.Render();

        var rect = canvas.Rects.Single(r => r.Inputs.Style.BackgroundColor == 0xFF112233u);
        Assert.Equal(7f, rect.TranslationY);
        // Render-only: the layout Position is untouched — the offset rides on the draw command.
        Assert.Equal(0f, rect.Inputs.Position.Bottom);
    }

    [Fact]
    public void NestedOpacity_Multiplies()
    {
        using var harness = GuiTestHarness.Create(
            ctx => new Box
            {
                Background = 0xFFAAAAAAu,
                Opacity = 0.5f,
                Children = [new Box { Background = 0xFF112233u, Opacity = 0.5f }],
            }.BuildView(ctx),
            width: 100,
            height: 100);

        var canvas = harness.Render();

        var inner = canvas.Rects.Single(r => r.Inputs.Style.BackgroundColor == 0xFF112233u);
        Assert.True(Math.Abs(inner.Opacity - 0.25f) < 0.001f, $"expected ~0.25 but was {inner.Opacity}");
    }

    [Fact]
    public void FullyOpaqueAndUntranslated_RecordsIdentity()
    {
        using var harness = GuiTestHarness.Create(
            ctx => new Box { Background = 0xFF112233u }.BuildView(ctx),
            width: 100,
            height: 100);

        var canvas = harness.Render();

        var rect = canvas.Rects.Single(r => r.Inputs.Style.BackgroundColor == 0xFF112233u);
        Assert.Equal(1f, rect.Opacity);
        Assert.Equal(0f, rect.TranslationX);
        Assert.Equal(0f, rect.TranslationY);
        Assert.Equal(1f, rect.ScaleX);
        Assert.Equal(1f, rect.ScaleY);
    }

    [Fact]
    public void Scale_IsRecorded_WithoutMovingLayoutPosition()
    {
        using var harness = GuiTestHarness.Create(
            ctx => new Box { Background = 0xFF112233u, ScaleX = 2f, ScaleY = 3f }.BuildView(ctx),
            width: 100,
            height: 100);

        var canvas = harness.Render();

        var rect = canvas.Rects.Single(r => r.Inputs.Style.BackgroundColor == 0xFF112233u);
        Assert.Equal(2f, rect.ScaleX);
        Assert.Equal(3f, rect.ScaleY);
        // Render-only: the layout Position is untouched — the scale rides on the draw command.
        Assert.Equal(0f, rect.Inputs.Position.Bottom);
        Assert.Equal(100f, rect.Inputs.Position.Width);
    }

    [Fact]
    public void NestedScale_Multiplies()
    {
        using var harness = GuiTestHarness.Create(
            ctx => new Box
            {
                Background = 0xFFAAAAAAu,
                ScaleX = 2f,
                Children = [new Box { Background = 0xFF112233u, ScaleX = 3f }],
            }.BuildView(ctx),
            width: 100,
            height: 100);

        var canvas = harness.Render();

        var inner = canvas.Rects.Single(r => r.Inputs.Style.BackgroundColor == 0xFF112233u);
        Assert.Equal(6f, inner.ScaleX);
    }
}
