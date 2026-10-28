namespace ZGF.Gui;

/// <summary>
/// One positioned slice of a run on a laid-out line. <paramref name="Start"/> and
/// <paramref name="Length"/> index the run's <see cref="RichTextRun.Text"/>; <paramref name="X"/>
/// is the slice's left edge from the line's start, and <paramref name="Width"/> its width in the
/// run's style. Slices tile the run text as <see cref="TextWrapper.WrapRanges"/> ranges tile
/// theirs: spaces at a soft break stay on the slice they follow, and a '\n' is in no slice.
/// </summary>
public readonly record struct RichTextSegment(int RunIndex, int Start, int Length, float X, float Width);

/// <summary>
/// One line: its slices left to right, its width, and its height — the tallest line height among
/// the styles on it. A line left empty by consecutive or trailing newlines has no slices and the
/// height of the run whose '\n' made it. <paramref name="Start"/> and <paramref name="End"/> bound
/// the line in the runs' joined text (see <see cref="RichTextLayout.ConcatenateRuns"/>): its slices
/// tile them, and a '\n' that ends the line is past <paramref name="End"/>.
/// </summary>
public sealed record RichTextLine(IReadOnlyList<RichTextSegment> Segments, float Width, float Height, int Start, int End);

/// <summary>Laid-out rich text: lines top to bottom, their total height and the widest line's width.</summary>
public sealed record RichTextLayoutResult(IReadOnlyList<RichTextLine> Lines, float Height, float MaxLineWidth)
{
    public static RichTextLayoutResult Empty { get; } = new(Array.Empty<RichTextLine>(), 0f, 0f);
}

/// <summary>
/// Wraps styled runs into lines, measuring each slice in its own run's <see cref="TextStyle"/>.
/// <para>
/// It breaks where <see cref="TextWrapper"/> does, and for a single style splits lines exactly as
/// <see cref="TextWrapper.WrapRanges"/>: at spaces and after <c>/ \ - _ . :</c>, between CJK code
/// points, never against kinsoku, and between code points inside a chunk wider than the line. A
/// run boundary is not a break by itself: runs break at their seam only where their joined text
/// would. A non-positive <c>maxWidth</c> does not wrap; '\n' still breaks. Runs without text lay
/// out to no lines.
/// </para>
/// </summary>
/// <remarks>Adapted from GitBench's markdown renderer.</remarks>
public static class RichTextLayout
{
    /// <summary>Lays <paramref name="runs"/> out into lines no wider than
    /// <paramref name="maxWidth"/>, measuring through <paramref name="canvas"/>.</summary>
    public static RichTextLayoutResult Layout(ICanvas canvas, IReadOnlyList<RichTextRun> runs, float maxWidth)
    {
        if (!TryCreateMap(canvas, runs, out var map))
            return RichTextLayoutResult.Empty;

        var ranges = new List<(int Start, int End)>();
        var lineStart = 0;
        var total = map.Text.Length;
        for (var i = 0; i <= total; i++)
        {
            if (i != total && map.Text[i] != '\n')
                continue;
            WrapForcedLine(in map, lineStart, i, maxWidth, ranges);
            lineStart = i + 1;
        }

        var lines = new RichTextLine[ranges.Count];
        var height = 0f;
        var maxLineWidth = 0f;
        for (var i = 0; i < ranges.Count; i++)
        {
            var line = BuildLine(in map, ranges[i].Start, ranges[i].End);
            lines[i] = line;
            height += line.Height;
            maxLineWidth = MathF.Max(maxLineWidth, line.Width);
        }
        return new RichTextLayoutResult(lines, height, maxLineWidth);
    }

    /// <summary>The run whose slice is at <paramref name="x"/>, <paramref name="y"/> from the
    /// top-left of <paramref name="layout"/>, or -1 between or beyond its slices.</summary>
    public static int RunAt(RichTextLayoutResult layout, float x, float y)
    {
        var top = 0f;
        foreach (var line in layout.Lines)
        {
            var bottom = top + line.Height;
            if (y >= top && y < bottom)
                return RunOnLine(line, x);
            top = bottom;
        }
        return -1;
    }

    private static int RunOnLine(RichTextLine line, float x)
    {
        foreach (var segment in line.Segments)
        {
            if (x >= segment.X && x < segment.X + segment.Width)
                return segment.RunIndex;
        }
        return -1;
    }

    /// <summary>The caret index in the runs' joined text nearest <paramref name="x"/>,
    /// <paramref name="y"/> from the top-left of <paramref name="layout"/>, on a grapheme-cluster
    /// boundary: on the line at <paramref name="y"/>, or the first or last line above or below
    /// them; and its start or end left or right of it.</summary>
    public static int CharAt(ICanvas canvas, IReadOnlyList<RichTextRun> runs, RichTextLayoutResult layout, float x, float y)
    {
        if (layout.Lines.Count == 0)
            return 0;
        var line = LineAt(layout, y);
        if (x <= 0f)
            return line.Start;
        var index = line.Start;
        foreach (var segment in line.Segments)
        {
            if (x < segment.X + segment.Width)
                return index + CaretInSegment(canvas, runs[segment.RunIndex], segment, x - segment.X);
            index += segment.Length;
        }
        return line.End;
    }

    private static RichTextLine LineAt(RichTextLayoutResult layout, float y)
    {
        var top = 0f;
        foreach (var line in layout.Lines)
        {
            top += line.Height;
            if (y < top)
                return line;
        }
        return layout.Lines[^1];
    }

    /// <summary>The offset into <paramref name="segment"/> nearest <paramref name="x"/> from its left
    /// edge, on a grapheme-cluster boundary.</summary>
    private static int CaretInSegment(ICanvas canvas, RichTextRun run, RichTextSegment segment, float x)
    {
        var text = run.Text.AsSpan(segment.Start, segment.Length);
        var low = 0;
        var high = text.Length;
        while (low < high)
        {
            var middle = (low + high + 1) / 2;
            if (canvas.MeasureTextWidth(text[..middle], run.Style) <= x)
                low = middle;
            else
                high = middle - 1;
        }
        low = TextBoundaries.Snap(text, low);
        if (low >= text.Length)
            return low;
        var next = TextBoundaries.Next(text, low);
        var pastLow = x - canvas.MeasureTextWidth(text[..low], run.Style);
        var beforeNext = canvas.MeasureTextWidth(text[..next], run.Style) - x;
        return beforeNext < pastLow ? next : low;
    }

    /// <summary>How far from the start of <paramref name="line"/> the caret at
    /// <paramref name="index"/> of the runs' joined text sits; clamped to the line.</summary>
    public static float XOf(ICanvas canvas, IReadOnlyList<RichTextRun> runs, RichTextLine line, int index)
    {
        var start = line.Start;
        foreach (var segment in line.Segments)
        {
            if (index <= start + segment.Length)
            {
                var length = Math.Max(0, index - start);
                var run = runs[segment.RunIndex];
                return segment.X + canvas.MeasureTextWidth(run.Text.AsSpan(segment.Start, length), run.Style);
            }
            start += segment.Length;
        }
        return line.Width;
    }

    /// <summary>The width of the widest chunk of <paramref name="runs"/> that cannot break: the
    /// narrowest width <see cref="Layout"/> takes without splitting between code points.</summary>
    public static float MeasureWidestChunk(ICanvas canvas, IReadOnlyList<RichTextRun> runs)
    {
        if (!TryCreateMap(canvas, runs, out var map))
            return 0f;

        var text = map.Text;
        var widest = 0f;
        var i = 0;
        while (i < text.Length)
        {
            if (text[i] is ' ' or '\n')
            {
                i++;
                continue;
            }
            var chunkStart = i;
            i = ScanChunkEnd(text, i, text.Length);
            widest = MathF.Max(widest, map.MeasureRange(chunkStart, i));
        }
        return widest;
    }

    /// <summary>The runs' text joined, with each run's start in it in <paramref name="runStarts"/>
    /// and the total length last: a <see cref="RichTextSegment"/> begins at
    /// <c>runStarts[RunIndex] + Start</c>.</summary>
    public static string ConcatenateRuns(IReadOnlyList<RichTextRun> runs, out int[] runStarts)
    {
        runStarts = new int[runs.Count + 1];
        var total = 0;
        for (var i = 0; i < runs.Count; i++)
        {
            runStarts[i] = total;
            total += runs[i].Text.Length;
        }
        runStarts[runs.Count] = total;
        if (total == 0)
            return string.Empty;
        return string.Concat(runs.Select(static r => r.Text));
    }

    private static bool TryCreateMap(ICanvas canvas, IReadOnlyList<RichTextRun> runs, out RunMap map)
    {
        var text = ConcatenateRuns(runs, out var starts);
        if (text.Length == 0)
        {
            map = default;
            return false;
        }
        map = new RunMap(canvas, runs, starts, text);
        return true;
    }

    /// <summary>Greedy wrap of one line between forced breaks, as <see cref="TextWrapper"/> wraps,
    /// with every width measured in the style of the run it falls in.</summary>
    private static void WrapForcedLine(in RunMap map, int start, int end, float maxWidth, List<(int Start, int End)> output)
    {
        if (start >= end || maxWidth <= 0f || map.MeasureRange(start, end) <= maxWidth)
        {
            output.Add((start, end));
            return;
        }

        var text = map.Text;
        var lineStart = start;
        var lineWidth = 0f;
        var lineHasContent = false;
        var i = start;
        while (i < end)
        {
            var spacesStart = i;
            while (i < end && text[i] == ' ')
                i++;
            if (i >= end)
                break;

            var chunkStart = i;
            i = ScanChunkEnd(text, i, end);
            var chunkWidth = map.MeasureRange(chunkStart, i);
            var separator = map.MeasureRange(spacesStart, chunkStart);

            if (chunkWidth > maxWidth)
            {
                if (lineHasContent)
                {
                    output.Add((lineStart, chunkStart));
                    lineStart = chunkStart;
                    lineWidth = 0f;
                }
                else
                {
                    lineWidth += separator;
                }
                SplitChunk(in map, chunkStart, i, end, maxWidth, ref lineStart, ref lineWidth, output);
                lineHasContent = true;
            }
            else if (!lineHasContent)
            {
                lineWidth += separator + chunkWidth;
                lineHasContent = true;
            }
            else if (lineWidth + separator + chunkWidth <= maxWidth)
            {
                lineWidth += separator + chunkWidth;
            }
            else
            {
                output.Add((lineStart, chunkStart));
                lineStart = chunkStart;
                lineWidth = chunkWidth;
            }
        }
        output.Add((lineStart, end));
    }

    /// <summary>Breaks a chunk wider than the line between code points, kinsoku still honored.</summary>
    private static void SplitChunk(in RunMap map, int chunkStart, int chunkEnd, int end, float maxWidth, ref int lineStart, ref float lineWidth,
        List<(int Start, int End)> output)
    {
        var text = map.Text.AsSpan(0, end);
        var j = chunkStart;
        var before = -1;
        while (j < chunkEnd)
        {
            var current = TextWrapper.ReadCodePoint(text, j, out var length);
            var width = map.MeasureRange(j, j + length);
            if (j > lineStart && lineWidth + width > maxWidth && TextWrapper.BreakAllowedHere(before, current))
            {
                output.Add((lineStart, j));
                lineStart = j;
                lineWidth = 0f;
            }
            lineWidth += width;
            before = current;
            j += length;
        }
    }

    /// <summary>From <paramref name="start"/>, on a character that is not a space, to the end of
    /// the chunk that cannot break: up to a space, a '\n' or a break between code points.</summary>
    private static int ScanChunkEnd(string text, int start, int end)
    {
        var span = text.AsSpan(0, end);
        var previous = TextWrapper.ReadCodePoint(span, start, out var length);
        var i = start + length;
        while (i < end && text[i] != ' ' && text[i] != '\n')
        {
            var next = TextWrapper.ReadCodePoint(span, i, out var nextLength);
            if (TextWrapper.BreakAllowedBetween(previous, next))
                break;
            previous = next;
            i += nextLength;
        }
        return i;
    }

    /// <summary>The slices of the line between <paramref name="start"/> and <paramref name="end"/>
    /// of the joined text; empty runs give none.</summary>
    private static RichTextLine BuildLine(in RunMap map, int start, int end)
    {
        if (start >= end)
            return EmptyLine(in map, start);

        var segments = new List<RichTextSegment>(2);
        var x = 0f;
        var height = 0f;
        var k = map.RunAt(start);
        var g = start;
        while (g < end)
        {
            var runEnd = map.Starts[k + 1];
            if (runEnd <= g)
            {
                k++;
                continue;
            }
            var run = map.Runs[k];
            var sliceStart = g - map.Starts[k];
            var sliceLength = Math.Min(end, runEnd) - map.Starts[k] - sliceStart;
            var width = map.Canvas.MeasureTextWidth(run.Text.AsSpan(sliceStart, sliceLength), run.Style);
            segments.Add(new RichTextSegment(k, sliceStart, sliceLength, x, width));
            x += width;
            height = MathF.Max(height, map.Canvas.MeasureTextLineHeight(run.Style));
            g = Math.Min(end, runEnd);
            k++;
        }
        return new RichTextLine(segments, x, height, start, end);
    }

    /// <summary>A line a '\n' left empty, as tall as the run of the '\n' before it, or of the one
    /// ending it when it is the first line.</summary>
    private static RichTextLine EmptyLine(in RunMap map, int start)
    {
        var anchor = start > 0 ? start - 1 : start;
        var style = map.Runs[map.RunAt(anchor)].Style;
        return new RichTextLine(Array.Empty<RichTextSegment>(), 0f, map.Canvas.MeasureTextLineHeight(style), start, start);
    }

    /// <summary>The runs as one text for break decisions, measured slice by slice in each run's style.</summary>
    private readonly struct RunMap(ICanvas canvas, IReadOnlyList<RichTextRun> runs, int[] starts, string text)
    {
        public readonly ICanvas Canvas = canvas;
        public readonly IReadOnlyList<RichTextRun> Runs = runs;
        public readonly int[] Starts = starts;
        public readonly string Text = text;

        /// <summary>The run holding character <paramref name="index"/> of the joined text: the last
        /// to start at or before it, which skips empty runs as they hold no character.</summary>
        public int RunAt(int index)
        {
            var low = 0;
            var high = Runs.Count - 1;
            while (low < high)
            {
                var middle = low + (high - low + 1) / 2;
                if (Starts[middle] <= index)
                    low = middle;
                else
                    high = middle - 1;
            }
            return low;
        }

        /// <summary>The width of the joined text from <paramref name="start"/> to
        /// <paramref name="end"/>, each run's slice in its own style.</summary>
        public float MeasureRange(int start, int end)
        {
            var width = 0f;
            var k = start < end ? RunAt(start) : 0;
            var g = start;
            while (g < end)
            {
                var runEnd = Starts[k + 1];
                if (runEnd <= g)
                {
                    k++;
                    continue;
                }
                var run = Runs[k];
                var sliceStart = g - Starts[k];
                var sliceLength = Math.Min(end, runEnd) - Starts[k] - sliceStart;
                width += Canvas.MeasureTextWidth(run.Text.AsSpan(sliceStart, sliceLength), run.Style);
                g = Math.Min(end, runEnd);
                k++;
            }
            return width;
        }
    }
}
