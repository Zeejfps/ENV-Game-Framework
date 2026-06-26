using PngSharp.Api;
using ZGF.Fonts;
using ZGF.Gui.Desktop.Widgets;
using ZGF.Gui.Testing;
using ZGF.Gui.Views;
using ZGF.Gui.Widgets;

namespace ZGF.Gui.Tests;

/// <summary>The LLM-facing debugging surface: snapshot (roles/labels/hover), act-by-label with
/// rich failures, diff, settle, and a real raster screenshot.</summary>
public class HarnessDebugTests
{
    private static IWidget Screen() =>
        new Box
        {
            Children =
            [
                new Text { Id = "title", Value = "Local Changes" },
                new KbmInput
                {
                    Id = "stage",
                    OnClick = () => { },
                    Child = new Box { Children = [new Text { Value = "Stage All" }] },
                },
            ],
        };

    [Fact]
    public void Snapshot_IncludesRoleAndAggregatedLabel()
    {
        using var h = GuiTestHarness.Create(ctx => Screen().BuildView(ctx), 300, 120);

        var text = h.Snapshot().ToText();

        Assert.Contains("role=button", text);
        Assert.Contains("\"Stage All\"", text);   // aggregated from the button's descendant text
        Assert.Contains("\"Local Changes\"", text);
    }

    [Fact]
    public void Snapshot_MarksHoveredView()
    {
        using var h = GuiTestHarness.Create(ctx => Screen().BuildView(ctx), 300, 120);

        var button = h.Get("stage");
        h.MoveTo(button.Position.Center.X, button.Position.Center.Y);

        Assert.Contains("hovered", h.Snapshot().ToText());
    }

    [Fact]
    public void Click_ByLabel_InvokesHandler()
    {
        var clicks = 0;
        using var h = GuiTestHarness.Create(ctx =>
            new KbmInput { OnClick = () => clicks++, Child = new Box { Children = [new Text { Value = "Push" }] } }
                .BuildView(ctx));

        h.Click("Push");

        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Click_UnknownLabel_ThrowsWithCandidatesAndSnapshot()
    {
        using var h = GuiTestHarness.Create(ctx =>
            new KbmInput { OnClick = () => { }, Child = new Box { Children = [new Text { Value = "Pull" }] } }
                .BuildView(ctx));

        var ex = Assert.Throws<InvalidOperationException>(() => h.Click("Push"));

        Assert.Contains("Pull", ex.Message);          // lists the available button
        Assert.Contains("snapshot", ex.Message);       // embeds the snapshot
    }

    [Fact]
    public void DiffTo_ReportsLabelChange()
    {
        using var h = GuiTestHarness.Create(ctx => Screen().BuildView(ctx), 300, 120);
        var before = h.Snapshot();

        ((TextView)h.Get("title")).Text = "Staged Changes";
        h.Layout();

        var diff = before.DiffTo(h.Snapshot());
        Assert.Contains("Staged Changes", diff);
    }

    [Fact]
    public void FrameTicker_ActiveCount_TracksRegistrations()
    {
        var ticker = new FrameTicker();
        Assert.Equal(0, ticker.ActiveCount);

        Action<float> tick = _ => { };
        ticker.Add(tick);
        Assert.Equal(1, ticker.ActiveCount);

        ticker.Remove(tick);
        Assert.Equal(0, ticker.ActiveCount);
    }

    [Fact]
    public void SaveScreenshot_WritesDecodablePng()
    {
        var fonts = new FreeTypeFontBackend();
        var font = fonts.LoadFontFromFile(
            Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf"), 16);

        using var h = GuiTestHarness.CreateRaster(
            ctx => new Box { Background = 0xFF202020u, Children = [new Text { Value = "Hello" }] }.BuildView(ctx),
            fonts, font, 200, 80);
        h.Settle();

        var path = Path.Combine(Path.GetTempPath(), "zgf-gui-tests", "shot.png");
        h.SaveScreenshot(path);

        var png = Png.DecodeFromFile(path);
        Assert.Equal(200, png.GetDimensions().Width);
        Assert.Equal(80, png.GetDimensions().Height);
    }
}
