using BenchmarkDotNet.Attributes;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Benchmarks;

/// <summary>
/// Exercises the real per-frame pipeline of <see cref="RenderedCanvasBase"/>.
///
/// EndFrame_CleanFrame isolates #2: a frame whose staged content is byte-identical
/// to the previous frame (the common idle case). The dirty check passes, so the
/// only question is how much sort/materialize/build-draw-calls work we still do.
///
/// DrawText_Centered vs DrawText_LeftAligned isolates #3: centered text currently
/// shapes each line twice (once to measure width, once to emit glyphs); left-aligned
/// shapes once. The gap is the double-shaping cost.
///
/// FullFrame_Static is the end-to-end realistic idle frame (re-stage identical
/// content + EndFrame) and reflects both #2 and #3 together.
/// </summary>
[MemoryDiagnoser]
public class FrameBenchmarks
{
    private const int Width = 1920;
    private const int Height = 1080;
    private const int RectCount = 300;
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

        // Prime the previous-frame mirrors so EndFrame_CleanFrame measures the
        // unchanged path rather than the first (always-dirty) upload.
        StageFrame(_centeredStyle);
        _canvas.EndFrame();
    }

    [Benchmark]
    public void EndFrame_CleanFrame()
    {
        // Staged content from setup is left untouched (no BeginFrame), so this is
        // the idle path: dirty check passes, nothing uploads.
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
