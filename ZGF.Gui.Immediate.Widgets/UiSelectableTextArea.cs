using ZGF.Desktop;

namespace ZGF.Gui.Immediate.Widgets;

public static partial class UiWidgets
{
    private sealed class TextAreaLines
    {
        public string? Text;
        public string[] Lines = [];
        public int[] Starts = [];
        public float Widest = -1f;
    }

    /// <summary>
    /// Shows <paramref name="text"/> read-only a line at a time, each <paramref name="lineHeight"/>
    /// tall, in the element <paramref name="i"/> interacts with, from the top of its content: the
    /// multi-line form of <see cref="SelectableText"/>, selecting across lines the same way and
    /// scrolling them sideways together by <paramref name="shift"/> to follow the caret. As there,
    /// only an <paramref name="active"/> text reads or moves the selection and shift it may share.
    /// </summary>
    public static void SelectableTextArea(this Ui ui, Interaction i, string text, TextSelection selection, ref float shift, bool active, float lineHeight,
        uint? color = null, float? size = null, string? family = null, TextOverflow overflow = TextOverflow.Ellipsis)
    {
        var theme = ui.Theme;
        var lines = ui.GetState<TextAreaLines>(i.Id);
        if (!ReferenceEquals(lines.Text, text))
            Split(lines, text);

        if (!active)
        {
            using (ui.Column(new Box { Width = Sizing.Grow(), Gap = 0f }))
            {
                foreach (var line in lines.Lines)
                {
                    using (ui.Row(new Box { Width = Sizing.Grow(), Height = lineHeight, Gap = 0f, AlignY = Align.Center }))
                    {
                        if (line.Length > 0)
                            ui.Text(line, new Box { Width = Sizing.Fit() }, color: color, size: size, family: family, overflow: overflow);
                    }
                }
            }
            return;
        }

        if (lines.Widest < 0f)
            lines.Widest = lines.Lines.Max(l => ui.MeasureText(l, size, family: family));
        var hasRect = ui.TryGetContentRect(i.Id, out var rect);
        if (selection.End > text.Length)
            selection.Select(0, 0);
        if (hasRect && (i.Pressed || i.Held))
        {
            var pointer = ui.Input.Pointer;
            var line = Math.Clamp((int)MathF.Floor((pointer.Y - rect.Y) / lineHeight), 0, lines.Lines.Length - 1);
            var at = lines.Starts[line] + ui.CaretAt(lines.Lines[line], pointer.X - rect.X + shift, size, family);
            if (i.Pressed)
                selection.Press(text, at, i.ClickCount, (ui.Input.Modifiers & KeyModifiers.Shift) != 0);
            else
                selection.Drag(text, at);
            if (i.Held && !i.Pressed && (pointer.X < rect.X || pointer.X > rect.Right))
                ui.RequestFrame();
        }

        if (hasRect)
        {
            var caretLine = LineOf(lines, selection.Caret);
            var caretX = ui.MeasureText(lines.Lines[caretLine], selection.Caret - lines.Starts[caretLine], size, family: family);
            if (caretX - shift > rect.Width) shift = caretX - rect.Width;
            if (caretX < shift) shift = caretX;
            shift = MathF.Max(0f, MathF.Min(shift, lines.Widest - rect.Width));
        }

        var plain = !selection.HasSelection && shift == 0f;
        var newline = ui.MeasureText(" ", size, family: family);
        using (ui.Column(new Box { Width = Sizing.Grow(), Gap = 0f }))
        {
            for (var l = 0; l < lines.Lines.Length; l++)
            {
                var line = lines.Lines[l];
                var start = lines.Starts[l];
                using (ui.Row(new Box { Width = Sizing.Grow(), Height = lineHeight, Gap = 0f, AlignY = Align.Center, Clip = !plain, ScrollX = plain ? 0f : shift }))
                {
                    var from = Math.Max(selection.Start, start);
                    var to = Math.Min(selection.End, start + line.Length);
                    if (selection.HasSelection && from <= to && (from < to || selection.End > start + line.Length))
                    {
                        var left = ui.MeasureText(line, from - start, size, family: family) - shift;
                        var right = ui.MeasureText(line, to - start, size, family: family) - shift + (selection.End > start + line.Length ? newline : 0f);
                        ui.Paint((left, right, theme.TextSelection), static (p, s) =>
                        {
                            var r = p.Rect;
                            var x0 = MathF.Max(r.X + s.left, r.X);
                            var x1 = MathF.Min(r.X + s.right, r.Right);
                            if (x1 > x0)
                                p.FillRect(new UiRect(x0, r.Y, x1 - x0, r.Height), s.TextSelection);
                        });
                    }
                    if (line.Length == 0)
                        continue;
                    if (plain)
                        ui.Text(line, new Box { Width = Sizing.Fit() }, color: color, size: size, family: family, overflow: overflow);
                    else
                        ui.Text(line, color: color, size: size, family: family);
                }
            }
        }
    }

    private static void Split(TextAreaLines lines, string text)
    {
        var texts = new List<string>();
        var starts = new List<int>();
        var start = 0;
        while (true)
        {
            var end = text.IndexOf('\n', start);
            var stop = end < 0 ? text.Length : end;
            texts.Add(text[start..(stop > start && text[stop - 1] == '\r' ? stop - 1 : stop)]);
            starts.Add(start);
            if (end < 0) break;
            start = end + 1;
        }
        lines.Text = text;
        lines.Lines = texts.ToArray();
        lines.Starts = starts.ToArray();
        lines.Widest = -1f;
    }

    private static int LineOf(TextAreaLines lines, int index)
    {
        var found = Array.BinarySearch(lines.Starts, index);
        return found >= 0 ? found : ~found - 1;
    }
}
