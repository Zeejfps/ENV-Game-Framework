using ZGF.Geometry;

namespace ZGF.Gui;

public sealed class TextView : MultiChildView
{
    // Getters return the cascade-resolved value (what's actually drawn). Setters route
    // to the imperative local-override slot and trigger a re-cascade. Reading back
    // therefore reflects the rendered color/font/etc., not just the last local write.
    public StyleValue<uint> TextColor
    {
        get => new(_resolvedStyle.TextColor, true);
        set
        {
            if (Equals(_localStyle.TextColor, value)) return;
            _localStyle.TextColor = value;
            MarkLocalStyleDirty();
        }
    }

    public StyleValue<float> FontSize
    {
        get => new(_resolvedStyle.FontSize, true);
        set
        {
            if (Equals(_localStyle.FontSize, value)) return;
            _localStyle.FontSize = value;
            MarkLocalStyleDirty();
        }
    }

    public StyleValue<string> FontFamily
    {
        get => _resolvedStyle.FontFamily != null
            ? new StyleValue<string>(_resolvedStyle.FontFamily, true)
            : default;
        set
        {
            if (Equals(_localStyle.FontFamily, value)) return;
            _localStyle.FontFamily = value;
            MarkLocalStyleDirty();
        }
    }

    public StyleValue<TextAlignment> VerticalTextAlignment
    {
        get => new(_resolvedStyle.VerticalAlignment, true);
        set
        {
            if (Equals(_localStyle.VerticalAlignment, value)) return;
            _localStyle.VerticalAlignment = value;
            MarkLocalStyleDirty();
        }
    }

    public StyleValue<TextAlignment> HorizontalTextAlignment
    {
        get => new(_resolvedStyle.HorizontalAlignment, true);
        set
        {
            if (Equals(_localStyle.HorizontalAlignment, value)) return;
            _localStyle.HorizontalAlignment = value;
            MarkLocalStyleDirty();
        }
    }

    public StyleValue<TextWrap> TextWrap
    {
        get => new(_resolvedStyle.TextWrap, true);
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
        get => new(_resolvedStyle.Rotation, true);
        set
        {
            if (Equals(_localStyle.Rotation, value)) return;
            _localStyle.Rotation = value;
            MarkLocalStyleDirty();
        }
    }

    public StyleValue<FontWeight> FontWeight
    {
        get => new(_resolvedStyle.FontWeight, true);
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

    // Snapshot of the wrap-relevant fields at the last invalidation. OnStyleResolved
    // only invalidates when these actually change, so pure modifier toggles (hover, etc.)
    // that don't touch font / size / wrap don't trash the wrap cache.
    private string? _wrappedFontFamily;
    private float _wrappedFontSize;
    private FontWeight _wrappedFontWeight;
    private TextWrap _wrappedTextWrap;

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
        // Only invalidate the wrap cache when a field that actually affects wrap geometry
        // changed. Hover-only modifier toggles (BindModifier on background color) run
        // through the cascade too — re-wrapping every hover is pure waste.
        if (style.FontFamily != _wrappedFontFamily
            || style.FontSize != _wrappedFontSize
            || style.FontWeight != _wrappedFontWeight
            || style.TextWrap != _wrappedTextWrap)
        {
            _wrappedFontFamily = style.FontFamily;
            _wrappedFontSize = style.FontSize;
            _wrappedFontWeight = style.FontWeight;
            _wrappedTextWrap = style.TextWrap;
            InvalidateWrap();
        }
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
