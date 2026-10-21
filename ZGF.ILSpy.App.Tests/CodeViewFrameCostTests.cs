using System.Diagnostics;
using Xunit.Abstractions;

namespace ZGF.ILSpy.App.Tests;

public sealed class CodeViewFrameCostTests(ITestOutputHelper output)
{
    private const int Lines = 100_000;
    private const int WarmUpFrames = 60;
    private const int MeasuredFrames = 300;
    private const float Height = 900f;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_100k_line_document_costs_only_its_visible_lines_per_frame(bool scrolling)
    {
        var view = new CodeViewDriver(CodeViewDriver.Numbered(Lines), Height);
        view.Frames.MovePointer(200f, 200f);
        var visibleRows = (int)MathF.Ceiling((Height - 2 * CodeViewDriver.Origin) / CodeViewDriver.Row) + 1;

        for (var i = 0; i < WarmUpFrames; i++)
            Frame(view, scrolling);

        var maxElements = 0;
        var totalElements = 0L;
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        var clock = Stopwatch.StartNew();
        for (var i = 0; i < MeasuredFrames; i++)
        {
            Frame(view, scrolling);
            var elements = Elements(view);
            maxElements = Math.Max(maxElements, elements);
            totalElements += elements;
        }
        clock.Stop();
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;

        var glyphRuns = view.Frames.Canvas.GlyphRuns.Count;
        output.WriteLine($"{(scrolling ? "scrolling" : "static")}: {clock.Elapsed.TotalMilliseconds / MeasuredFrames:F3} ms/frame, " +
                         $"{allocated / MeasuredFrames:N0} B/frame allocated (recording canvas included), " +
                         $"{(double)totalElements / MeasuredFrames:F1} elements/frame (max {maxElements}), {glyphRuns} glyph runs, {visibleRows} rows in view");

        Assert.InRange(glyphRuns, visibleRows - 2, 2 * visibleRows + 4);
        Assert.True(maxElements <= 4 * visibleRows + 32, $"{maxElements} elements for {visibleRows} visible rows");
    }

    private static void Frame(CodeViewDriver view, bool scrolling)
    {
        if (scrolling)
            view.Frames.Wheel(-3);
        view.Run();
    }

    private static int Elements(CodeViewDriver view)
    {
        var canvas = view.Frames.Canvas;
        return canvas.Rects.Count + canvas.Texts.Count + canvas.GlyphRuns.Count + canvas.Lines.Count + canvas.Images.Count +
               canvas.BoxShadows.Count + canvas.Circles.Count + canvas.Beziers.Count + canvas.CubicBeziers.Count;
    }
}
