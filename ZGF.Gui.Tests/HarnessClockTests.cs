using ZGF.Gui.Testing;
using ZGF.Gui.Widgets;

namespace ZGF.Gui.Tests;

/// <summary>Clock control (gap D): the harness drives <see cref="IFrameTicker"/> subscribers
/// deterministically.</summary>
public class HarnessClockTests
{
    [Fact]
    public void Tick_AndAdvance_DriveFrameTickerSubscribers()
    {
        var elapsed = 0f;
        using var harness = GuiTestHarness.Create(ctx =>
        {
            ctx.Require<IFrameTicker>().Add(dt => elapsed += dt);
            return new Box().BuildView(ctx);
        });

        harness.Tick(0.5f);
        Assert.Equal(0.5f, elapsed, 3);

        harness.Advance(1.0f);
        Assert.Equal(1.5f, elapsed, 3);
    }
}
