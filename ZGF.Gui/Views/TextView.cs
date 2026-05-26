using ZGF.Geometry;

namespace ZGF.Gui;

public sealed class TextView : MultiChildView
{
    public StyleValue<uint> TextColor
    {
        get => _localStyle.TextColor;
        set
        {
            if (Equals(_localStyle.TextColor, value)) return;
            _localStyle.TextColor = value;
            MarkLocalStyleDirty();
        }
    }

    public StyleValue<float> FontSize
    {
        get => _localStyle.FontSize;
        set
        {
            if (Equals(_localStyle.FontSize, value)) return;
            _localStyle.FontSize = value;
            MarkLocalStyleDirty();
        }
    }

    public StyleValue<string> FontFamily
    {
        get => _localStyle.FontFamily;
        set
        {
            if (Equals(_localStyle.FontFamily, value)) return;
            _localStyle.FontFamily = value;
            MarkLocalStyleDirty();
        }
    }

    public StyleValue<TextAlignment> VerticalTextAlignment
    {
        get => _localStyle.VerticalAlignment;
        set
        {
            if (Equals(_localStyle.VerticalAlignment, value)) return;
            _localStyle.VerticalAlignment = value;
            MarkLocalStyleDirty();
        }
    }

    public StyleValue<TextAlignment> HorizontalTextAlignment
    {
        get => _localStyle.HorizontalAlignment;
        set
        {
            if (Equals(_localStyle.HorizontalAlignment, value)) return;
            _localStyle.HorizontalAlignment = value;
            MarkLocalStyleDirty();
        }
    }

    public StyleValue<TextWrap> TextWrap
    {
        get => _localStyle.TextWrap;
        set
        {
            if (Equals(_localStyle.TextWrap, value)) return;
            _localStyle.TextWrap = value;
            MarkLocalStyleDirty();
            InvalidateWrap();
        }
    }

    public StyleValue<float> Rotation
    {
        get => _localStyle.Rotation;
        set
        {
            if (Equals(_localStyle.Rotation, value)) return;
            _localStyle.Rotation = value;
            MarkLocalStyleDirty();
        }
    }

    public StyleValue<FontWeight> FontWeight
    {
        get => _localStyle.FontWeight;
        set
        {
            if (Equals(_localStyle.FontWeight, value)) return;
            _localStyle.FontWeight = value;
            MarkLocalStyleDirty();
        }
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

    private bool IsWrapping => _resolvedStyle.TextWrap == ZGF.Gui.TextWrap.Wrap;

    public override float MeasureWidth()
    {
        if (PreferredWidth.IsSet)
            return PreferredWidth;

        if (Context == null || _text == null)
            return 0f;

        var style = BuildDrawStyle();
        if (!HasNewlines(_text))
            return Context.Canvas.MeasureTextWidth(_text, style);

        var max = 0f;
        foreach (var line in SplitLines(_text))
        {
            var w = Context.Canvas.MeasureTextWidth(line, style);
            if (w > max) max = w;
        }
        return max;
    }

    public override float MeasureHeight(float availableWidth)
    {
        if (Context == null || _text == null)
            return 0f;

        var style = BuildDrawStyle();
        var lineHeight = Context.Canvas.MeasureTextLineHeight(style);

        // availableWidth <= 0 means "unconstrained"; treat as single-line natural width.
        if (IsWrapping && availableWidth > 0f)
        {
            EnsureWrapped(availableWidth, style);
            var lines = Math.Max(1, _wrappedLines.Count);
            return lines * lineHeight;
        }

        var lineCount = HasNewlines(_text) ? SplitLines(_text).Length : 1;
        return lineCount * lineHeight;
    }

    protected override void OnStyleResolved(ResolvedStyle style)
    {
        base.OnStyleResolved(style);
        // Anything that affects wrap geometry — font, size, wrap mode — has changed,
        // so the cached wrap is stale.
        InvalidateWrap();
    }

    private static bool HasNewlines(string s) => s.IndexOf('\n') >= 0 || s.IndexOf('\r') >= 0;

    private static string[] SplitLines(string s) => s.Replace("\r\n", "\n").Split('\n');

    private void EnsureWrapped(float width, TextStyle style)
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
        TextWrapper.Wrap(Context.Canvas, _text, style, width, _wrappedLines);
        _wrappedForWidth = width;
        _wrappedFromText = _text;
    }

    private void InvalidateWrap()
    {
        _wrappedForWidth = -1f;
        _wrappedFromText = null;
    }

    private TextStyle BuildDrawStyle() => new()
    {
        TextColor = new StyleValue<uint>(_resolvedStyle.TextColor, true),
        FontFamily = _resolvedStyle.FontFamily != null
            ? new StyleValue<string>(_resolvedStyle.FontFamily, true)
            : default,
        FontSize = new StyleValue<float>(_resolvedStyle.FontSize, true),
        FontWeight = new StyleValue<FontWeight>(_resolvedStyle.FontWeight, true),
        HorizontalAlignment = new StyleValue<TextAlignment>(_resolvedStyle.HorizontalAlignment, true),
        VerticalAlignment = new StyleValue<TextAlignment>(_resolvedStyle.VerticalAlignment, true),
        TextWrap = new StyleValue<TextWrap>(_resolvedStyle.TextWrap, true),
        Rotation = new StyleValue<float>(_resolvedStyle.Rotation, true),
    };

    protected override void OnDrawSelf(ICanvas c)
    {
        if (_text == null)
            return;

        var z = GetDrawZIndex();
        var style = BuildDrawStyle();

        if (IsWrapping)
        {
            EnsureWrapped(Position.Width, style);
            DrawLines(c, _wrappedLines, z, style);
            return;
        }

        if (!HasNewlines(_text))
        {
            c.DrawText(new DrawTextInputs
            {
                Position = Position,
                Text = _text,
                Style = style,
                ZIndex = z,
            });
            return;
        }

        DrawLines(c, SplitLines(_text), z, style);
    }

    private void DrawLines(ICanvas c, IReadOnlyList<string> lines, int z, TextStyle style)
    {
        var lineHeight = c.MeasureTextLineHeight(style);
        var top = Position.Top;
        for (var i = 0; i < lines.Count; i++)
        {
            var lineRect = new RectF(Position.Left, top - lineHeight, Position.Width, lineHeight);
            c.DrawText(new DrawTextInputs
            {
                Position = lineRect,
                Text = lines[i],
                Style = style,
                ZIndex = z,
            });
            top -= lineHeight;
        }
    }
}
