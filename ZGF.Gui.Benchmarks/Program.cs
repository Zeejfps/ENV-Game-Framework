using BenchmarkDotNet.Running;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Benchmarks;

if (args.Length > 0 && args[0] == "--drawcalls")
{
    DrawCallProbe.Run();
    return;
}

if (args.Length > 0 && args[0] == "--verify")
{
    VerifyProbe.Run();
    return;
}

if (args.Length > 0 && args[0] == "--layout")
{
    LayoutProbe.Run();
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

public partial class Program;

static class DrawCallProbe
{
    public static void Run()
    {
        const int width = 1920, height = 1080, widgets = 150;
        var fonts = new FreeTypeFontBackend();
        var font = fonts.LoadFontFromFile(ResolveFontPath(), 16);
        var canvas = new BenchCanvas(width, height, fonts, font);

        var rectStyle = new RectStyle { BackgroundColor = 0xFF202020 };
        var textStyle = new TextStyle { TextColor = 0xFFFFFFFF, FontSize = 16f };

        // Single-column list (the common case): rect+text per row, no overlaps.
        canvas.BeginFrame();
        for (var i = 0; i < 30; i++)
        {
            var y = 20 + i * 34;
            canvas.DrawRect(new DrawRectInputs { Position = new RectF(20, y, 300, 28), Style = rectStyle, ZIndex = 0 });
            canvas.DrawText(new DrawTextInputs { Position = new RectF(26, y + 4, 288, 20), Text = "Label", Style = textStyle, ZIndex = 0 });
        }
        canvas.EndFrame();
        Console.WriteLine($"Interleaved 30 rect+text rows, single column   -> {canvas.LastDrawCallCount} draw calls");

        // 5-column grid: conservative union batches per column (correct, not tight).
        canvas.BeginFrame();
        for (var i = 0; i < widgets; i++)
        {
            var x = 20 + (i / 30) * 320;
            var y = 20 + (i % 30) * 34;
            canvas.DrawRect(new DrawRectInputs { Position = new RectF(x, y, 300, 28), Style = rectStyle, ZIndex = 0 });
            canvas.DrawText(new DrawTextInputs { Position = new RectF(x + 6, y + 4, 288, 20), Text = "Label", Style = textStyle, ZIndex = 0 });
        }
        canvas.EndFrame();
        Console.WriteLine($"Interleaved {widgets} rect+text rows, 5-col grid -> {canvas.LastDrawCallCount} draw calls");

        // Heavily overlapping widgets: must NOT over-batch (correctness).
        canvas.BeginFrame();
        for (var i = 0; i < widgets; i++)
        {
            var x = (i * 41) % (width - 200);
            var y = (i * 29) % (height - 24);
            canvas.DrawRect(new DrawRectInputs { Position = new RectF(x, y, 200, 24), Style = rectStyle, ZIndex = 0 });
            canvas.DrawText(new DrawTextInputs { Position = new RectF(x, y, 200, 24), Text = "Label", Style = textStyle, ZIndex = 0 });
        }
        canvas.EndFrame();
        Console.WriteLine($"Interleaved {widgets} rect+text widgets, overlapping -> {canvas.LastDrawCallCount} draw calls");
    }

    static string ResolveFontPath()
    {
        const string rel = "ZGF.Gui/Assets/Fonts/Inter/Inter-Regular.ttf";
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            var candidate = Path.Combine(dir, rel);
            if (File.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        throw new FileNotFoundException(rel);
    }
}
