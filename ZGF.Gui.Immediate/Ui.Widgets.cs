using ZGF.Desktop;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate;

public sealed partial class Ui
{
    private const float CheckboxSize = 18f;
    private const float FieldPaddingX = 8f;
    private const float MinFieldWidth = 60f;
    private const float SliderThumbRadius = 8f;

    private int _caret;
    private float _fieldShift;

    public void Label(string text, uint? color = null, float? size = null, bool bold = false) =>
        Leaf(0, default, text, Style(size, color ?? Theme.Text, bold, Align.Start));

    public void Heading(string text) => Label(text, size: MathF.Round(Theme.FontSize * 1.5f), bold: true);

    /// <summary>Returns true on the frame the button is clicked. A label may carry a hidden id
    /// suffix after <c>##</c>: <c>"Save##toolbar"</c>.</summary>
    public bool Button(string label, Sizing? width = null, bool enabled = true)
    {
        var id = GetId(label);
        var i = enabled ? Interact(id) : default;
        if (i.Hovered) SetCursor(MouseCursor.Hand);

        Leaf(id, new Box
        {
            Width = width ?? Sizing.Fit(),
            Height = Theme.ControlHeight,
            Padding = new Spacing(Theme.ControlPaddingX, 0f),
            Background = !enabled ? Theme.ControlActive : i.Held ? Theme.ControlActive : i.Hovered ? Theme.ControlHovered : Theme.Control,
            BorderColor = enabled ? Theme.ControlBorder : Theme.Control,
            BorderWidth = 1f,
            Radius = Theme.CornerRadius,
        }, DisplayText(label), Style(null, enabled ? Theme.Text : Theme.TextMuted, false, Align.Center));
        return i.Clicked;
    }

    /// <summary>Returns true on the frame <paramref name="value"/> changes.</summary>
    public bool Checkbox(string label, ref bool value, uint? textColor = null)
    {
        var id = GetId(label);
        var i = Interact(id);
        if (i.Hovered) SetCursor(MouseCursor.Hand);
        if (i.Clicked) value = !value;

        Open(id, new Box
        {
            Direction = Direction.Horizontal,
            Height = Theme.ControlHeight,
            Gap = Theme.Gap,
            AlignY = Align.Center,
        });
        Open(0, new Box
        {
            Width = CheckboxSize,
            Height = CheckboxSize,
            Background = value ? Theme.Accent : i.Hovered ? Theme.ControlHovered : Theme.Control,
            BorderColor = value ? Theme.Accent : Theme.ControlBorder,
            BorderWidth = 1f,
            Radius = 4f,
        });
        if (value)
        {
            Line(0.25f, 0.5f, 0.42f, 0.68f, 2f, Theme.OnAccent);
            Line(0.42f, 0.68f, 0.75f, 0.32f, 2f, Theme.OnAccent);
        }
        Close();
        Leaf(0, default, DisplayText(label), Style(null, textColor ?? Theme.Text, false, Align.Start));
        Close();
        return i.Clicked;
    }

    /// <summary>Edits <paramref name="text"/> in place. Returns true when Enter is pressed while
    /// the field has focus.</summary>
    public bool TextField(string id, ref string text, string? placeholder = null, Sizing? width = null)
    {
        var fieldId = GetId(id);
        var i = Interact(fieldId);
        if (i.Hovered) SetCursor(MouseCursor.Text);

        var focused = KeepFocus(fieldId);
        var innerWidth = TryGetRect(fieldId, out var rect) ? rect.Width - FieldPaddingX * 2 : float.PositiveInfinity;
        if (i.Pressed)
        {
            if (!focused) _fieldShift = 0f;
            Focus(fieldId);
            focused = true;
            _caret = CaretAt(text, Input.Pointer.X - rect.X - FieldPaddingX + _fieldShift);
        }

        var submitted = false;
        var caretX = 0f;
        if (focused)
        {
            submitted = Edit(fieldId, ref text);
            focused = _focused == fieldId;
            _caret = Math.Clamp(_caret, 0, text.Length);
            caretX = MeasurePrefix(text, _caret);
            if (caretX - _fieldShift > innerWidth) _fieldShift = caretX - innerWidth;
            if (caretX < _fieldShift) _fieldShift = caretX;
            _fieldShift = MathF.Max(0f, MathF.Min(_fieldShift, MeasureText(text) - innerWidth));
        }
        var shift = focused ? _fieldShift : 0f;

        var field = Open(fieldId, new Box
        {
            Direction = Direction.Horizontal,
            Width = width ?? Sizing.Grow(MinFieldWidth),
            Height = Theme.ControlHeight,
            Padding = new Spacing(FieldPaddingX, 0f),
            AlignY = Align.Center,
            Clip = true,
            Background = Theme.FieldBackground,
            BorderColor = focused ? Theme.Accent : i.Hovered ? Theme.TextMuted : Theme.ControlBorder,
            BorderWidth = 1f,
            Radius = Theme.CornerRadius,
        });
        _tree[field].ScrollX = shift;

        if (text.Length > 0)
            Leaf(0, default, text, Style(null, Theme.Text, false, Align.Start));
        else if (placeholder != null)
            Leaf(0, default, placeholder, Style(null, Theme.TextMuted, false, Align.Start));

        if (focused)
        {
            var lineHeight = LineHeight();
            OpenFloating(0, new Box { Width = 1.5f, Height = lineHeight, Background = Theme.Text },
                0f, 0.5f, FieldPaddingX + caretX - shift, -lineHeight / 2, 1);
            Close();
        }
        Close();
        return submitted;
    }

    /// <summary>Returns true on frames where <paramref name="value"/> changes.</summary>
    public bool Slider(string id, ref float value, float min, float max, Sizing? width = null)
    {
        var sliderId = GetId(id);
        var i = Interact(sliderId);
        if (i.Hovered || i.Held) SetCursor(MouseCursor.Hand);

        var changed = false;
        if (i.Held && max > min && TryGetRect(sliderId, out var rect))
        {
            var track = MathF.Max(1f, rect.Width - SliderThumbRadius * 2);
            var t = Math.Clamp((Input.Pointer.X - rect.X - SliderThumbRadius) / track, 0f, 1f);
            var next = min + t * (max - min);
            changed = next != value;
            value = next;
        }

        var fraction = max > min ? Math.Clamp((value - min) / (max - min), 0f, 1f) : 0f;
        Open(sliderId, new Box
        {
            Direction = Direction.Horizontal,
            Width = width ?? Sizing.Grow(MinFieldWidth),
            Height = Theme.ControlHeight,
            Padding = new Spacing(SliderThumbRadius, 0f),
            AlignY = Align.Center,
        });
        Open(0, new Box { Width = Sizing.Grow(), Height = 4f, Background = Theme.Control, Radius = 2f });
        Leaf(0, new Box { Width = Sizing.Percent(fraction), Height = Sizing.Grow(), Background = Theme.Accent, Radius = 2f });
        OpenFloating(0, new Box
        {
            Width = SliderThumbRadius * 2,
            Height = SliderThumbRadius * 2,
            Background = i.Held || i.Hovered ? Theme.OnAccent : Theme.Text,
            Radius = SliderThumbRadius,
        }, fraction, 0.5f, -SliderThumbRadius, -SliderThumbRadius, 1);
        Close();
        Close();
        Close();
        return changed;
    }

    public void ProgressBar(float fraction, Sizing? width = null)
    {
        Open(0, new Box { Width = width ?? Sizing.Grow(), Height = 8f, Background = Theme.Control, Radius = 4f });
        Leaf(0, new Box { Width = Sizing.Percent(Math.Clamp(fraction, 0f, 1f)), Height = Sizing.Grow(), Background = Theme.Accent, Radius = 4f });
        Close();
    }

    public void Separator()
    {
        if (ParentIsHorizontal)
            Leaf(0, new Box { Width = 1f, Height = Sizing.Grow(), Background = Theme.PanelBorder });
        else
            Leaf(0, new Box { Width = Sizing.Grow(), Height = 1f, Background = Theme.PanelBorder });
    }

    private bool Edit(int fieldId, ref string text)
    {
        var submitted = false;
        _caret = Math.Clamp(_caret, 0, text.Length);
        foreach (var stroke in Input.Keys)
        {
            var shortcut = (stroke.Modifiers & (KeyModifiers.Control | KeyModifiers.Super)) != 0;
            switch (stroke.Key)
            {
                case KeyboardKey.Backspace when _caret > 0:
                {
                    var n = StepBack(text, _caret);
                    text = text.Remove(_caret - n, n);
                    _caret -= n;
                    break;
                }
                case KeyboardKey.Delete when _caret < text.Length:
                    text = text.Remove(_caret, StepForward(text, _caret));
                    break;
                case KeyboardKey.LeftArrow when _caret > 0:
                    _caret -= StepBack(text, _caret);
                    break;
                case KeyboardKey.RightArrow when _caret < text.Length:
                    _caret += StepForward(text, _caret);
                    break;
                case KeyboardKey.Home:
                    _caret = 0;
                    break;
                case KeyboardKey.End:
                    _caret = text.Length;
                    break;
                case KeyboardKey.Enter:
                    submitted = true;
                    break;
                case KeyboardKey.Escape:
                    _focused = 0;
                    break;
                case KeyboardKey.V when shortcut:
                    Insert(ref text, ClipboardText.ReplaceLineEndings(" "));
                    break;
            }
        }

        if (Input.Text.Length > 0 && _focused == fieldId)
            Insert(ref text, Input.Text);
        return submitted;
    }

    private void Insert(ref string text, string value)
    {
        text = text.Insert(_caret, value);
        _caret += value.Length;
    }

    private int CaretAt(string text, float localX)
    {
        var best = 0;
        var bestDistance = MathF.Abs(localX);
        for (var index = 1; index <= text.Length; index++)
        {
            if (index < text.Length && char.IsLowSurrogate(text[index])) continue;
            var distance = MathF.Abs(MeasurePrefix(text, index) - localX);
            if (distance >= bestDistance) continue;
            best = index;
            bestDistance = distance;
        }
        return best;
    }

    private static int StepBack(string text, int caret) =>
        caret >= 2 && char.IsSurrogatePair(text[caret - 2], text[caret - 1]) ? 2 : 1;

    private static int StepForward(string text, int caret) =>
        caret + 1 < text.Length && char.IsSurrogatePair(text[caret], text[caret + 1]) ? 2 : 1;
}
