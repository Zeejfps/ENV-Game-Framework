using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Testing;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy.App.Tests;

public sealed class CodeViewScrollTests
{
    private const int FitColumns = 37;

    private static DecompiledDocument Document(int widest) =>
        DecompiledDocument.FromText("t", string.Join('\n', Enumerable.Range(0, 1000).Select(i => i == 999 ? new string('w', widest) : $"line {i}")));

    private static List<(float Left, float Top, float Width, float Height)> Thumbs(CodeViewDriver view)
    {
        var thumb = UiTheme.Dark.ScrollThumb;
        var height = view.Frames.Height;
        return view.Frames.Canvas.Rects.Where(r => r.Inputs.Style.BackgroundColor == thumb)
            .Select(r => (r.Inputs.Position.Left, height - r.Inputs.Position.Bottom - r.Inputs.Position.Height, r.Inputs.Position.Width, r.Inputs.Position.Height))
            .ToList();
    }

    [Fact]
    public void The_last_line_scrolled_to_sits_above_the_horizontal_bar()
    {
        var view = new CodeViewDriver(Document(200));
        view.Run();
        view.Click(0, 0);

        view.Key(KeyboardKey.End);
        for (var i = 0; i < 4; i++)
            view.Run();

        var last = view.DrawnText().Single(d => d.Text.StartsWith("www"));
        var horizontal = Thumbs(view).Single(t => t.Width > t.Height);
        Assert.True(last.Top + CodeViewDriver.Row <= horizontal.Top, $"last line ends at {last.Top + CodeViewDriver.Row}, bar starts at {horizontal.Top}");
    }

    [Fact]
    public void A_line_as_wide_as_the_view_scrolls_clear_of_the_vertical_bar_and_the_bars_do_not_cross()
    {
        var view = new CodeViewDriver(Document(FitColumns));
        view.Run();
        view.Click(0, 0);

        view.Key(KeyboardKey.End);
        for (var i = 0; i < 20; i++)
            view.Key(KeyboardKey.RightArrow);
        view.Run();

        var thumbs = Thumbs(view);
        var vertical = thumbs.Single(t => t.Height > t.Width);
        var horizontal = thumbs.Single(t => t.Width > t.Height);
        var last = view.Frames.Canvas.GlyphRuns.Single(r => r.Text.StartsWith("www"));
        var right = last.Origin.X + last.Text.Length * CodeViewDriver.Cell;
        Assert.True(right <= vertical.Left, $"text ends at {right}, bar starts at {vertical.Left}");
        Assert.True(horizontal.Left + horizontal.Width <= vertical.Left);
    }

    [Fact]
    public void A_tab_left_for_over_a_thousand_frames_keeps_its_scroll()
    {
        var frames = new UiFrames();
        var a = new CodeViewState();
        var b = new CodeViewState();
        var documentA = CodeViewDriver.Numbered(1000);
        var documentB = CodeViewDriver.Numbered(1000, "row ");
        void Show(int tab, CodeViewState state, DecompiledDocument document) => frames.Run(1f / 60f, ui =>
        {
            using var screen = ui.Screen();
            using (ui.Id(tab))
                ui.CodeView("code", document, state);
        });

        Show(1, a, documentA);
        frames.MovePointer(200f, 150f);
        Show(1, a, documentA);
        frames.Wheel(-10);
        Show(1, a, documentA);
        Show(1, a, documentA);
        var scrolled = a.Position.TopLine;

        for (var i = 0; i < 1100; i++)
            Show(2, b, documentB);
        Show(1, a, documentA);
        Show(1, a, documentA);

        Assert.True(scrolled > 10);
        Assert.Equal(scrolled, a.Position.TopLine);
        Assert.Contains(frames.Canvas.GlyphRuns, r => r.Text == $"line {scrolled}");
        Assert.DoesNotContain(frames.Canvas.GlyphRuns, r => r.Text == "line 0");
    }
}
