using System.Text;

namespace ZGF.Gui;

/// <summary>
/// Greedy word-wrap that uses an <see cref="ICanvas"/> to measure widths in the supplied
/// <see cref="TextStyle"/>. Splits on spaces; words longer than the width are kept whole on
/// their own line (no character-level breaking).
/// </summary>
public static class TextWrapper
{
    /// <summary>
    /// Wraps <paramref name="text"/> into <paramref name="output"/>. Each newline in the input
    /// starts a new visual line. Lines that already fit are appended unmodified.
    /// </summary>
    public static void Wrap(ICanvas canvas, string text, TextStyle style, float maxWidth, List<string> output)
    {
        if (text.Length == 0)
        {
            output.Add(string.Empty);
            return;
        }

        var rawLines = text.Replace("\r\n", "\n").Split('\n');
        foreach (var raw in rawLines)
        {
            WrapLine(canvas, raw, style, maxWidth, output);
        }
    }

    private static void WrapLine(ICanvas canvas, string raw, TextStyle style, float maxWidth, List<string> output)
    {
        if (raw.Length == 0)
        {
            output.Add(string.Empty);
            return;
        }
        if (maxWidth <= 0f || canvas.MeasureTextWidth(raw, style) <= maxWidth)
        {
            output.Add(raw);
            return;
        }

        var sb = new StringBuilder();
        var i = 0;
        while (i < raw.Length)
        {
            if (sb.Length == 0)
            {
                while (i < raw.Length && raw[i] == ' ') i++;
                if (i >= raw.Length) break;
            }

            var wordStart = i;
            while (i < raw.Length && raw[i] != ' ') i++;
            var word = raw.AsSpan(wordStart, i - wordStart);

            if (sb.Length == 0)
            {
                sb.Append(word);
            }
            else
            {
                var candidate = sb.ToString() + " " + word.ToString();
                if (canvas.MeasureTextWidth(candidate, style) <= maxWidth)
                {
                    sb.Append(' ').Append(word);
                }
                else
                {
                    output.Add(sb.ToString());
                    sb.Clear();
                    sb.Append(word);
                }
            }

            if (i < raw.Length && raw[i] == ' ') i++;
        }

        if (sb.Length > 0) output.Add(sb.ToString());
    }
}
