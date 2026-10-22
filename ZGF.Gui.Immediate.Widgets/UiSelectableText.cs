using ZGF.Desktop;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>A caret and the anchor a selection runs to from it in one line of text, and how the
/// pointer extends it while held: by character, word or the whole text after a single, double or
/// triple press.</summary>
public sealed class TextSelection
{
    private int _clicks;
    private int _unitStart;
    private int _unitEnd;

    public int Caret { get; private set; }
    public int Anchor { get; private set; }

    public bool HasSelection => Caret != Anchor;
    public int Start => Math.Min(Caret, Anchor);
    public int End => Math.Max(Caret, Anchor);

    public void Select(int anchor, int caret)
    {
        Anchor = anchor;
        Caret = caret;
    }

    /// <summary>The selected part of <paramref name="text"/>.</summary>
    public string Of(string text) => text.Substring(Start, End - Start);

    /// <summary>A press at <paramref name="at"/>: places the caret, or with <paramref name="extend"/>
    /// moves it keeping the anchor; the second press of a double-click selects the word there, the
    /// third the whole text.</summary>
    public void Press(string text, int at, int clickCount, bool extend)
    {
        _clicks = (clickCount - 1) % 3 + 1;
        switch (_clicks)
        {
            case 1 when extend:
                Select(Anchor, at);
                _unitStart = _unitEnd = Anchor;
                break;
            case 1:
                Select(at, at);
                _unitStart = _unitEnd = at;
                break;
            case 2:
                (_unitStart, _unitEnd) = UiWidgets.WordAt(text, at);
                Select(_unitStart, _unitEnd);
                break;
            default:
                (_unitStart, _unitEnd) = (0, text.Length);
                Select(0, text.Length);
                break;
        }
    }

    /// <summary>The held pointer moved over <paramref name="at"/>: extends the selection by the
    /// unit the press chose.</summary>
    public void Drag(string text, int at)
    {
        switch (_clicks)
        {
            case 1:
                Select(Anchor, at);
                break;
            case 2:
                var (start, end) = UiWidgets.WordAt(text, at);
                if (start < _unitStart)
                    Select(_unitEnd, start);
                else
                    Select(_unitStart, Math.Max(end, _unitEnd));
                break;
        }
    }
}

public static partial class UiWidgets
{
    /// <summary>
    /// Shows <paramref name="text"/> on one line, read-only, in the element <paramref name="i"/>
    /// interacts with, where it is the first content: the pointer selects part of it as in a
    /// <see cref="TextField"/> — dragging, double-clicking a word, triple-clicking all of it,
    /// shift-clicking. Only an <paramref name="active"/> text presses and drags move
    /// <paramref name="selection"/>, which it shows, scrolled by <paramref name="shift"/> to follow
    /// the caret past its edge; otherwise, or while nothing is selected and it is not scrolled, it
    /// is shortened by <paramref name="overflow"/>.
    /// </summary>
    public static void SelectableText(this Ui ui, Interaction i, string text, TextSelection selection, ref float shift, bool active, uint? color = null,
        float? size = null, string? family = null, TextOverflow overflow = TextOverflow.Ellipsis)
    {
        var theme = ui.Theme;
        var hasRect = ui.TryGetContentRect(i.Id, out var rect);
        if (!active)
            shift = 0f;
        else if (hasRect && (i.Pressed || i.Held))
        {
            var at = ui.CaretAt(text, ui.Input.Pointer.X - rect.X + shift, size, family);
            if (i.Pressed)
                selection.Press(text, at, i.ClickCount, (ui.Input.Modifiers & KeyModifiers.Shift) != 0);
            else
                selection.Drag(text, at);
            var pointerX = ui.Input.Pointer.X;
            if (i.Held && !i.Pressed && (pointerX < rect.X || pointerX > rect.Right))
                ui.RequestFrame();
        }

        var width = ui.MeasureText(text, size, family: family);
        if (active && hasRect)
        {
            var caretX = ui.MeasureText(text, selection.Caret, size, family: family);
            if (caretX - shift > rect.Width) shift = caretX - rect.Width;
            if (caretX < shift) shift = caretX;
            shift = MathF.Max(0f, MathF.Min(shift, width - rect.Width));
        }

        if (!active || !selection.HasSelection && shift == 0f)
        {
            ui.Text(text, new Box { Width = Sizing.Fit() }, color: color, size: size, family: family, overflow: overflow);
            return;
        }

        var from = ui.MeasureText(text, selection.Start, size, family: family) - shift;
        var to = ui.MeasureText(text, selection.End, size, family: family) - shift;
        using (ui.Row(new Box { Width = Sizing.Grow(0f, width), Gap = 0f, Clip = true, ScrollX = shift }))
        {
            if (selection.HasSelection)
                ui.Paint((from, to, theme.Selection), static (p, s) =>
                {
                    var r = p.Rect;
                    var left = MathF.Max(r.X + s.from, r.X);
                    var right = MathF.Min(r.X + s.to, r.Right);
                    if (right > left)
                        p.FillRect(new UiRect(left, r.Y, right - left, r.Height), s.Selection);
                });
            ui.Text(text, color: color, size: size, family: family);
        }
    }

    /// <summary>The caret index in <paramref name="text"/> nearest <paramref name="x"/> from its
    /// start, as <see cref="Ui.MeasureText(string, int, float?, bool, string?)"/> places it.</summary>
    public static int CaretAt(this Ui ui, string text, float x, float? size = null, string? family = null)
    {
        var low = 0;
        var high = text.Length;
        while (low < high)
        {
            var middle = (low + high + 1) / 2;
            if (ui.MeasureText(text, middle, size, family: family) <= x)
                low = middle;
            else
                high = middle - 1;
        }
        if (low < text.Length)
        {
            var next = low + StepForward(text, low);
            if (ui.MeasureText(text, next, size, family: family) - x < x - ui.MeasureText(text, low, size, family: family))
                low = next;
        }
        if (low > 0 && low < text.Length && char.IsLowSurrogate(text[low]))
            low--;
        return low;
    }
}
