using System.Runtime.InteropServices;
using ZGF.Geometry;

namespace ZGF.Gui.Immediate;

internal struct Element
{
    public int Id;
    public int Parent;
    public int FirstChild;
    public int LastChild;
    public int NextSibling;
    public int FirstPaint;
    public int LastPaint;

    public bool Horizontal;
    public Sizing Width;
    public Sizing Height;
    public Spacing Padding;
    public float Gap;
    public Align AlignX;
    public Align AlignY;
    public float ScrollX;
    public float ScrollY;

    public uint Background;
    public uint BorderColor;
    public float BorderWidth;
    public float Radius;
    public UiShadow Shadow;
    public bool Clip;

    public string? Text;
    public TextStyle? TextStyle;
    public float ContentWidth;
    public float ContentHeight;
    public float LineHeight;
    public TextLines? Lines;
    public bool Wrap;
    public TextOverflow Overflow;
    public string? Shown;
    public IReadOnlyList<RichTextRun>? Runs;
    public RichTextLayoutResult? RunLines;
    public uint CodeBackground;

    public bool Floating;
    public bool KeepsClip;
    public float AnchorX;
    public float AnchorY;
    public float OffsetX;
    public float OffsetY;
    public int Z;
    public int Layer;
    public bool PassThrough;

    public float X;
    public float Y;
    public float W;
    public float H;
    public float ChildExtent;
    public float CrossExtent;
}

internal struct PaintEntry
{
    public PaintCommand Command;
    public int Next;
}

internal abstract class PaintCommand
{
    public abstract void Run(UiPainter painter);
    public abstract void Release();
}

/// <summary>A deferred paint and the state it was given, pooled per state type so recording
/// one allocates nothing once the pool is warm.</summary>
internal sealed class PaintCommand<TState> : PaintCommand
{
    [ThreadStatic] private static Stack<PaintCommand<TState>>? _pool;

    private TState _state = default!;
    private Action<UiPainter, TState> _paint = null!;

    public static PaintCommand<TState> Rent(TState state, Action<UiPainter, TState> paint)
    {
        var pool = _pool ??= new Stack<PaintCommand<TState>>();
        var command = pool.Count > 0 ? pool.Pop() : new PaintCommand<TState>();
        command._state = state;
        command._paint = paint;
        return command;
    }

    public override void Run(UiPainter painter) => _paint(painter, _state);

    public override void Release()
    {
        _state = default!;
        _paint = null!;
        (_pool ??= new Stack<PaintCommand<TState>>()).Push(this);
    }
}

internal readonly record struct HitRegion(int Id, UiRect Rect, int Z, int Order);

/// <summary>One frame's recorded elements: sized, positioned, then emitted to the canvas.</summary>
internal sealed class LayoutTree
{
    private readonly List<Element> _elements = new();
    private readonly List<PaintEntry> _paints = new();
    private readonly List<(int Index, int Z, int Layer, bool PassThrough, UiRect? Clip)> _floating = new();
    private readonly List<(ICanvas? Canvas, UiRect Rect)> _layers = new();
    private readonly List<UiRect> _clips = new();
    private readonly RectStyle _rectStyle = new();
    private ICanvas _mainCanvas = null!;
    private ICanvas _canvas = null!;
    private UiPainter _painter = null!;
    private UiRect _screen;
    private CanvasOrigin _origin;
    private int _order;
    private Dictionary<int, UiRect> _contentRects = null!;

    public ref Element this[int index] => ref CollectionsMarshal.AsSpan(_elements)[index];

    public void Clear()
    {
        _elements.Clear();
        _layers.Clear();
        foreach (var entry in _paints)
            entry.Command.Release();
        _paints.Clear();
    }

    public int Add(in Element element)
    {
        var index = _elements.Count;
        _elements.Add(element);
        ref var e = ref this[index];
        e.FirstChild = e.LastChild = e.NextSibling = -1;
        e.FirstPaint = e.LastPaint = -1;
        if (e.Parent < 0) return index;

        ref var parent = ref this[e.Parent];
        if (parent.LastChild >= 0)
            this[parent.LastChild].NextSibling = index;
        else
            parent.FirstChild = index;
        parent.LastChild = index;
        return index;
    }

    public void AddPaint(int element, PaintCommand command)
    {
        var index = _paints.Count;
        _paints.Add(new PaintEntry { Command = command, Next = -1 });
        ref var e = ref this[element];
        if (e.LastPaint >= 0)
            CollectionsMarshal.AsSpan(_paints)[e.LastPaint].Next = index;
        else
            e.FirstPaint = index;
        e.LastPaint = index;
    }

    /// <summary>Where popup <paramref name="layer"/> is drawn: onto its own canvas, whose origin
    /// is the top-left of <paramref name="rect"/>, or with the main tree when null.</summary>
    public void SetLayer(int layer, ICanvas? canvas, UiRect rect)
    {
        while (_layers.Count < layer)
            _layers.Add(default);
        _layers[layer - 1] = (canvas, rect);
    }

    public void Solve(ITextLayout text)
    {
        Fit(true);
        Distribute(true);
        LayOutText(text);
        Fit(false);
        Distribute(false);
        Position();
    }

    private void LayOutText(ITextLayout text)
    {
        var span = CollectionsMarshal.AsSpan(_elements);
        for (var i = 0; i < span.Length; i++)
        {
            ref var e = ref span[i];
            if (e.Runs != null)
            {
                if (!e.Wrap) continue;
                e.RunLines = text.LayOutRuns(e.Runs, Inner(ref e));
                e.ContentHeight = MathF.Ceiling(e.RunLines.Height);
            }
            else if (e.Wrap)
            {
                e.Lines = text.BreakLines(e.Text!, e.TextStyle!, Inner(ref e));
                e.ContentHeight = e.Lines.Count * e.LineHeight;
            }
            else if (e.Overflow != TextOverflow.Clip && e.Lines == null && e.Text is { } value)
            {
                var inner = Inner(ref e);
                e.Shown = e.ContentWidth <= inner + 0.5f ? null : text.Ellipsize(value, e.TextStyle!, inner, e.Overflow);
            }
        }

        static float Inner(ref Element e) => MathF.Max(0f, e.W - e.Padding.Left - e.Padding.Right);
    }

    public void Position()
    {
        var span = CollectionsMarshal.AsSpan(_elements);
        for (var i = 0; i < span.Length; i++)
        {
            ref var e = ref span[i];
            if (e.FirstChild < 0) continue;

            var innerW = e.W - e.Padding.Left - e.Padding.Right;
            var innerH = e.H - e.Padding.Top - e.Padding.Bottom;
            var startX = e.X + e.Padding.Left - e.ScrollX;
            var startY = e.Y + e.Padding.Top - e.ScrollY;

            var used = 0f;
            var cross = 0f;
            var count = 0;
            for (var c = e.FirstChild; c >= 0; c = span[c].NextSibling)
            {
                if (span[c].Floating) continue;
                used += e.Horizontal ? span[c].W : span[c].H;
                cross = MathF.Max(cross, e.Horizontal ? span[c].H : span[c].W);
                count++;
            }
            used += e.Gap * Math.Max(0, count - 1);
            e.ChildExtent = used + (e.Horizontal ? e.Padding.Left + e.Padding.Right : e.Padding.Top + e.Padding.Bottom);
            e.CrossExtent = cross + (e.Horizontal ? e.Padding.Top + e.Padding.Bottom : e.Padding.Left + e.Padding.Right);

            var free = (e.Horizontal ? innerW : innerH) - used;
            var cursor = MathF.Max(0f, free) * Factor(e.Horizontal ? e.AlignX : e.AlignY);
            var crossAlign = Factor(e.Horizontal ? e.AlignY : e.AlignX);

            for (var c = e.FirstChild; c >= 0; c = span[c].NextSibling)
            {
                ref var child = ref span[c];
                if (child.Floating)
                {
                    child.X = e.X + e.W * child.AnchorX + child.OffsetX;
                    child.Y = e.Y + e.H * child.AnchorY + child.OffsetY;
                    continue;
                }

                if (e.Horizontal)
                {
                    child.X = startX + cursor;
                    child.Y = startY + MathF.Max(0f, innerH - child.H) * crossAlign;
                    cursor += child.W + e.Gap;
                }
                else
                {
                    child.Y = startY + cursor;
                    child.X = startX + MathF.Max(0f, innerW - child.W) * crossAlign;
                    cursor += child.H + e.Gap;
                }
            }
        }
    }

    public void Emit(ICanvas canvas, UiPainter painter, float canvasWidth, float canvasHeight, List<HitRegion> hits, Dictionary<int, UiRect> rects,
        Dictionary<int, UiRect> contentRects)
    {
        _contentRects = contentRects;
        _mainCanvas = canvas;
        _painter = painter;
        _screen = new UiRect(0f, 0f, canvasWidth, canvasHeight);
        _order = 0;
        _floating.Clear();

        UseSurface(0);
        if (_elements.Count > 0)
            EmitElement(0, 0, 0, false, hits, rects);

        for (var k = 0; k < _floating.Count; k++)
        {
            var (index, z, layer, passThrough, clip) = _floating[k];
            UseSurface(layer);
            if (clip is { } kept)
            {
                var inner = _clips[^1].Intersect(kept);
                _clips.Add(inner);
                _canvas.PushClip(_origin.ToCanvas(inner));
            }
            EmitElement(index, z, layer, passThrough, hits, rects);
            if (clip != null)
            {
                _clips.RemoveAt(_clips.Count - 1);
                _canvas.PopClip();
            }
        }
    }

    private void UseSurface(int layer)
    {
        var (canvas, rect) = layer > 0 && layer <= _layers.Count ? _layers[layer - 1] : default;
        _clips.Clear();
        if (canvas == null)
        {
            _canvas = _mainCanvas;
            _origin = new CanvasOrigin(0f, 0f, _screen.Height);
            _clips.Add(_screen);
        }
        else
        {
            _canvas = canvas;
            _origin = new CanvasOrigin(rect.X, rect.Y, rect.Height);
            _clips.Add(rect);
        }
    }

    private void EmitElement(int index, int z, int layer, bool passThrough, List<HitRegion> hits, Dictionary<int, UiRect> rects)
    {
        ref var e = ref this[index];
        var rect = new UiRect(e.X, e.Y, e.W, e.H);
        var clip = _clips[^1];
        var visible = rect.Width > 0f && rect.Height > 0f && clip.Intersects(rect);

        if (e.Id != 0)
        {
            rects[e.Id] = rect;
            if (e.Padding != default)
                _contentRects[e.Id] = new UiRect(
                    rect.X + e.Padding.Left,
                    rect.Y + e.Padding.Top,
                    MathF.Max(0f, rect.Width - e.Padding.Left - e.Padding.Right),
                    MathF.Max(0f, rect.Height - e.Padding.Top - e.Padding.Bottom));
            if (visible && !passThrough)
                hits.Add(new HitRegion(e.Id, clip.Intersect(rect), z, _order++));
        }

        if (visible)
            Paint(ref e, rect, z);

        if (e.Clip)
        {
            var inner = clip.Intersect(rect);
            _clips.Add(inner);
            _canvas.PushClip(_origin.ToCanvas(inner));
        }

        for (var c = e.FirstChild; c >= 0; c = this[c].NextSibling)
        {
            ref var child = ref this[c];
            if (child.Layer != 0)
                _floating.Add((c, child.Z, child.Layer, child.PassThrough, null));
            else if (child.Floating)
                _floating.Add((c, z + child.Z, layer, passThrough, child.KeepsClip ? _clips[^1] : null));
            else
                EmitElement(c, z, layer, passThrough, hits, rects);
        }

        if (e.Clip)
        {
            _clips.RemoveAt(_clips.Count - 1);
            _canvas.PopClip();
        }
    }

    private void Paint(ref Element e, UiRect rect, int z)
    {
        if (e.Shadow.IsVisible)
        {
            _canvas.DrawBoxShadow(new DrawBoxShadowInputs
            {
                Position = _origin.ToCanvas(rect),
                BorderRadius = BorderRadiusStyle.All(e.Radius),
                Shadow = ToStyle(e.Shadow),
                ZIndex = z,
            });
        }

        if ((e.Background >> 24) != 0 || e.BorderWidth > 0f)
        {
            _rectStyle.BackgroundColor = e.Background;
            _rectStyle.BorderColor = BorderColorStyle.All(e.BorderColor);
            _rectStyle.BorderSize = BorderSizeStyle.All(e.BorderWidth);
            _rectStyle.BorderRadius = BorderRadiusStyle.All(e.Radius);
            _canvas.DrawRect(new DrawRectInputs { Position = _origin.ToCanvas(rect), Style = _rectStyle, ZIndex = z });
        }

        if (!string.IsNullOrEmpty(e.Text) && e.TextStyle != null)
        {
            var content = new UiRect(
                rect.X + e.Padding.Left,
                rect.Y + e.Padding.Top,
                MathF.Max(0f, rect.Width - e.Padding.Left - e.Padding.Right),
                MathF.Max(0f, rect.Height - e.Padding.Top - e.Padding.Bottom));
            if (e.Lines == null)
                _canvas.DrawText(new DrawTextInputs { Position = _origin.ToCanvas(content), Text = e.Shown ?? e.Text, Style = e.TextStyle, ZIndex = z });
            else
                PaintLines(ref e, content, z);
        }

        if (e.RunLines != null)
            PaintRuns(ref e, ContentOf(ref e, rect), z);

        for (var p = e.FirstPaint; p >= 0; p = _paints[p].Next)
        {
            _painter.Begin(rect, _clips[^1], z, _canvas, _origin);
            _paints[p].Command.Run(_painter);
        }
    }

    private static UiRect ContentOf(ref Element e, UiRect rect) =>
        new(rect.X + e.Padding.Left,
            rect.Y + e.Padding.Top,
            MathF.Max(0f, rect.Width - e.Padding.Left - e.Padding.Right),
            MathF.Max(0f, rect.Height - e.Padding.Top - e.Padding.Bottom));

    /// <summary>Paints rich text line by line, each slice in its run's style over its code chip,
    /// with its underline or strikethrough.</summary>
    private void PaintRuns(ref Element e, UiRect content, int z)
    {
        var clip = _clips[^1];
        var top = content.Y;
        foreach (var line in e.RunLines!.Lines)
        {
            var band = new UiRect(content.X, top, content.Width, line.Height);
            top += line.Height;
            if (band.Y >= clip.Bottom) break;
            if (!band.Intersects(clip)) continue;
            foreach (var segment in line.Segments)
                PaintSegment(e.Runs![segment.RunIndex], segment, band, e.CodeBackground, z);
        }
    }

    private void PaintSegment(RichTextRun run, RichTextSegment segment, UiRect band, uint codeBackground, int z)
    {
        var rect = new UiRect(band.X + segment.X, band.Y, segment.Width, band.Height);
        if (run.IsCode && codeBackground != 0u)
            PaintCodeChip(rect, codeBackground, z);
        if (run.Underline)
            PaintRule(rect, rect.Bottom - RuleThickness, run.Style, z);
        if (run.Strikethrough)
            PaintRule(rect, rect.Y + rect.Height * StrikeHeight, run.Style, z);
        _canvas.DrawText(new DrawTextInputs
        {
            Position = _origin.ToCanvas(rect),
            Text = run.Text,
            Slice = new Range(segment.Start, segment.Start + segment.Length),
            Style = run.Style,
            ZIndex = z,
        });
    }

    private const float RuleThickness = 1f;
    private const float StrikeHeight = 0.55f;
    private const float CodeChipRadius = 2f;

    private void PaintCodeChip(UiRect rect, uint background, int z)
    {
        _rectStyle.BackgroundColor = background;
        _rectStyle.BorderColor = BorderColorStyle.All(0u);
        _rectStyle.BorderSize = BorderSizeStyle.All(0f);
        _rectStyle.BorderRadius = BorderRadiusStyle.All(CodeChipRadius);
        _canvas.DrawRect(new DrawRectInputs { Position = _origin.ToCanvas(rect), Style = _rectStyle, ZIndex = z });
    }

    private void PaintRule(UiRect rect, float y, TextStyle style, int z)
    {
        _canvas.DrawLine(new DrawLineInputs
        {
            Start = _origin.ToCanvas(new PointF(rect.X, y)),
            End = _origin.ToCanvas(new PointF(rect.Right, y)),
            Thickness = RuleThickness,
            Color = style.TextColor.Value,
            ZIndex = z,
        });
    }

    private void PaintLines(ref Element e, UiRect content, int z)
    {
        var lines = e.Lines!;
        var height = e.LineHeight;
        var top = content.Y + MathF.Max(0f, (content.Height - lines.Count * height) / 2);
        var clip = _clips[^1];
        for (var i = 0; i < lines.Count; i++)
        {
            var length = lines.Length(i);
            if (length == 0) continue;
            var line = new UiRect(content.X, top + i * height, content.Width, height);
            if (line.Y >= clip.Bottom) break;
            if (!line.Intersects(clip)) continue;
            var start = lines.Start(i);
            _canvas.DrawText(new DrawTextInputs
            {
                Position = _origin.ToCanvas(line),
                Text = e.Text!,
                Slice = new Range(start, start + length),
                Style = e.TextStyle!,
                ZIndex = z,
            });
        }
    }

    private void Fit(bool x)
    {
        var span = CollectionsMarshal.AsSpan(_elements);
        for (var i = span.Length - 1; i >= 0; i--)
        {
            ref var e = ref span[i];
            float content;
            if (e.FirstChild >= 0)
            {
                var sum = 0f;
                var max = 0f;
                var count = 0;
                for (var c = e.FirstChild; c >= 0; c = span[c].NextSibling)
                {
                    if (span[c].Floating) continue;
                    var size = x ? span[c].W : span[c].H;
                    sum += size;
                    max = MathF.Max(max, size);
                    count++;
                }
                content = e.Horizontal == x ? sum + e.Gap * Math.Max(0, count - 1) : max;
            }
            else
            {
                content = x ? e.ContentWidth : e.ContentHeight;
            }

            content += x ? e.Padding.Left + e.Padding.Right : e.Padding.Top + e.Padding.Bottom;
            var sizing = x ? e.Width : e.Height;
            var resolved = sizing.Kind switch
            {
                SizingKind.Fixed => sizing.Value,
                SizingKind.Percent => 0f,
                _ => Math.Clamp(content, sizing.Min, MathF.Max(sizing.Min, sizing.Max)),
            };
            if (x) e.W = resolved;
            else e.H = resolved;
        }
    }

    private void Distribute(bool x)
    {
        var span = CollectionsMarshal.AsSpan(_elements);
        for (var i = 0; i < span.Length; i++)
        {
            ref var e = ref span[i];
            if (e.FirstChild < 0) continue;

            var outer = x ? e.W : e.H;
            var inner = outer - (x ? e.Padding.Left + e.Padding.Right : e.Padding.Top + e.Padding.Bottom);
            var mainAxis = e.Horizontal == x;

            var count = 0;
            for (var c = e.FirstChild; c >= 0; c = span[c].NextSibling)
            {
                if (!span[c].Floating) count++;
            }
            var gaps = e.Gap * Math.Max(0, count - 1);

            var used = gaps;
            for (var c = e.FirstChild; c >= 0; c = span[c].NextSibling)
            {
                ref var child = ref span[c];
                var sizing = x ? child.Width : child.Height;
                var basis = child.Floating ? outer : mainAxis ? inner - gaps : inner;
                var size = x ? child.W : child.H;
                if (sizing.Kind == SizingKind.Percent)
                    size = Math.Clamp(basis * sizing.Value, sizing.Min, MathF.Max(sizing.Min, sizing.Max));
                else if (sizing.Kind == SizingKind.Grow && (!mainAxis || child.Floating))
                    size = Math.Clamp(basis, sizing.Min, MathF.Max(sizing.Min, sizing.Max));
                if (x) child.W = size;
                else child.H = size;
                if (!child.Floating) used += size;
            }

            if (!mainAxis) continue;
            var remaining = inner - used;
            if (remaining > 0.01f)
                Resize(span, ref e, x, remaining, grow: true);
            else if (remaining < -0.01f)
                Resize(span, ref e, x, -remaining, grow: false);
        }
    }

    private static void Resize(Span<Element> span, ref Element parent, bool x, float amount, bool grow)
    {
        for (var pass = 0; pass < 32 && amount > 0.01f; pass++)
        {
            var candidates = 0;
            var priority = int.MaxValue;
            for (var c = parent.FirstChild; c >= 0; c = span[c].NextSibling)
            {
                if (!CanResize(ref span[c], x, grow)) continue;
                var own = grow ? 0 : (x ? span[c].Width : span[c].Height).Priority;
                if (own < priority)
                {
                    priority = own;
                    candidates = 0;
                }
                if (own == priority) candidates++;
            }
            if (candidates == 0) return;

            var share = amount / candidates;
            for (var c = parent.FirstChild; c >= 0; c = span[c].NextSibling)
            {
                ref var child = ref span[c];
                if (!CanResize(ref child, x, grow)) continue;
                if (!grow && (x ? child.Width : child.Height).Priority != priority) continue;
                var sizing = x ? child.Width : child.Height;
                var size = x ? child.W : child.H;
                var next = grow ? MathF.Min(size + share, sizing.Max) : MathF.Max(size - share, sizing.Min);
                amount -= MathF.Abs(next - size);
                if (x) child.W = next;
                else child.H = next;
            }
        }
    }

    private static bool CanResize(ref Element child, bool x, bool grow)
    {
        if (child.Floating) return false;
        var sizing = x ? child.Width : child.Height;
        if (sizing.Kind != SizingKind.Grow) return false;
        var size = x ? child.W : child.H;
        return grow ? size < sizing.Max : size > sizing.Min;
    }

    internal static BoxShadowStyle ToStyle(UiShadow shadow) => new()
    {
        OffsetX = shadow.OffsetX,
        OffsetY = -shadow.OffsetY,
        Blur = shadow.Blur,
        Spread = shadow.Spread,
        Color = shadow.Color,
    };

    private static float Factor(Align align) => align switch
    {
        Align.Center => 0.5f,
        Align.End => 1f,
        _ => 0f,
    };
}
