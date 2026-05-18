using ZGF.Geometry;

namespace ZGF.Gui;

public sealed class TextView : MultiChildView
{
    private readonly TextStyle _style = new();

    public StyleValue<uint> TextColor
    {
        get => _style.TextColor;
        set => SetField(ref _style.TextColor, value);
    }

    public StyleValue<float> FontSize
    {
        get => _style.FontSize;
        set => SetField(ref _style.FontSize, value);
    }

    public StyleValue<string> FontFamily
    {
        get => _style.FontFamily;
        set => SetField(ref _style.FontFamily, value);
    }

    public StyleValue<TextAlignment> VerticalTextAlignment
    {
        get => _style.VerticalAlignment;
        set => SetField(ref _style.VerticalAlignment, value);
    }

    public StyleValue<TextAlignment> HorizontalTextAlignment
    {
        get => _style.HorizontalAlignment;
        set => SetField(ref _style.HorizontalAlignment, value);
    }

    public StyleValue<TextWrap> TextWrap
    {
        get => _style.TextWrap;
        set
        {
            if (SetField(ref _style.TextWrap, value))
                InvalidateWrap();
        }
    }

    public StyleValue<float> Rotation
    {
        get => _style.Rotation;
        set => SetField(ref _style.Rotation, value);
    }

    private string? _text;
    public string? Text
    {
        get => _text;
        set
        {
            if (SetField(ref _text, value))
                InvalidateWrap();
        }
    }

    private readonly List<string> _wrappedLines = new();
    private float _wrappedForWidth = -1f;
    private string? _wrappedFromText;

    private bool IsWrapping => _style.TextWrap.IsSet && _style.TextWrap.Value == ZGF.Gui.TextWrap.Wrap;

    public override float MeasureWidth()
    {
        if (PreferredWidth.IsSet)
            return PreferredWidth;

        if (Context == null || _text == null)
            return 0f;

        if (!HasNewlines(_text))
            return Context.Canvas.MeasureTextWidth(_text, _style);

        var max = 0f;
        foreach (var line in SplitLines(_text))
        {
            var w = Context.Canvas.MeasureTextWidth(line, _style);
            if (w > max) max = w;
        }
        return max;
    }

    public override float MeasureHeight()
    {
        if (Context == null || _text == null)
            return 0f;

        var lineHeight = Context.Canvas.MeasureTextLineHeight(_style);

        if (IsWrapping)
        {
            var width = ResolveWrapWidth();
            EnsureWrapped(width);
            var lines = Math.Max(1, _wrappedLines.Count);
            return lines * lineHeight;
        }

        var lineCount = HasNewlines(_text) ? SplitLines(_text).Length : 1;
        return lineCount * lineHeight;
    }

    private static bool HasNewlines(string s) => s.IndexOf('\n') >= 0 || s.IndexOf('\r') >= 0;

    private static string[] SplitLines(string s) => s.Replace("\r\n", "\n").Split('\n');

    private float ResolveWrapWidth()
    {
        if (WidthConstraint.IsSet) return WidthConstraint.Value;
        if (PreferredWidth.IsSet) return PreferredWidth.Value;
        return 0f;
    }

    private void EnsureWrapped(float width)
    {
        if (Context == null || _text == null)
        {
            _wrappedLines.Clear();
            _wrappedFromText = null;
            _wrappedForWidth = -1f;
            return;
        }
        if (Math.Abs(width - _wrappedForWidth) < 0.5f && ReferenceEquals(_wrappedFromText, _text))
            return;
        _wrappedLines.Clear();
        TextWrapper.Wrap(Context.Canvas, _text, _style, width, _wrappedLines);
        _wrappedForWidth = width;
        _wrappedFromText = _text;
    }

    private void InvalidateWrap()
    {
        _wrappedForWidth = -1f;
        _wrappedFromText = null;
    }

    protected override void OnApplyStyle(Style style)
    {
        base.OnApplyStyle(style);
        if (style.TextColor.IsSet)
            _style.TextColor = style.TextColor;
        if (style.FontSize.IsSet)
            _style.FontSize = style.FontSize;
        if (style.Rotation.IsSet)
            _style.Rotation = style.Rotation;
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        if (_text == null)
            return;

        var z = GetDrawZIndex();

        if (IsWrapping)
        {
            EnsureWrapped(Position.Width);
            DrawLines(c, _wrappedLines, z);
            return;
        }

        if (!HasNewlines(_text))
        {
            c.DrawText(new DrawTextInputs
            {
                Position = Position,
                Text = _text,
                Style = _style,
                ZIndex = z,
            });
            return;
        }

        DrawLines(c, SplitLines(_text), z);
    }

    private void DrawLines(ICanvas c, IReadOnlyList<string> lines, int z)
    {
        var lineHeight = c.MeasureTextLineHeight(_style);
        var top = Position.Top;
        for (var i = 0; i < lines.Count; i++)
        {
            var lineRect = new RectF(Position.Left, top - lineHeight, Position.Width, lineHeight);
            c.DrawText(new DrawTextInputs
            {
                Position = lineRect,
                Text = lines[i],
                Style = _style,
                ZIndex = z,
            });
            top -= lineHeight;
        }
    }
}
