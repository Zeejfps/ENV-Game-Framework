using ZGF.Gui.Desktop.Widgets;
using ZGF.Gui.Desktop.Inspection;
using ZGF.Gui.Testing;
using ZGF.Gui.Widgets;

namespace ZGF.Gui.Tests;

/// <summary>End-to-end input routing through the harness: hit-test → dispatch (gap A), tree
/// query (gap B), and redraw notification.</summary>
public class HarnessSmokeTests
{
    [Fact]
    public void ClickOnId_RoutesThroughDispatch_IncrementsCounter_AndNotifiesRedraw()
    {
        var clicks = 0;
        using var harness = GuiTestHarness.Create(ctx =>
            new KbmInput
            {
                Id = "ok",
                OnClick = () => clicks++,
                Child = new Box { Background = 0xFF3366CCu },
            }.BuildView(ctx));

        harness.ClickOn("ok");

        Assert.Equal(1, clicks);
        Assert.True(harness.RedrawCount > 0);
    }

    [Fact]
    public void PressWithoutHover_DoesNotDispatch_ProvingRealRoutingPath()
    {
        var clicks = 0;
        using var harness = GuiTestHarness.Create(ctx =>
            new KbmInput
            {
                Id = "ok",
                OnClick = () => clicks++,
                Child = new Box(),
            }.BuildView(ctx));

        harness.Press();
        Assert.Equal(0, clicks);

        harness.ClickOn("ok");
        Assert.Equal(1, clicks);
    }
}
