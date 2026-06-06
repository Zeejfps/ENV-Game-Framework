using BenchmarkDotNet.Attributes;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Benchmarks;

/// <summary>
/// Exercises the real per-frame pipeline of <see cref="RenderedCanvasBase"/>.
/// Every benchmark re-stages its content each iteration (BeginFrame + draws),
/// exactly as the app does, so the pre-sort clean detection sees draw-order
/// buffers.
///
/// IdleFrame_RectsHeavy isolates #2: a large, cheap-to-stage rect set that is
/// byte-identical frame-to-frame. Rects avoid text-shaping noise, so the
/// measured delta is the sort/materialize/build work that clean detection skips.
///
/// DrawText_Centered vs DrawText_LeftAligned isolates #3: centered text used to
/// shape each line twice (measure width + emit glyphs); left-aligned shapes once.
///
/// FullFrame_Static is the end-to-end realistic idle frame (rects + text) and
/// reflects #2 and #3 together.
/// </summary>
[MemoryDiagnoser]
public class FrameBenchmarks
{
    private const int Width = 1920;
    private const int Height = 1080;
    private const int RectCount = 300;
    private const int IdleRectCount = 3000;
    private const int TextCount = 150;
    private const string LineText = "The quick brown fox";

    private FreeTypeFontBackend _fonts = null!;
    private BenchCanvas _canvas = null!;
    private RectStyle _rectStyle = null!;
    private TextStyle _centeredStyle = null!;
    private TextStyle _leftStyle = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fonts = new FreeTypeFontBackend();
        var defaultFont = _fonts.LoadFontFromFile(ResolveFontPath(), 16);
        _canvas = new BenchCanvas(Width, Height, _fonts, defaultFont);

        _rectStyle = new RectStyle
        {
            BackgroundColor = 0xFF202020,
            BorderColor = BorderColorStyle.All(0xFF808080),
            BorderSize = BorderSizeStyle.All(1f),
            BorderRadius = BorderRadiusStyle.All(4f),
        };

        _centeredStyle = new TextStyle
        {
            TextColor = 0xFFFFFFFF,
            FontSize = 16f,
            HorizontalAlignment = TextAlignment.Center,
        };

        _leftStyle = new TextStyle
        {
            TextColor = 0xFFFFFFFF,
            FontSize = 16f,
            HorizontalAlignment = TextAlignment.Start,
        };

    }

    [Benchmark]
    public void IdleFrame_RectsHeavy()
    {
        _canvas.BeginFrame();
        for (var i = 0; i < IdleRectCount; i++)
        {
            var x = (i * 37) % (Width - 120);
            var y = (i * 53) % (Height - 40);
            _canvas.DrawRect(new DrawRectInputs
            {
                Position = new RectF(x, y, 120, 32),
                Style = _rectStyle,
                ZIndex = i % 4,
            });
        }
        _canvas.EndFrame();
    }

    [Benchmark]
    public void FullFrame_Static()
    {
        StageFrame(_centeredStyle);
        _canvas.EndFrame();
    }

    [Benchmark]
    public void DrawText_Centered()
    {
        _canvas.BeginFrame();
        EmitText(_centeredStyle);
    }

    [Benchmark]
    public void DrawText_LeftAligned()
    {
        _canvas.BeginFrame();
        EmitText(_leftStyle);
    }

    private void StageFrame(TextStyle textStyle)
    {
        _canvas.BeginFrame();
        for (var i = 0; i < RectCount; i++)
        {
            var x = (i * 37) % (Width - 120);
            var y = (i * 53) % (Height - 40);
            _canvas.DrawRect(new DrawRectInputs
            {
                Position = new RectF(x, y, 120, 32),
                Style = _rectStyle,
                ZIndex = i % 4,
            });
        }
        EmitText(textStyle);
    }

    private void EmitText(TextStyle textStyle)
    {
        for (var i = 0; i < TextCount; i++)
        {
            var x = (i * 41) % (Width - 200);
            var y = (i * 29) % (Height - 24);
            _canvas.DrawText(new DrawTextInputs
            {
                Position = new RectF(x, y, 200, 24),
                Text = LineText,
                Style = textStyle,
                ZIndex = i % 4,
            });
        }
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
