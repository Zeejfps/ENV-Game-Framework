using ZGF.Desktop;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>A caret and the anchor a selection runs to from it in a <typeparamref name="TText"/>,
/// and how the pointer extends it while held: by position, word or block after a single, double or
/// triple press.</summary>
public abstract class TextSelection<TText, TPos> where TPos : struct, IComparable<TPos>
{
    private TPos _unitStart;
    private TPos _unitEnd;

    public TPos Caret { get; private set; }
    public TPos Anchor { get; private set; }

    /// <summary>The presses of the last click: 1, 2 or 3.</summary>
    public int Clicks { get; private set; }

    public bool HasSelection => Caret.CompareTo(Anchor) != 0;
    public TPos Start => IsCaretFirst ? Caret : Anchor;
    public TPos End => IsCaretFirst ? Anchor : Caret;

    private bool IsCaretFirst => Caret.CompareTo(Anchor) < 0;

    public void Select(TPos anchor, TPos caret)
    {
        Anchor = anchor;
        Caret = caret;
    }

    /// <summary>A press at <paramref name="at"/>: places the caret, or with <paramref name="extend"/>
    /// moves it keeping the anchor; the second press of a double-click selects the word there, the
    /// third the block.</summary>
    public void Press(TText text, TPos at, int clickCount, bool extend)
    {
        Clicks = (Math.Max(1, clickCount) - 1) % 3 + 1;
        if (Clicks == 1)
        {
            var anchor = extend ? Anchor : at;
            Select(anchor, at);
            return;
        }
        (_unitStart, _unitEnd) = UnitAt(text, at);
        Select(_unitStart, _unitEnd);
    }

    /// <summary>The held pointer moved over <paramref name="at"/>: extends the selection by the
    /// unit the press chose.</summary>
    public void Drag(TText text, TPos at)
    {
        if (Clicks == 1)
        {
            Select(Anchor, at);
            return;
        }
        var (start, end) = UnitAt(text, at);
        if (start.CompareTo(_unitStart) < 0)
        {
            Select(_unitEnd, start);
            return;
        }
        var reach = end.CompareTo(_unitEnd) > 0 ? end : _unitEnd;
        Select(_unitStart, reach);
    }

    private (TPos Start, TPos End) UnitAt(TText text, TPos at) => Clicks == 2 ? WordAt(text, at) : BlockAt(text, at);

    protected abstract (TPos Start, TPos End) WordAt(TText text, TPos at);

    /// <summary>What a triple press at <paramref name="at"/> selects.</summary>
    protected abstract (TPos Start, TPos End) BlockAt(TText text, TPos at);
}

/// <summary>A <see cref="TextSelection{TText, TPos}"/> in a string, whose triple press selects
/// all of it.</summary>
public sealed class TextSelection : TextSelection<string, int>
{
    /// <summary>The selected part of <paramref name="text"/>.</summary>
    public string Of(string text) => text.Substring(Start, End - Start);

    protected override (int Start, int End) WordAt(string text, int at)
    {
        TextBoundaries.WordAt(text, at, out var start, out var end, splitPunctuation: true);
        return (start, end);
    }

    protected override (int Start, int End) BlockAt(string text, int at) => (0, text.Length);
}

public static partial class UiWidgets
{
    /// <summary>
    /// Shows <paramref name="text"/> on one line, read-only, in the element <paramref name="i"/>
    /// interacts with, where it is the first content: the pointer selects part of it as in a
    /// <see cref="TextField"/> — dragging, double-clicking a word, triple-clicking all of it,
    /// shift-clicking. Only an <paramref name="active"/> text reads or moves <paramref name="selection"/>
    /// and <paramref name="shift"/>, which it shows, scrolled to follow the caret past its edge; so
    /// texts can share them while one is active at a time. Otherwise, or while nothing is selected
    /// and it is not scrolled, it is shortened by <paramref name="overflow"/>.
    /// </summary>
    public static void SelectableText(this Ui ui, Interaction i, string text, TextSelection selection, ref float shift, bool active, uint? color = null,
        float? size = null, string? family = null, TextOverflow overflow = TextOverflow.Ellipsis)
    {
        if (!active)
        {
            ui.Text(text, new Box { Width = Sizing.Fit() }, color: color, size: size, family: family, overflow: overflow);
            return;
        }

        var hasRect = ui.TryGetContentRect(i.Id, out var rect);
        if (selection.End > text.Length)
            selection.Select(0, 0);
        if (hasRect && (i.Pressed || i.Held))
        {
            var at = ui.CaretAt(text, ui.Input.Pointer.X - rect.X + shift, size, family);
            SelectWithPointer(ui, i, selection, text, at, IsShiftHeld(ui), rect.X, rect.Right);
        }

        var width = ui.MeasureText(text, size, family: family);
        if (hasRect)
        {
            var caretX = ui.MeasureText(text, selection.Caret, size, family: family);
            shift = FollowCaret(shift, caretX, rect.Width, width);
        }

        if (!selection.HasSelection && shift == 0f)
        {
            ui.Text(text, new Box { Width = Sizing.Fit() }, color: color, size: size, family: family, overflow: overflow);
            return;
        }

        var from = ui.MeasureText(text, selection.Start, size, family: family) - shift;
        var to = ui.MeasureText(text, selection.End, size, family: family) - shift;
        using (ui.Row(new Box { Width = Sizing.Grow(0f, width), Gap = 0f, Clip = true, ScrollX = shift }))
        {
            if (selection.HasSelection)
                PaintSpan(ui, from, to, ui.Theme.TextSelection);
            ui.Text(text, color: color, size: size, family: family);
        }
    }

    /// <summary>The caret index in <paramref name="text"/> nearest <paramref name="x"/> from its
    /// start, as <see cref="Ui.MeasureText(string, int, float?, bool, string?)"/> places it, on a
    /// grapheme-cluster boundary.</summary>
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
        low = TextBoundaries.Snap(text, low);
        if (low >= text.Length)
            return low;
        var next = TextBoundaries.Next(text, low);
        var pastLow = x - ui.MeasureText(text, low, size, family: family);
        var beforeNext = ui.MeasureText(text, next, size, family: family) - x;
        return beforeNext < pastLow ? next : low;
    }

    private static bool IsShiftHeld(Ui ui) => (ui.Input.Modifiers & KeyModifiers.Shift) != 0;

    private static void SelectWithPointer(Ui ui, Interaction i, TextSelection selection, string text, int at, bool extend, float left, float right)
    {
        if (i.Pressed)
        {
            selection.Press(text, at, i.ClickCount, extend);
            return;
        }
        selection.Drag(text, at);
        var pointerX = ui.Input.Pointer.X;
        if (pointerX < left || pointerX > right)
            ui.RequestUpdate();
    }

    private static float FollowCaret(float shift, float caretX, float view, float content)
    {
        if (caretX - shift > view) shift = caretX - view;
        if (caretX < shift) shift = caretX;
        return MathF.Max(0f, MathF.Min(shift, content - view));
    }

    private static void PaintSpan(Ui ui, float from, float to, uint color, float? height = null, float drop = 0f) =>
        ui.Paint((from, to, color, height, drop), static (p, s) => FillSpan(p, s.from, s.to, s.color, s.height, s.drop));

    private static void FillSpan(UiPainter painter, float from, float to, uint color, float? height, float drop)
    {
        var rect = painter.Rect;
        var left = MathF.Max(rect.X + from, rect.X);
        var right = MathF.Min(rect.X + to, rect.Right);
        if (right <= left)
            return;
        var tall = height ?? rect.Height;
        painter.FillRect(new UiRect(left, rect.Y + (rect.Height - tall) / 2 + drop, right - left, tall), color);
    }
}
