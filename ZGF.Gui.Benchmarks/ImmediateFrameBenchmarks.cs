using BenchmarkDotNet.Attributes;
using ZGF.Fonts;
using ZGF.Gui;
using ZGF.Gui.Benchmarks;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;

/// <summary>
/// One immediate-mode frame end to end — record, solve, emit into the canvas — for a screen
/// shaped like the todo sample with a long scrolling list, recorded whole or virtualized. Every
/// other row is checked so the checkmark paint path runs.
/// </summary>
[MemoryDiagnoser]
public class ImmediateFrameBenchmarks
{
    private readonly UiInput _input = new();
    private bool[] _done = null!;
    private string[] _labels = null!;
    private BenchCanvas _canvas = null!;
    private Ui _ui = null!;
    private string _draft = "Draft text";
    private bool _hideCompleted;
    private float _volume = 0.4f;

    [Params(200, 10_000)]
    public int Rows { get; set; } = 200;

    [Params(false, true)]
    public bool Virtual { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _done = new bool[Rows];
        _labels = new string[Rows];
        var fonts = new FreeTypeFontBackend();
        var font = fonts.LoadFontFromFile(ResolveFontPath(), 16);
        _canvas = new BenchCanvas(520, 640, fonts, font);
        _ui = new Ui(_canvas, new NullClipboard());
        for (var i = 0; i < Rows; i++)
        {
            _labels[i] = $"Task number {i}##done";
            _done[i] = i % 2 == 0;
        }
        Frame();
    }

    [Benchmark]
    public void Frame()
    {
        _canvas.BeginFrame();
        _ui.BeginFrame(_input, 1f / 60f, 0, _canvas.Width, _canvas.Height);
        Render(_ui);
        _ui.EndFrame();
        _canvas.EndFrame();
    }

    internal const int StageCount = 5;
    internal static readonly string[] StageNames =
        ["canvas.BeginFrame", "ui.BeginFrame", "Render (record)", "ui.EndFrame (solve+emit)", "canvas.EndFrame"];

    internal void MeasureStages(long[] stages)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        _canvas.BeginFrame();
        var t1 = GC.GetAllocatedBytesForCurrentThread();
        _ui.BeginFrame(_input, 1f / 60f, 0, _canvas.Width, _canvas.Height);
        var t2 = GC.GetAllocatedBytesForCurrentThread();
        Render(_ui);
        var t3 = GC.GetAllocatedBytesForCurrentThread();
        _ui.EndFrame();
        var t4 = GC.GetAllocatedBytesForCurrentThread();
        _canvas.EndFrame();
        var t5 = GC.GetAllocatedBytesForCurrentThread();
        stages[0] += t1 - before;
        stages[1] += t2 - t1;
        stages[2] += t3 - t2;
        stages[3] += t4 - t3;
        stages[4] += t5 - t4;
    }

    private void Render(Ui ui)
    {
        var style = ui.Style;
        ui.Heading("Todos");
        ui.Label(ui.Memo("remaining", (Remaining: Rows / 2, Rows), static a => $"{a.Remaining} of {a.Rows} remaining"), style.TextMuted);
        ui.ProgressBar(0.5f);

        using (ui.Row())
        {
            ui.TextField("draft", ref _draft, "What needs doing?");
            ui.Button("Add", enabled: _draft.Length > 0);
        }

        using (ui.Row())
        {
            ui.Checkbox("Hide completed", ref _hideCompleted);
            ui.Spacer();
            ui.Button("Clear completed");
        }

        using (ui.Panel())
        {
            ui.Label("Settings", bold: true);
            using (ui.Row())
            {
                ui.Label("Volume", style.TextMuted);
                ui.Slider("volume", ref _volume, 0f, 1f);
                ui.Label(ui.Memo("volume", (int)MathF.Round(_volume * 100), static percent => $"{percent}%"));
            }
        }

        ui.Separator();

        if (Virtual)
        {
            using var list = ui.VirtualScroll("items", Rows, WidgetTheme.For(ui).ControlHeight);
            for (var i = list.First; i < list.End; i++)
                Row(ui, i);
        }
        else
        {
            using (ui.Scroll("items"))
            {
                for (var i = 0; i < Rows; i++)
                    Row(ui, i);
            }
        }
    }

    private void Row(Ui ui, int i)
    {
        using (ui.Id(i))
        using (ui.Row())
        {
            ui.Checkbox(_labels[i], ref _done[i]);
            ui.Spacer();
            ui.Button("Delete");
        }
    }

    private sealed class NullClipboard : IClipboard
    {
        public void SetText(string text) { }
        public string? GetText() => null;
    }

    private static string ResolveFontPath()
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
