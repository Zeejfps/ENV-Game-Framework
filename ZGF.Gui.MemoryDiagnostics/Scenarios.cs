using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Views;
using ZGF.Observable;

namespace ZGF.Gui.MemoryDiagnostics;

/// <summary>
/// A stress scenario: builds a root view and optionally mutates it each frame (on the UI
/// thread) to isolate one suspected leak source. Run "idle" first — if that alone climbs,
/// the leak is in the per-frame render loop / driver, not the view tree.
/// </summary>
public interface IScenario
{
    string Name { get; }
    View BuildRoot(ICanvas canvas);
    void Tick(long frame);
}

public static class Scenarios
{
    public static readonly string[] Names =
        { "idle", "fontsizes", "textchurn", "listchurn", "derivedchurn" };

    public static IScenario Create(string name) => name switch
    {
        "idle" => new IdleScenario(),
        "fontsizes" => new FontSizesScenario(),
        "textchurn" => new TextChurnScenario(),
        "listchurn" => new ListChurnScenario(),
        "derivedchurn" => new DerivedChurnScenario(),
        _ => throw new ArgumentException(
            $"Unknown scenario '{name}'. Available: {string.Join(", ", Names)}"),
    };

    private static RectView MakeRow(uint color) => new()
    {
        Width = 240f,
        Height = 18f,
        BackgroundColor = color,
    };

    // Static content. Renders every frame but never mutates — isolates the pure per-frame
    // native render path (drawable / command buffer / encoder acquisition).
    private sealed class IdleScenario : IScenario
    {
        public string Name => "idle";

        public View BuildRoot(ICanvas canvas)
        {
            var col = new ColumnView { Gap = 4 };
            col.Children.Add(new RectView { Width = 400f, Height = 60f, BackgroundColor = 0xFF2D6CDF });
            col.Children.Add(new TextView(canvas) { Text = "memory diagnostics — idle", TextColor = 0xFFFFFFFF, FontSize = 18f });
            col.Children.Add(new TextView(canvas) { Text = "static frame, no mutation", TextColor = 0xFFAAAAAA, FontSize = 14f });
            return col;
        }

        public void Tick(long frame) { }
    }

    // Cycles the font size every frame. If a bounded set of sizes still grows memory
    // without plateauing, the font backend's sized-variant/atlas cache isn't deduping.
    private sealed class FontSizesScenario : IScenario
    {
        private TextView? _text;
        public string Name => "fontsizes";
        public View BuildRoot(ICanvas canvas) =>
            _text = new TextView(canvas) { Text = "The quick brown fox 0123456789", TextColor = 0xFFFFFFFF };
        public void Tick(long frame) => _text!.FontSize = 8f + frame % 64;
    }

    // Replaces the text string every frame — new managed string + reshape each frame.
    // Managed churn should be collected; if the working set climbs, shaping/glyph path leaks.
    private sealed class TextChurnScenario : IScenario
    {
        private TextView? _text;
        public string Name => "textchurn";
        public View BuildRoot(ICanvas canvas) =>
            _text = new TextView(canvas) { TextColor = 0xFFFFFFFF, FontSize = 18f };
        public void Tick(long frame) => _text!.Text = $"frame {frame} :: the quick brown fox jumps {frame * 7 % 9973}";
    }

    // Steady-size list with constant add/remove churn — exercises ChildrenBindingBehavior
    // attach/detach and view creation/teardown over time.
    private sealed class ListChurnScenario : IScenario
    {
        private readonly ObservableList<int> _items = new();
        private readonly ColumnView _root;

        public ListChurnScenario()
        {
            _root = new ColumnView { Gap = 2 };
            _root.Children.BindChildren(_items, i => MakeRow((uint)(0xFF000000 | (uint)(i * 2654435761u))));
            for (var i = 0; i < 40; i++) _items.Add(i);
        }

        public string Name => "listchurn";
        public View BuildRoot(ICanvas canvas) => _root;

        public void Tick(long frame)
        {
            _items.Add((int)frame);
            if (_items.Count > 40) _items.RemoveAt(0);
        }
    }

    // Derived-children binding driven by a changing State — forces a full reseed (detach all,
    // re-create all) every frame. This is the path that orphaned a Derived per detach.
    private sealed class DerivedChurnScenario : IScenario
    {
        private readonly State<int> _version = new(1);
        private readonly ColumnView _root;

        public DerivedChurnScenario()
        {
            _root = new ColumnView { Gap = 2 };
            _root.Children.BindChildren(
                () => Enumerable.Range(0, _version.Value % 8 + 1),
                i => MakeRow((uint)(0xFF000000 | (uint)(i * 40503u))));
        }

        public string Name => "derivedchurn";
        public View BuildRoot(ICanvas canvas) => _root;
        public void Tick(long frame) => _version.Value = (int)frame;
    }
}
