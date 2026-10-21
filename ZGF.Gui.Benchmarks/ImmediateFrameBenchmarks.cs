using BenchmarkDotNet.Attributes;
using ZGF.Desktop;
using ZGF.Geometry;
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
        using var screen = ui.Screen();
        var theme = ui.Theme;
        ui.Heading("Todos");
        ui.Label(ui.Memo("remaining", (Remaining: Rows / 2, Rows), static a => $"{a.Remaining} of {a.Rows} remaining"), theme.TextMuted);
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
                ui.Label("Volume", theme.TextMuted);
                ui.Slider("volume", ref _volume, 0f, 1f);
                ui.Label(ui.Memo("volume", (int)MathF.Round(_volume * 100), static percent => $"{percent}%"));
            }
        }

        ui.Separator();

        if (Virtual)
        {
            using var list = ui.VirtualScroll("items", Rows, theme.ControlHeight);
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

    internal static string ResolveFontPath()
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

/// <summary>
/// One frame of a fully expanded virtualized tree of about 100k nodes (100 roots, 10 children
/// each, 99 grandchildren each), scrolled to the middle, with a selected and focused row.
/// </summary>
[MemoryDiagnoser]
public class ImmediateTreeBenchmarks
{
    private readonly UiInput _input = new();
    private readonly Model _model = new();
    private readonly TreeViewState<Node> _state = new(ReferenceEqualityComparer.Instance);
    private Node[] _roots = null!;
    private BenchCanvas _canvas = null!;
    private Ui _ui = null!;

    private sealed class Node(string label, Node[] children)
    {
        public readonly string Label = label;
        public readonly Node[] Children = children;
    }

    private sealed class Model : ITreeModel<Node>
    {
        public bool HasChildren(Node node) => node.Children.Length > 0;
        public IReadOnlyList<Node> GetChildren(Node node) => node.Children;
        public TreeRow Row(Node node) => new(node.Label, Icon: "#", IconColor: 0xFF4C8DFF, IconBackground: 0x334C8DFF);
    }

    [GlobalSetup]
    public void Setup()
    {
        _roots = new Node[100];
        for (var r = 0; r < _roots.Length; r++)
        {
            var children = new Node[10];
            for (var c = 0; c < children.Length; c++)
            {
                var leaves = new Node[99];
                for (var l = 0; l < leaves.Length; l++)
                    leaves[l] = new Node($"Member{r}_{c}_{l}", []);
                children[c] = new Node($"Type{r}_{c}", leaves);
                _state.SetExpanded(children[c], true);
            }
            _roots[r] = new Node($"Assembly{r}", children);
            _state.SetExpanded(_roots[r], true);
        }
        var fonts = new FreeTypeFontBackend();
        var font = fonts.LoadFontFromFile(ImmediateFrameBenchmarks.ResolveFontPath(), 16);
        _canvas = new BenchCanvas(520, 640, fonts, font);
        _ui = new Ui(_canvas, new NullClipboard());
        Frame();
        _state.Select(_roots[50].Children[5].Children[50]);
        for (var i = 0; i < 4; i++)
            Frame();
    }

    public int VisibleRows => _state.VisibleCount;

    [Benchmark]
    public void Frame()
    {
        _canvas.BeginFrame();
        _ui.BeginFrame(_input, 1f / 60f, 0, _canvas.Width, _canvas.Height);
        using (_ui.Screen())
            _ui.TreeView("tree", _roots, _model, _state);
        _ui.EndFrame();
        _canvas.EndFrame();
    }

    private sealed class NullClipboard : IClipboard
    {
        public void SetText(string text) { }
        public string? GetText() => null;
    }
}

/// <summary>
/// One frame of a menu bar with its File menu open over a screen of content and a tooltip
/// showing on the hovered item: the layer, popup placement and in-window popup emit path.
/// </summary>
[MemoryDiagnoser]
public class ImmediateMenuBenchmarks
{
    private UiInput _input = new();
    private UiInput _next = new();
    private BenchCanvas _canvas = null!;
    private Ui _ui = null!;
    private double _time;
    private bool _wrap = true;

    [GlobalSetup]
    public void Setup()
    {
        var fonts = new FreeTypeFontBackend();
        var font = fonts.LoadFontFromFile(ImmediateFrameBenchmarks.ResolveFontPath(), 16);
        _canvas = new BenchCanvas(800, 600, fonts, font);
        _ui = new Ui(_canvas, new NullClipboard());

        Step();
        Step();
        _ui.TryGetRect(_ui.GetId("File"), out var title);
        Step(i => i.MovePointer(new PointF(title.X + 5f, title.Y + 5f)));
        Step(i => i.Button((int)PointerButton.Left, InputAction.Press, default));
        Step(i => i.Button((int)PointerButton.Left, InputAction.Release, default));
        Step();
        Step();
        _ui.TryGetRect(_ui.GetId("Save as…"), out var item);
        Step(i => i.MovePointer(new PointF(item.X + 20f, item.Y + 5f)));
        _time += 1.0;
        for (var i = 0; i < 4; i++)
            Step();
        if (!_ui.TryGetRect(_ui.GetId("Save as…"), out _) || _canvas.Width == 0)
            throw new InvalidOperationException("The menu did not open.");
    }

    private void Step(Action<UiInput>? input = null)
    {
        (_input, _next) = (_next, _input);
        _input.BeginAfter(_next);
        input?.Invoke(_input);
        Frame();
    }

    [Benchmark]
    public void Frame()
    {
        _canvas.BeginFrame();
        _ui.BeginFrame(_input, 1f / 60f, _time, _canvas.Width, _canvas.Height);
        Render(_ui);
        _ui.EndFrame();
        _canvas.EndFrame();
    }

    private void Render(Ui ui)
    {
        using var screen = ui.Screen();
        using (ui.MenuBar("menu"))
        {
            using (var file = ui.Menu("File"))
            {
                if (file.IsOpen)
                {
                    ui.MenuItem("Open…", "Ctrl+O");
                    ui.MenuItem("Save", "Ctrl+S");
                    ui.MenuItem("Save as…", "Ctrl+Shift+S");
                    ui.Tooltip(ui.GetId("Save as…"), "Saves the document under a new name");
                    ui.MenuSeparator();
                    using (var recent = ui.SubMenu("Recent"))
                    {
                        if (recent.IsOpen)
                            ui.MenuItem("One");
                    }
                    ui.MenuItem("Word wrap", ref _wrap);
                    ui.MenuItem("Exit", "Alt+F4");
                }
            }
            using (var view = ui.Menu("View"))
            {
                if (view.IsOpen)
                    ui.MenuItem("Dark");
            }
        }
        for (var i = 0; i < 20; i++)
        {
            using (ui.Id(i))
                ui.Label("Line of content under the menu");
        }
    }

    private sealed class NullClipboard : IClipboard
    {
        public void SetText(string text) { }
        public string? GetText() => null;
    }
}
