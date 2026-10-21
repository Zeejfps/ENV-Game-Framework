using System.Diagnostics;
using Xunit.Abstractions;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.App.Tests;

public sealed class CodeViewFrameCostTests(ITestOutputHelper output)
{
    private const int Lines = 100_000;
    private const int WarmUpFrames = 60;
    private const int MeasuredFrames = 300;
    private const float Height = 900f;

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void A_100k_line_document_costs_only_its_visible_lines_per_frame(bool scrolling, bool folded)
    {
        var view = new CodeViewDriver(folded ? Folded(Lines) : CodeViewDriver.Numbered(Lines), Height);
        if (folded)
        {
            view.Run();
            for (var f = 0; f < view.Document.Folds.Length; f += 2)
                view.State.SetCollapsed(f, true);
        }
        view.Frames.MovePointer(200f, 200f);
        var visibleRows = (int)MathF.Ceiling((Height - 2 * CodeViewDriver.Origin) / CodeViewDriver.Row) + 1;

        for (var i = 0; i < WarmUpFrames; i++)
            Frame(view, scrolling);

        var maxElements = 0;
        var totalElements = 0L;
        for (var i = 0; i < MeasuredFrames; i++)
        {
            Frame(view, scrolling);
            var elements = Elements(view);
            maxElements = Math.Max(maxElements, elements);
            totalElements += elements;
        }
        var glyphRuns = view.Frames.Canvas.GlyphRuns.Count;

        view.Frames.Canvas.Recording = false;
        for (var i = 0; i < WarmUpFrames; i++)
            Frame(view, scrolling);
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        var clock = Stopwatch.StartNew();
        for (var i = 0; i < MeasuredFrames; i++)
            Frame(view, scrolling);
        clock.Stop();
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;

        output.WriteLine($"{(scrolling ? "scrolling" : "static")}{(folded ? ", half the folds collapsed" : "")}: " +
                         $"{clock.Elapsed.TotalMilliseconds / MeasuredFrames:F3} ms/frame, {allocated / MeasuredFrames:N0} B/frame allocated, " +
                         $"{(double)totalElements / MeasuredFrames:F1} elements/frame (max {maxElements}), {glyphRuns} glyph runs, {visibleRows} rows in view");

        Assert.InRange(glyphRuns, visibleRows - 2, (folded ? 3 : 2) * visibleRows + 4);
        Assert.True(maxElements <= (folded ? 5 : 4) * visibleRows + 32, $"{maxElements} elements for {visibleRows} visible rows");
        Assert.Equal(0, allocated / MeasuredFrames);
    }

    private static DecompiledDocument Folded(int lines)
    {
        var builder = new DocumentBuilder(DecompilationLanguage.CSharp);
        for (var member = 0; member < lines / 10; member++)
        {
            builder.Write($"    public void Member{member}()", SpanKind.Plain);
            builder.WriteLine();
            builder.MarkFoldStart(isDefinition: true);
            builder.Write("    {", SpanKind.Plain);
            builder.WriteLine();
            for (var i = 0; i < 7; i++)
            {
                builder.Write($"        Call({member}, {i});", SpanKind.Plain);
                builder.WriteLine();
            }
            builder.Write("    }", SpanKind.Plain);
            builder.MarkFoldEnd();
            builder.WriteLine();
        }
        return builder.Build("folded");
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
