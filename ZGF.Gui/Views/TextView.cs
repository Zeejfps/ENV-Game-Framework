using ZGF.Fonts;
using ZGF.Geometry;

namespace ZGF.Gui.Views;

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

    public StyleValue<TextOverflow> TextOverflow
    {
        get => _style.TextOverflow;
        set => SetField(ref _style.TextOverflow, value);
    }

    public StyleValue<float> Rotation
    {
        get => _style.Rotation;
        set => SetField(ref _style.Rotation, value);
    }

    public StyleValue<FontWeight> FontWeight
    {
        get => _style.FontWeight;
        set => SetField(ref _style.FontWeight, value);
    }

    public StyleValue<FontFeatureSet> FontFeatures
    {
        get => _style.FontFeatures;
        set => SetField(ref _style.FontFeatures, value);
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

    protected override Size MeasureContent(Constraints c)
    {
        if (Context == null || _text == null)
            return new Size(Width.IsSet ? Width.Value : 0f, Height.IsSet ? Height.Value : 0f);

        var lineHeight = Context.Canvas.MeasureTextLineHeight(_style);

        // The width we will actually be laid out at: a fixed Width, else our natural width,
        // clamped into the incoming constraint (tight cross from a Stretch parent forces it).
        var laidWidth = Width.IsSet ? Width.Value : NaturalWidth();
        laidWidth = Math.Clamp(laidWidth, c.MinWidth, c.MaxWidth);

        float height;
        if (Height.IsSet)
            height = Height.Value;
        else if (IsWrapping && float.IsFinite(laidWidth) && laidWidth > 0f)
        {
            EnsureWrapped(laidWidth);
            height = Math.Max(1, _wrappedLines.Count) * lineHeight;
        }
        else
        {
            var lineCount = HasNewlines(_text) ? SplitLines(_text).Length : 1;
            height = lineCount * lineHeight;
        }
        return new Size(laidWidth, height);
    }

    private float NaturalWidth()
    {
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

    public override float MeasureWidth()
    {
        if (Width.IsSet)
            return Width;

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

    public override float MeasureHeight(float availableWidth)
    {
        if (Context == null || _text == null)
            return 0f;

        var lineHeight = Context.Canvas.MeasureTextLineHeight(_style);

        // availableWidth <= 0 means "unconstrained"; treat as single-line natural width.
        if (IsWrapping && availableWidth > 0f)
        {
            EnsureWrapped(availableWidth);
            var lines = Math.Max(1, _wrappedLines.Count);
            return lines * lineHeight;
        }

        var lineCount = HasNewlines(_text) ? SplitLines(_text).Length : 1;
        return lineCount * lineHeight;
    }

    private static bool HasNewlines(string s) => s.IndexOf('\n') >= 0 || s.IndexOf('\r') >= 0;

    private static string[] SplitLines(string s) => s.Replace("\r\n", "\n").Split('\n');

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
                Text = Ellipsize(c, _text, Position.Width),
                Style = _style,
                ZIndex = z,
            });
            return;
        }

        DrawLines(c, SplitLines(_text), z);
    }

    private string Ellipsize(ICanvas c, string text, float available)
    {
        if (!_style.TextOverflow.IsSet || _style.TextOverflow.Value != ZGF.Gui.TextOverflow.Ellipsis)
            return text;
        if (string.IsNullOrEmpty(text)) return text;
        if (available <= 0f) return string.Empty;
        if (c.MeasureTextWidth(text, _style) <= available) return text;

        const string ellipsis = "…";
        var ellipsisWidth = c.MeasureTextWidth(ellipsis, _style);
        if (ellipsisWidth > available) return ellipsis;

        var lo = 0;
        var hi = text.Length;
        while (lo < hi)
        {
            var mid = (lo + hi + 1) / 2;
            if (c.MeasureTextWidth(text.AsSpan(0, mid), _style) + ellipsisWidth <= available)
                lo = mid;
            else
                hi = mid - 1;
        }
        return text[..lo] + ellipsis;
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
