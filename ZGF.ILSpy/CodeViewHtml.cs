using System.Globalization;
using System.Text;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>How copied code looks as HTML: the code view's colors and the font it draws code in, in
/// pixels.</summary>
internal readonly record struct CodeHtmlStyle(
    CodeViewPalette Palette, uint Background, uint ErrorText, string FontFamily, float FontSize, float LineHeight);

/// <summary>Code as HTML in the code view's colors, for pasting where formatting is kept: a div per
/// line, keeping its spaces, with each run of one color in a span.</summary>
internal static class CodeHtml
{
    public static string Between(DecompiledDocument document, TextPosition start, TextPosition end, CodeHtmlStyle style)
    {
        var lines = document.Lines;
        if (lines.Length == 0 || start >= end)
            return "";
        start = CodeText.Clamp(document, start);
        end = CodeText.Clamp(document, end);

        var html = new StringBuilder();
        var textColor = TextColorOf(document, style);
        AppendBlockStart(html, style, textColor);
        for (var line = start.Line; line <= end.Line; line++)
        {
            var from = line == start.Line ? start.Column : 0;
            var to = line == end.Line ? end.Column : lines[line].Text.Length;
            AppendLine(html, document, lines[line], from, to, style, textColor);
        }
        html.Append("</div>");
        return html.ToString();
    }

    private static uint TextColorOf(DecompiledDocument document, CodeHtmlStyle style)
    {
        if (document.IsError)
            return style.ErrorText;
        return style.Palette.Plain;
    }

    private static void AppendBlockStart(StringBuilder html, CodeHtmlStyle style, uint textColor)
    {
        var family = style.FontFamily.Replace("'", "");
        html.Append("<div style=\"color: ").Append(Hex(textColor))
            .Append("; background-color: ").Append(Hex(style.Background))
            .Append("; font-family: '").Append(Escape(family)).Append("', monospace")
            .Append("; font-size: ").Append(Pixels(style.FontSize))
            .Append("; line-height: ").Append(Pixels(style.LineHeight))
            .Append("; white-space: pre;\">");
    }

    // An empty line holds a break, so it keeps its height where it is pasted.
    private static void AppendLine(StringBuilder html, DecompiledDocument document, DocumentLine line, int from, int to,
        CodeHtmlStyle style, uint textColor)
    {
        html.Append("<div>");
        if (from >= to)
            html.Append("<br>");
        else
            AppendRuns(html, document, line, from, to, style, textColor);
        html.Append("</div>");
    }

    private static void AppendRuns(StringBuilder html, DecompiledDocument document, DocumentLine line, int from, int to,
        CodeHtmlStyle style, uint textColor)
    {
        var current = textColor;
        foreach (var span in line.Spans)
        {
            var runStart = Math.Max(span.Start, from);
            var runEnd = Math.Min(span.End, to);
            if (runStart >= runEnd)
                continue;
            var color = ColorOf(document, span.Kind, style);
            SwitchColor(html, current, color, textColor);
            current = color;
            html.Append(Escape(line.Text.Substring(runStart, runEnd - runStart)));
        }
        SwitchColor(html, current, textColor, textColor);
    }

    private static uint ColorOf(DecompiledDocument document, SpanKind kind, CodeHtmlStyle style)
    {
        if (document.IsError)
            return style.ErrorText;
        return style.Palette[kind];
    }

    // Text in the block's own color needs no span of its own.
    private static void SwitchColor(StringBuilder html, uint current, uint next, uint textColor)
    {
        if (next == current)
            return;
        if (current != textColor)
            html.Append("</span>");
        if (next != textColor)
            html.Append("<span style=\"color: ").Append(Hex(next)).Append(";\">");
    }

    private static string Hex(uint color) => "#" + (color & 0xFFFFFF).ToString("x6", CultureInfo.InvariantCulture);

    private static string Pixels(float size) => size.ToString("0.##", CultureInfo.InvariantCulture) + "px";

    private static string Escape(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}
