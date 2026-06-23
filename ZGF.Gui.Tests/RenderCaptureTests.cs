using ZGF.Gui.Testing;
using ZGF.Gui.Widgets;

namespace ZGF.Gui.Tests;

/// <summary>Render capture (gap C): draws are recorded with their bounds, colour, and order.</summary>
public class RenderCaptureTests
{
    [Fact]
    public void Render_CapturesDrawnRect_WithBoundsAndColor()
    {
        const uint color = 0xFF112233u;
        using var harness = GuiTestHarness.Create(
            ctx => new Box { Background = color }.BuildView(ctx),
            width: 320,
            height: 240);

        var canvas = harness.Render();

        Assert.Contains(canvas.Rects, r =>
            r.Inputs.Style.BackgroundColor == color &&
            r.Inputs.Position.Width == 320f &&
            r.Inputs.Position.Height == 240f);
    }

    [Fact]
    public void Reset_ClearsCapturedCommands()
    {
        using var harness = GuiTestHarness.Create(
            ctx => new Box { Background = 0xFF445566u }.BuildView(ctx));

        var canvas = harness.Render();
        Assert.NotEmpty(canvas.Rects);

        canvas.Reset();
        Assert.Empty(canvas.Rects);
        Assert.Empty(canvas.InDrawOrder());
    }
}
