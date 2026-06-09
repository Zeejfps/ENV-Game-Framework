using BenchmarkDotNet.Attributes;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Benchmarks;
using ZGF.Gui.Views;

/// <summary>
/// Head-to-head: the V1 layout approach (re-measure every pass, no cache, dirty discovered by
/// an O(subtree) walk) vs the V2 box-constraints engine (Measure cache + targeted invalidation),
/// on an identical 2000-row commit-list tree.
///
/// V1 containers below faithfully mirror the original FlexColumnView/FlexRowView: they re-call
/// child.MeasureHeight/MeasureWidth on every layout pass (no caching), so any relayout re-shapes
/// the whole subtree's text. They are driven by the legacy View.LayoutSelf() path.
///
/// Compare with the V2 numbers from <c>LayoutBenchmarks</c> (Layout_SteadyState / Layout_OneRowChanged).
/// </summary>
[MemoryDiagnoser]
public class LegacyLayoutBenchmarks
{
    [Params(2000)]
    public int RowCount;

    [Params(false, true)]
    public bool Wrap;

    private const float Height = 800f;
    private float _width;

    private FreeTypeFontBackend _fonts = null!;
    private BenchCanvas _canvas = null!;
    private Context _context = null!;
    private V1Column _root = null!;
    private TextView[] _messageCells = null!;
    private string[] _altMessages = null!;
    private bool _toggle;
    private int _touchIndex;

    [GlobalSetup]
    public void Setup()
    {
        _fonts = new FreeTypeFontBackend();
        var font = _fonts.LoadFontFromFile(ResolveFontPath(), 13);
        _width = Wrap ? 360f : 1200f;
        _canvas = new BenchCanvas((int)_width, (int)Height, _fonts, font);
        _context = new Context { Canvas = _canvas };

        _messageCells = new TextView[RowCount];
        _altMessages = new string[RowCount];

        _root = new V1Column { Width = _width, Height = Height };
        for (var i = 0; i < RowCount; i++)
        {
            var message = new TextView { Text = $"Commit number {i} touching several files across modules", FontSize = 13f };
            if (Wrap) message.TextWrap = TextWrap.Wrap;
            _messageCells[i] = message;
            _altMessages[i] = $"Reworked commit {i} with a noticeably different and longer subject line that also wraps";
            _root.Add(message);
        }

        _root.Context = _context;
        LegacyPass(); // prime
    }

    private void LegacyPass()
    {
        // Legacy driver: the V1 main loop called _root.LayoutSelf() each frame.
        _root.LayoutSelf();
    }

    [Benchmark(Description = "V1 unchanged frame")]
    public void Legacy_Unchanged() => LegacyPass();

    [Benchmark(Description = "V1 one row changed")]
    public void Legacy_OneRowChanged()
    {
        var cell = _messageCells[_touchIndex];
        cell.Text = _toggle ? _altMessages[_touchIndex] : $"Commit number {_touchIndex} touching some files";
        _toggle = !_toggle;
        _touchIndex = (_touchIndex + 1) % RowCount;
        LegacyPass();
    }

    [Benchmark(Description = "V1 root relayout")]
    public void Legacy_RootRelayout()
    {
        _root.TouchSelf();
        LegacyPass();
    }

    private static string ResolveFontPath()
    {
        const string rel = "ZGF.Gui/Assets/Fonts/Inter/Inter-Regular.ttf";
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            var candidate = Path.Combine(dir, rel);
            if (File.Exists(candidate))
                return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        throw new FileNotFoundException(rel);
    }
}

// ---- V1-faithful containers: no measure cache; re-measure children every pass. ----

sealed class V1Column : MultiChildView
{
    public float Gap { get; set => SetField(ref field, value); }
    public void Add(View v) => AddChildToSelf(v);
    public void TouchSelf() => SetDirty();

    public override float MeasureHeight(float availableWidth)
    {
        if (Height.IsSet) return Height;
        var total = 0f; var c = 0;
        foreach (var ch in _children) { if (!ch.IsVisible) continue; total += ch.MeasureHeight(availableWidth); c++; }
        return total + (c > 0 ? (c - 1) * Gap : 0f);
    }

    protected override void OnLayoutChildren()
    {
        var pos = Position;
        var top = pos.Top;
        var first = true;
        foreach (var ch in _children)
        {
            if (!ch.IsVisible) continue;
            if (!first) top -= Gap;
            var h = ch.MeasureHeight(pos.Width);   // re-measure every pass (no cache)
            ch.LeftConstraint = pos.Left;
            ch.BottomConstraint = top - h;
            ch.WidthConstraint = pos.Width;
            ch.HeightConstraint = h;
            ch.LayoutSelf();
            top -= h;
            first = false;
        }
    }
}

sealed class V1Row : MultiChildView
{
    public float Gap { get; set => SetField(ref field, value); }
    public void Add(View v) => AddChildToSelf(v);

    public override float MeasureWidth()
    {
        var total = 0f; var c = 0;
        foreach (var ch in _children) { if (!ch.IsVisible) continue; total += ch.MeasureWidth(); c++; }
        return total + (c > 0 ? (c - 1) * Gap : 0f);
    }

    protected override void OnLayoutChildren()
    {
        var pos = Position;
        var left = pos.Left;
        var first = true;
        foreach (var ch in _children)
        {
            if (!ch.IsVisible) continue;
            if (!first) left += Gap;
            var w = ch.MeasureWidth();             // re-measure every pass (no cache)
            ch.LeftConstraint = left;
            ch.BottomConstraint = pos.Bottom;
            ch.WidthConstraint = w;
            ch.HeightConstraint = pos.Height;
            ch.LayoutSelf();
            left += w;
            first = false;
        }
    }
}

sealed class V1Rect : MultiChildView
{
    public void Add(View v) => AddChildToSelf(v);

    protected override void OnLayoutChildren()
    {
        var pos = Position;
        foreach (var ch in _children)
        {
            ch.LeftConstraint = pos.Left;
            ch.BottomConstraint = pos.Bottom;
            ch.WidthConstraint = pos.Width;
            ch.HeightConstraint = pos.Height;
            ch.LayoutSelf();
        }
    }
}
