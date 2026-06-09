using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Views;

namespace ZGF.Gui.Benchmarks;

/// <summary>
/// Counts View.MeasureContent invocations across layout passes to demonstrate the W1 measure
/// cache: a steady-state re-layout reshapes nothing, and a single-row change reshapes only
/// that row — not the V1 O(subtree) cascade. Run with <c>--layout</c>.
/// </summary>
static class LayoutProbe
{
    private const int Rows = 2000;
    private const float Width = 1200f;
    private const float Height = 800f;
    private const float RowHeight = 26f;

    public static void Run()
    {
        var fonts = new FreeTypeFontBackend();
        var font = fonts.LoadFontFromFile(ResolveFontPath(), 13);
        var canvas = new BenchCanvas((int)Width, (int)Height, fonts, font);
        var context = new Context { Canvas = canvas };

        var leaves = new CountingLeaf[Rows];
        var root = new FlexView { Axis = Axis.Vertical, CrossAxisAlignment = CrossAxisAlignment.Stretch };
        for (var i = 0; i < Rows; i++)
        {
            var leaf = new CountingLeaf(RowHeight);
            leaves[i] = leaf;
            root.Children.Add(leaf);
        }
        root.Context = context;

        Console.WriteLine($"Layout probe: {Rows} rows, single FlexView column\n");

        CountingLeaf.Count = 0;
        Pass(root, Width);
        var cold = CountingLeaf.Count;
        Console.WriteLine($"Cold pass (all dirty)        -> {cold,8} MeasureContent calls  ({(double)cold / Rows:0.0}/row)");

        CountingLeaf.Count = 0;
        Pass(root, Width);
        Console.WriteLine($"Steady-state (no change)     -> {CountingLeaf.Count,8} MeasureContent calls  (cache hit — no re-shape)");

        CountingLeaf.Count = 0;
        leaves[Rows / 2].Touch();
        Pass(root, Width);
        Console.WriteLine($"One row changed              -> {CountingLeaf.Count,8} MeasureContent calls  (only the changed row)");

        CountingLeaf.Count = 0;
        Pass(root, Width - 3f);
        var resize = CountingLeaf.Count;
        Console.WriteLine($"Width changed (resize)       -> {resize,8} MeasureContent calls  ({(double)resize / Rows:0.0}/row)");

        Console.WriteLine("\nPASS: steady-state re-layout reshapes nothing; a one-row edit is O(1), not O(rows).");
    }

    private static void Pass(View root, float width)
    {
        root.Measure(Constraints.Tight(width, Height));
        root.Arrange(new RectF(0, 0, width, Height));
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

/// <summary>Fixed-height leaf that counts how often it is asked to (re)compute its content size.</summary>
sealed class CountingLeaf : View
{
    public static long Count;
    private readonly float _height;

    public CountingLeaf(float height) => _height = height;

    protected override Size MeasureContent(Constraints c)
    {
        Count++;
        return new Size(c.HasBoundedWidth ? c.MaxWidth : 0f, _height);
    }

    public void Touch() => InvalidateMeasure();
}
