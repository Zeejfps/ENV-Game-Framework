namespace ZGF.Gui;

/// <summary>
/// One styled run of rich text, the input unit of <see cref="RichTextLayout"/>.
/// <para>
/// <paramref name="Style"/> is the complete measure and draw style: every slice of the run is
/// measured and drawn with it. Give each distinct look its own style instance; one instance
/// changed between runs would alias, as the canvas keeps the style it is handed.
/// </para>
/// <para>
/// The flags decorate the run without changing its metrics: <paramref name="IsCode"/> draws a code
/// chip behind its segments, <paramref name="Underline"/> a rule under them and
/// <paramref name="Strikethrough"/> one through them, in the run's text color.
/// </para>
/// <para>A '\n' in <paramref name="Text"/> forces a line break there.</para>
/// </summary>
public sealed record RichTextRun(
    string Text,
    TextStyle Style,
    bool IsCode = false,
    bool Underline = false,
    bool Strikethrough = false);
