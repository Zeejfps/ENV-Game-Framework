using BenchmarkDotNet.Attributes;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Benchmarks;
using ZGF.Gui.Views;

/// <summary>
/// Exercises the W1 box-constraints layout pass (View.Measure / View.Arrange) on a large,
/// fully-mounted commit-list tree (no virtualization — worst case for the engine).
///
/// Layout_SteadyState is the number that matters most: the app re-runs Measure+Arrange every
/// frame, so this is the per-frame layout cost when nothing changed. The measure cache should
/// make it cheap (every child.Measure is a cache hit; no text re-shaping).
///
/// Layout_OneRowChanged invalidates a single row's text — only that row's subtree should
/// re-measure (O(changed)), the rest stays cached.
///
/// Layout_Cold alternates the viewport width each invocation, busting the cache top-down so
/// the whole tree re-measures — the cost of a genuine relayout (e.g. a window resize).
/// </summary>
[MemoryDiagnoser]
public class LayoutBenchmarks
{
    [Params(2000)]
    public int RowCount;

    // false = fixed-height single-line rows; true = wrapping messages (variable row height).
    [Params(false, true)]
    public bool Wrap;

    private const float Height = 800f;
    private const float RowHeight = 26f;
    private float _width;

    private FreeTypeFontBackend _fonts = null!;
    private BenchCanvas _canvas = null!;
    private Context _context = null!;
    private TouchableFlex _root = null!;
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

        // Column of message rows. Stretch so each row takes the column width; in wrap mode the
        // message wraps to that width (variable row height — the height-for-width case).
        _root = new TouchableFlex { Axis = Axis.Vertical, CrossAxisAlignment = CrossAxisAlignment.Stretch };
        for (var i = 0; i < RowCount; i++)
        {
            var message = new TextView { Text = $"Commit number {i} touching several files across modules", FontSize = 13f };
            if (Wrap) message.TextWrap = TextWrap.Wrap;
            _messageCells[i] = message;
            _altMessages[i] = $"Reworked commit {i} with a noticeably different and longer subject line that also wraps";
            _root.Children.Add(message);
        }

        _root.Context = _context;

        // Prime the cache with a first full pass.
        LayoutPass(_width);
    }

    private void LayoutPass(float width)
    {
        _root.Measure(Constraints.Tight(width, Height));
        _root.Arrange(new RectF(0, 0, width, Height));
    }

    [Benchmark]
    public void Layout_SteadyState() => LayoutPass(_width);

    [Benchmark]
    public void Layout_OneRowChanged()
    {
        var cell = _messageCells[_touchIndex];
        // Swap between two pre-built strings — no per-invocation allocation, but a real
        // content change that invalidates exactly this row's measure.
        cell.Text = _toggle ? _altMessages[_touchIndex] : $"Commit number {_touchIndex} touching some files";
        _toggle = !_toggle;
        _touchIndex = (_touchIndex + 1) % RowCount;
        LayoutPass(_width);
    }

    [Benchmark]
    public void Layout_RootRelayout()
    {
        // Force a relayout from the root (as if a top-level container changed). The cache means
        // unchanged children re-measure as cache hits — no text re-shaping.
        _root.TouchSelf();
        LayoutPass(_width);
    }

    [Benchmark]
    public void Layout_Cold()
    {
        // Alternate width so each invocation sees a constraint different from the last,
        // busting the measure cache from the root down.
        _toggle = !_toggle;
        LayoutPass(_toggle ? _width : _width - 2f);
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
        throw new FileNotFoundException($"Could not locate {rel} walking up from {AppContext.BaseDirectory}");
    }
}

/// <summary>FlexView that lets the benchmark force a measure invalidation from the root.</summary>
sealed class TouchableFlex : FlexView
{
    public void TouchSelf() => InvalidateMeasure();
}
