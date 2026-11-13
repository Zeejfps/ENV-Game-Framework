using System.Globalization;
using System.Text;
using ZGF.Geometry;
using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

/// <summary>Pins every draw call of the stock buttons, scroll bars and tabs, captured before their painting
/// moved out of their behavior, so a theme without painters draws exactly what it did.</summary>
public sealed class StockLookTests
{
    private static void Buttons(Ui ui)
    {
        using var screen = ui.Screen();
        using (ui.Row())
        {
            ui.Button("Neutral");
            ui.Button("Primary", variant: ButtonVariant.Primary);
            ui.Button("Ghost", variant: ButtonVariant.Ghost);
            ui.Button("Off", enabled: false, variant: ButtonVariant.Primary);
            ui.Button("Busy", busy: true);
        }
        using (ui.Row())
        {
            ui.Button("Iconed", icon: new UiIcon(IconGlyph.FromFont("+")));
            ui.Button("Tinted", icon: new UiIcon(IconGlyph.FromFont("*"), Color: 0xFF112233), enabled: false);
            ui.Button("A label far too long for it", width: 80f);
            ui.IconButton("ghost", IconGlyph.FromFont("<"));
            ui.IconButton("neutral", IconGlyph.FromFont(">"), variant: ButtonVariant.Neutral);
            ui.IconButton("off", IconGlyph.FromFont("x"), enabled: false);
        }
    }

    private static void Scrolls(Ui ui)
    {
        using var screen = ui.Screen();
        using (ui.Row(new Box { Height = 120f }))
        {
            using (ui.Scroll("column", new Box { Width = 150f, Height = 120f }))
                ui.Leaf(new Box { Width = Sizing.Grow(), Height = 500f });
            using (ui.Scroll("both", ScrollAxes.Both, new Box { Width = 150f, Height = 120f }))
                ui.Leaf(new Box { Width = 400f, Height = 300f });
        }
        using (ui.Scroll("row", new Box { Direction = Direction.Horizontal, Width = 200f }))
            ui.Leaf(new Box { Width = 600f, Height = 20f });
        using var list = ui.VirtualScroll("list", 100, 20f, new Box { Height = 80f });
        for (var i = list.First; i < list.End; i++)
            ui.Leaf(new Box { Width = Sizing.Grow(), Height = 20f });
    }

    private static readonly DockLayout TabsLayout = DockLayout.Split(Direction.Horizontal, 0.5f, DockLayout.Group("one", "two", "three"),
        DockLayout.Group("solo")) is var root ? new DockLayout { Root = root } : throw new InvalidOperationException();

    private static void Tabs(Ui ui)
    {
        using var screen = ui.Screen();
        using (var strip = ui.TabStrip("strip"))
        {
            strip.Tab("Alpha", selected: true);
            strip.Tab("Beta", selected: false);
            strip.Tab("Gamma", selected: false, closable: false, icon: new UiIcon(IconGlyph.FromFont("*")));
            strip.Tab("Delta", selected: true, current: false, closable: false);
        }
        using var dock = ui.DockSpace("dock", TabsLayout, static p => new DockTab(p.ToUpperInvariant(), Closable: p != "three"));
        while (dock.Next(out var panel))
            ui.Text(panel);
    }

    [Fact]
    public void Tabs_at_rest_hovered_and_focused_draw_as_before()
    {
        var frames = new UiFrames(new DefaultUiState(), width: 800f, height: 300f, body: Tabs).Frame(3);
        var dump = new StringBuilder();
        Dump(frames, "rest", dump);

        frames.MovePointer(frames.RectOf("Beta")).Frame(2);
        Dump(frames, "hover tab", dump);
        var close = frames.RectOf(ui => UiWidgets.CloseButtonId(ui.GetId("Beta")));
        frames.MovePointer(close).Frame(2);
        Dump(frames, "hover close", dump);
        frames.MovePointer(700f, 290f).Key(KeyboardKey.Tab).Frame(2);
        Dump(frames, "focus", dump);

        Assert.Equal(ExpectedTabs.ReplaceLineEndings("\n"), dump.ToString().TrimEnd());
    }

    [Fact]
    public void Buttons_at_rest_hovered_pressed_and_focused_draw_as_before()
    {
        var frames = new UiFrames(new DefaultUiState(), width: 800f, body: Buttons).Frame(2);
        var dump = new StringBuilder();
        Dump(frames, "rest", dump);

        frames.MovePointer(frames.RectOf("Primary")).Frame();
        Dump(frames, "hover primary", dump);
        frames.MovePointer(frames.RectOf("Neutral")).Press().Frame();
        Dump(frames, "press neutral", dump);
        frames.Release().Frame();
        frames.MovePointer(frames.RectOf("ghost")).Frame();
        Dump(frames, "hover icon", dump);
        frames.MovePointer(700f, 290f).Key(KeyboardKey.Tab).Frame();
        Dump(frames, "focus", dump);
        frames.Key(KeyboardKey.Enter).Frame();
        Dump(frames, "enter", dump);

        Assert.Equal(ExpectedButtons.ReplaceLineEndings("\n"), dump.ToString().TrimEnd());
    }

    [Fact]
    public void Scroll_bars_at_rest_hovered_and_dragged_draw_as_before()
    {
        var frames = new UiFrames(new DefaultUiState(), width: 400f, height: 400f, body: Scrolls).Frame(3);
        var dump = new StringBuilder();
        Dump(frames, "rest", dump);

        frames.MovePointer(frames.RectOf("row")).Frame(2);
        Dump(frames, "over row", dump);
        var thumb = frames.RectOf(ui => Ui.VerticalThumbId(ui.GetId("column")));
        frames.MovePointer(thumb).Frame();
        Dump(frames, "hover thumb", dump);
        frames.Press().Frame();
        frames.MovePointer(thumb.X + thumb.Width / 2, thumb.Y + thumb.Height / 2 + 30f).Frame(2);
        Dump(frames, "drag thumb", dump);
        frames.Release().Frame();
        frames.MovePointer(frames.RectOf("both")).Wheel(-2, -1).Frame(2);
        Dump(frames, "wheel both", dump);

        Assert.Equal(ExpectedScrolls.ReplaceLineEndings("\n"), dump.ToString().TrimEnd());
    }

    private static void Dump(UiFrames frames, string step, StringBuilder dump)
    {
        dump.Append("# ").Append(step).Append('\n');
        var calls = new List<(int Sequence, string Line)>();
        foreach (var r in frames.Canvas.Rects)
        {
            var s = r.Inputs.Style;
            calls.Add((r.Sequence, $"R {Rect(r.Inputs.Position)} {s.BackgroundColor:X8} {s.BorderColor.Top.Value:X8} {N(s.BorderSize.Top.Value)} {N(s.BorderRadius.TopLeft.Value)} {Clip(r.Clip)}"));
        }
        foreach (var t in frames.Canvas.Texts)
            calls.Add((t.Sequence, $"T {t.Inputs.Text} {Rect(t.Inputs.Position)} {t.Inputs.Style.TextColor.Value:X8}"));
        foreach (var c in frames.Canvas.Circles)
            calls.Add((c.Sequence, $"C {N(c.Inputs.Center.X)},{N(c.Inputs.Center.Y)} {N(c.Inputs.Radius)} {c.Inputs.Color:X8}"));
        foreach (var call in calls.OrderBy(c => c.Sequence))
            dump.Append(call.Line).Append('\n');
    }

    private static string Rect(RectF r) => $"{N(r.Left)},{N(r.Bottom)},{N(r.Width)},{N(r.Height)}";

    private static string Clip(RectF? clip) => clip == null ? "-" : Rect(clip.Value);

    private static string N(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private const string ExpectedTabs = """
        # rest
        R 0,0,800,300 FF1B1E24 00000000 0 0 -
        R 16,248,768,36 FF22252C 00000000 0 0 -
        R 16,248,88,36 FF191C22 00000000 0 0 16,248,768,36
        R 16,282,88,2 FF356FE0 00000000 0 0 16,248,768,36
        R 103,248,1,36 FF343A45 00000000 0 0 16,248,768,36
        T Alpha 28,258,40,16 FFE4E7ED
        R 183,248,1,36 FF343A45 00000000 0 0 16,248,768,36
        T Beta 116,258,32,16 FFA6ACB8
        R 267,248,1,36 FF343A45 00000000 0 0 16,248,768,36
        T * 200,258,8,16 FFA6ACB8
        T Gamma 216,258,40,16 FFA6ACB8
        R 268,248,80,36 FF191C22 00000000 0 0 16,248,768,36
        R 268,282,80,2 FF8B95A5 00000000 0 0 16,248,768,36
        R 347,248,1,36 FF343A45 00000000 0 0 16,248,768,36
        T Delta 280,258,40,16 FFE4E7ED
        R 16,16,384,224 FF191C22 00000000 0 0 16,16,384,224
        R 16,204,384,36 FF22252C 00000000 0 0 16,16,384,224
        R 16,204,80,36 FF191C22 00000000 0 0 16,204,384,36
        R 16,238,80,2 FF356FE0 00000000 0 0 16,204,384,36
        R 95,204,1,36 FF343A45 00000000 0 0 16,204,384,36
        T ONE 28,214,24,16 FFE4E7ED
        R 175,204,1,36 FF343A45 00000000 0 0 16,204,384,36
        T TWO 108,214,24,16 FFA6ACB8
        R 255,204,1,36 FF343A45 00000000 0 0 16,204,384,36
        T THREE 188,214,40,16 FFA6ACB8
        T one 16,188,24,16 FFE4E7ED
        R 402,16,1,224 FF343A45 00000000 0 0 -
        R 406,16,378,224 FF191C22 00000000 0 0 406,16,378,224
        R 406,204,378,36 FF22252C 00000000 0 0 406,16,378,224
        R 406,204,80,36 FF191C22 00000000 0 0 406,204,378,36
        R 406,238,80,2 FF356FE0 00000000 0 0 406,204,378,36
        R 485,204,1,36 FF343A45 00000000 0 0 406,204,378,36
        T SOLO 418,214,32,16 FFE4E7ED
        T solo 406,188,32,16 FFE4E7ED
        # hover tab
        R 0,0,800,300 FF1B1E24 00000000 0 0 -
        R 16,248,768,36 FF22252C 00000000 0 0 -
        R 16,248,88,36 FF191C22 00000000 0 0 16,248,768,36
        R 16,282,88,2 FF356FE0 00000000 0 0 16,248,768,36
        R 103,248,1,36 FF343A45 00000000 0 0 16,248,768,36
        T Alpha 28,258,40,16 FFE4E7ED
        R 104,248,80,36 FF2B303A 00000000 0 0 16,248,768,36
        R 183,248,1,36 FF343A45 00000000 0 0 16,248,768,36
        T Beta 116,258,32,16 FFA6ACB8
        R 267,248,1,36 FF343A45 00000000 0 0 16,248,768,36
        T * 200,258,8,16 FFA6ACB8
        T Gamma 216,258,40,16 FFA6ACB8
        R 268,248,80,36 FF191C22 00000000 0 0 16,248,768,36
        R 268,282,80,2 FF8B95A5 00000000 0 0 16,248,768,36
        R 347,248,1,36 FF343A45 00000000 0 0 16,248,768,36
        T Delta 280,258,40,16 FFE4E7ED
        R 16,16,384,224 FF191C22 00000000 0 0 16,16,384,224
        R 16,204,384,36 FF22252C 00000000 0 0 16,16,384,224
        R 16,204,80,36 FF191C22 00000000 0 0 16,204,384,36
        R 16,238,80,2 FF356FE0 00000000 0 0 16,204,384,36
        R 95,204,1,36 FF343A45 00000000 0 0 16,204,384,36
        T ONE 28,214,24,16 FFE4E7ED
        R 175,204,1,36 FF343A45 00000000 0 0 16,204,384,36
        T TWO 108,214,24,16 FFA6ACB8
        R 255,204,1,36 FF343A45 00000000 0 0 16,204,384,36
        T THREE 188,214,40,16 FFA6ACB8
        T one 16,188,24,16 FFE4E7ED
        R 402,16,1,224 FF343A45 00000000 0 0 -
        R 406,16,378,224 FF191C22 00000000 0 0 406,16,378,224
        R 406,204,378,36 FF22252C 00000000 0 0 406,16,378,224
        R 406,204,80,36 FF191C22 00000000 0 0 406,204,378,36
        R 406,238,80,2 FF356FE0 00000000 0 0 406,204,378,36
        R 485,204,1,36 FF343A45 00000000 0 0 406,204,378,36
        T SOLO 418,214,32,16 FFE4E7ED
        T solo 406,188,32,16 FFE4E7ED
        # hover close
        R 0,0,800,300 FF1B1E24 00000000 0 0 -
        R 16,248,768,36 FF22252C 00000000 0 0 -
        R 16,248,88,36 FF191C22 00000000 0 0 16,248,768,36
        R 16,282,88,2 FF356FE0 00000000 0 0 16,248,768,36
        R 103,248,1,36 FF343A45 00000000 0 0 16,248,768,36
        T Alpha 28,258,40,16 FFE4E7ED
        R 104,248,80,36 FF2B303A 00000000 0 0 16,248,768,36
        R 183,248,1,36 FF343A45 00000000 0 0 16,248,768,36
        T Beta 116,258,32,16 FFA6ACB8
        R 156,254,24,24 FF3A414D 00000000 0 4 16,248,768,36
        R 267,248,1,36 FF343A45 00000000 0 0 16,248,768,36
        T * 200,258,8,16 FFA6ACB8
        T Gamma 216,258,40,16 FFA6ACB8
        R 268,248,80,36 FF191C22 00000000 0 0 16,248,768,36
        R 268,282,80,2 FF8B95A5 00000000 0 0 16,248,768,36
        R 347,248,1,36 FF343A45 00000000 0 0 16,248,768,36
        T Delta 280,258,40,16 FFE4E7ED
        R 16,16,384,224 FF191C22 00000000 0 0 16,16,384,224
        R 16,204,384,36 FF22252C 00000000 0 0 16,16,384,224
        R 16,204,80,36 FF191C22 00000000 0 0 16,204,384,36
        R 16,238,80,2 FF356FE0 00000000 0 0 16,204,384,36
        R 95,204,1,36 FF343A45 00000000 0 0 16,204,384,36
        T ONE 28,214,24,16 FFE4E7ED
        R 175,204,1,36 FF343A45 00000000 0 0 16,204,384,36
        T TWO 108,214,24,16 FFA6ACB8
        R 255,204,1,36 FF343A45 00000000 0 0 16,204,384,36
        T THREE 188,214,40,16 FFA6ACB8
        T one 16,188,24,16 FFE4E7ED
        R 402,16,1,224 FF343A45 00000000 0 0 -
        R 406,16,378,224 FF191C22 00000000 0 0 406,16,378,224
        R 406,204,378,36 FF22252C 00000000 0 0 406,16,378,224
        R 406,204,80,36 FF191C22 00000000 0 0 406,204,378,36
        R 406,238,80,2 FF356FE0 00000000 0 0 406,204,378,36
        R 485,204,1,36 FF343A45 00000000 0 0 406,204,378,36
        T SOLO 418,214,32,16 FFE4E7ED
        T solo 406,188,32,16 FFE4E7ED
        # focus
        R 0,0,800,300 FF1B1E24 00000000 0 0 -
        R 16,248,768,36 FF22252C 00000000 0 0 -
        R 16,248,88,36 FF191C22 00000000 0 0 16,248,768,36
        R 16,282,88,2 FF356FE0 00000000 0 0 16,248,768,36
        R 103,248,1,36 FF343A45 00000000 0 0 16,248,768,36
        R 16,248,88,36 00000000 FF7AA7FF 2 0 16,248,768,36
        T Alpha 28,258,40,16 FFE4E7ED
        R 183,248,1,36 FF343A45 00000000 0 0 16,248,768,36
        T Beta 116,258,32,16 FFA6ACB8
        R 267,248,1,36 FF343A45 00000000 0 0 16,248,768,36
        T * 200,258,8,16 FFA6ACB8
        T Gamma 216,258,40,16 FFA6ACB8
        R 268,248,80,36 FF191C22 00000000 0 0 16,248,768,36
        R 268,282,80,2 FF8B95A5 00000000 0 0 16,248,768,36
        R 347,248,1,36 FF343A45 00000000 0 0 16,248,768,36
        T Delta 280,258,40,16 FFE4E7ED
        R 16,16,384,224 FF191C22 00000000 0 0 16,16,384,224
        R 16,204,384,36 FF22252C 00000000 0 0 16,16,384,224
        R 16,204,80,36 FF191C22 00000000 0 0 16,204,384,36
        R 16,238,80,2 FF356FE0 00000000 0 0 16,204,384,36
        R 95,204,1,36 FF343A45 00000000 0 0 16,204,384,36
        T ONE 28,214,24,16 FFE4E7ED
        R 175,204,1,36 FF343A45 00000000 0 0 16,204,384,36
        T TWO 108,214,24,16 FFA6ACB8
        R 255,204,1,36 FF343A45 00000000 0 0 16,204,384,36
        T THREE 188,214,40,16 FFA6ACB8
        T one 16,188,24,16 FFE4E7ED
        R 402,16,1,224 FF343A45 00000000 0 0 -
        R 406,16,378,224 FF191C22 00000000 0 0 406,16,378,224
        R 406,204,378,36 FF22252C 00000000 0 0 406,16,378,224
        R 406,204,80,36 FF191C22 00000000 0 0 406,204,378,36
        R 406,238,80,2 FF356FE0 00000000 0 0 406,204,378,36
        R 485,204,1,36 FF343A45 00000000 0 0 406,204,378,36
        T SOLO 418,214,32,16 FFE4E7ED
        T solo 406,188,32,16 FFE4E7ED
        """;

    private const string ExpectedButtons = """
        # rest
        R 0,0,800,300 FF1B1E24 00000000 0 0 -
        R 16,252,80,32 FF303641 FF4B5362 1 4 -
        T Neutral 28,260,56,16 FFE4E7ED
        R 104,252,80,32 FF356FE0 00000000 0 4 -
        T Primary 116,260,56,16 FFFFFFFF
        T Ghost 204,260,40,16 FFE4E7ED
        R 264,252,48,32 FF303641 00000000 0 4 -
        T Off 276,260,24,16 FF747D8C
        R 320,252,76,32 FF303641 FF4B5362 1 4 -
        C 346.08,268 1.44 FFE4E7ED
        C 344.3,263.7 1.44 FF5E636D
        C 340,261.92 1.44 FF71767F
        C 335.7,263.7 1.44 FF848992
        C 333.92,268 1.44 FF989CA4
        C 335.7,272.3 1.44 FFABAFB6
        C 340,274.08 1.44 FFBEC1C8
        C 344.3,272.3 1.44 FFD1D4DB
        T Busy 352,260,32,16 FFE4E7ED
        R 16,212,92,32 FF303641 FF4B5362 1 4 -
        T + 32,220,8,16 FFE4E7ED
        T Iconed 48,220,48,16 FFE4E7ED
        R 116,212,92,32 FF303641 00000000 0 4 -
        T * 132,220,8,16 FF747D8C
        T Tinted 148,220,48,16 FF747D8C
        R 216,212,80,32 FF303641 FF4B5362 1 4 -
        T A labe… 228,220,56,16 FFE4E7ED
        T < 312,220,16,16 FFE4E7ED
        R 344,212,32,32 FF303641 FF4B5362 1 4 -
        T > 352,220,16,16 FFE4E7ED
        T x 392,220,16,16 FF747D8C
        # hover primary
        R 0,0,800,300 FF1B1E24 00000000 0 0 -
        R 16,252,80,32 FF303641 FF4B5362 1 4 -
        T Neutral 28,260,56,16 FFE4E7ED
        R 104,252,80,32 FF4079E8 00000000 0 4 -
        T Primary 116,260,56,16 FFFFFFFF
        T Ghost 204,260,40,16 FFE4E7ED
        R 264,252,48,32 FF303641 00000000 0 4 -
        T Off 276,260,24,16 FF747D8C
        R 320,252,76,32 FF303641 FF4B5362 1 4 -
        C 346.08,268 1.44 FFE4E7ED
        C 344.3,263.7 1.44 FF5E636D
        C 340,261.92 1.44 FF71767F
        C 335.7,263.7 1.44 FF848992
        C 333.92,268 1.44 FF989CA4
        C 335.7,272.3 1.44 FFABAFB6
        C 340,274.08 1.44 FFBEC1C8
        C 344.3,272.3 1.44 FFD1D4DB
        T Busy 352,260,32,16 FFE4E7ED
        R 16,212,92,32 FF303641 FF4B5362 1 4 -
        T + 32,220,8,16 FFE4E7ED
        T Iconed 48,220,48,16 FFE4E7ED
        R 116,212,92,32 FF303641 00000000 0 4 -
        T * 132,220,8,16 FF747D8C
        T Tinted 148,220,48,16 FF747D8C
        R 216,212,80,32 FF303641 FF4B5362 1 4 -
        T A labe… 228,220,56,16 FFE4E7ED
        T < 312,220,16,16 FFE4E7ED
        R 344,212,32,32 FF303641 FF4B5362 1 4 -
        T > 352,220,16,16 FFE4E7ED
        T x 392,220,16,16 FF747D8C
        # press neutral
        R 0,0,800,300 FF1B1E24 00000000 0 0 -
        R 16,252,80,32 FF424B59 FF4B5362 1 4 -
        T Neutral 28,260,56,16 FFE4E7ED
        R 104,252,80,32 FF356FE0 00000000 0 4 -
        T Primary 116,260,56,16 FFFFFFFF
        T Ghost 204,260,40,16 FFE4E7ED
        R 264,252,48,32 FF303641 00000000 0 4 -
        T Off 276,260,24,16 FF747D8C
        R 320,252,76,32 FF303641 FF4B5362 1 4 -
        C 346.08,268 1.44 FFE4E7ED
        C 344.3,263.7 1.44 FF5E636D
        C 340,261.92 1.44 FF71767F
        C 335.7,263.7 1.44 FF848992
        C 333.92,268 1.44 FF989CA4
        C 335.7,272.3 1.44 FFABAFB6
        C 340,274.08 1.44 FFBEC1C8
        C 344.3,272.3 1.44 FFD1D4DB
        T Busy 352,260,32,16 FFE4E7ED
        R 16,212,92,32 FF303641 FF4B5362 1 4 -
        T + 32,220,8,16 FFE4E7ED
        T Iconed 48,220,48,16 FFE4E7ED
        R 116,212,92,32 FF303641 00000000 0 4 -
        T * 132,220,8,16 FF747D8C
        T Tinted 148,220,48,16 FF747D8C
        R 216,212,80,32 FF303641 FF4B5362 1 4 -
        T A labe… 228,220,56,16 FFE4E7ED
        T < 312,220,16,16 FFE4E7ED
        R 344,212,32,32 FF303641 FF4B5362 1 4 -
        T > 352,220,16,16 FFE4E7ED
        T x 392,220,16,16 FF747D8C
        # hover icon
        R 0,0,800,300 FF1B1E24 00000000 0 0 -
        R 16,252,80,32 FF303641 FF4B5362 1 4 -
        T Neutral 28,260,56,16 FFE4E7ED
        R 104,252,80,32 FF356FE0 00000000 0 4 -
        T Primary 116,260,56,16 FFFFFFFF
        T Ghost 204,260,40,16 FFE4E7ED
        R 264,252,48,32 FF303641 00000000 0 4 -
        T Off 276,260,24,16 FF747D8C
        R 320,252,76,32 FF303641 FF4B5362 1 4 -
        C 346.08,268 1.44 FFE4E7ED
        C 344.3,263.7 1.44 FF5E636D
        C 340,261.92 1.44 FF71767F
        C 335.7,263.7 1.44 FF848992
        C 333.92,268 1.44 FF989CA4
        C 335.7,272.3 1.44 FFABAFB6
        C 340,274.08 1.44 FFBEC1C8
        C 344.3,272.3 1.44 FFD1D4DB
        T Busy 352,260,32,16 FFE4E7ED
        R 16,212,92,32 FF303641 FF4B5362 1 4 -
        T + 32,220,8,16 FFE4E7ED
        T Iconed 48,220,48,16 FFE4E7ED
        R 116,212,92,32 FF303641 00000000 0 4 -
        T * 132,220,8,16 FF747D8C
        T Tinted 148,220,48,16 FF747D8C
        R 216,212,80,32 FF303641 FF4B5362 1 4 -
        T A labe… 228,220,56,16 FFE4E7ED
        R 304,212,32,32 FF3A414D 00000000 0 4 -
        T < 312,220,16,16 FFE4E7ED
        R 344,212,32,32 FF303641 FF4B5362 1 4 -
        T > 352,220,16,16 FFE4E7ED
        T x 392,220,16,16 FF747D8C
        # focus
        R 0,0,800,300 FF1B1E24 00000000 0 0 -
        R 16,252,80,32 FF303641 FF4B5362 1 4 -
        T Neutral 28,260,56,16 FFE4E7ED
        R 104,252,80,32 FF356FE0 00000000 0 4 -
        T Primary 116,260,56,16 FFFFFFFF
        T Ghost 204,260,40,16 FFE4E7ED
        R 264,252,48,32 FF303641 00000000 0 4 -
        T Off 276,260,24,16 FF747D8C
        R 320,252,76,32 FF303641 FF4B5362 1 4 -
        C 346.08,268 1.44 FFE4E7ED
        C 344.3,263.7 1.44 FF5E636D
        C 340,261.92 1.44 FF71767F
        C 335.7,263.7 1.44 FF848992
        C 333.92,268 1.44 FF989CA4
        C 335.7,272.3 1.44 FFABAFB6
        C 340,274.08 1.44 FFBEC1C8
        C 344.3,272.3 1.44 FFD1D4DB
        T Busy 352,260,32,16 FFE4E7ED
        R 16,212,92,32 FF303641 FF4B5362 1 4 -
        T + 32,220,8,16 FFE4E7ED
        T Iconed 48,220,48,16 FFE4E7ED
        R 116,212,92,32 FF303641 00000000 0 4 -
        T * 132,220,8,16 FF747D8C
        T Tinted 148,220,48,16 FF747D8C
        R 216,212,80,32 FF303641 FF4B5362 1 4 -
        T A labe… 228,220,56,16 FFE4E7ED
        T < 312,220,16,16 FFE4E7ED
        R 344,212,32,32 FF303641 FF4B5362 1 4 -
        T > 352,220,16,16 FFE4E7ED
        T x 392,220,16,16 FF747D8C
        # enter
        R 0,0,800,300 FF1B1E24 00000000 0 0 -
        R 16,252,80,32 FF424B59 FF4B5362 1 4 -
        R 16,252,80,32 00000000 FF7AA7FF 2 4 -
        T Neutral 28,260,56,16 FFE4E7ED
        R 104,252,80,32 FF356FE0 00000000 0 4 -
        T Primary 116,260,56,16 FFFFFFFF
        T Ghost 204,260,40,16 FFE4E7ED
        R 264,252,48,32 FF303641 00000000 0 4 -
        T Off 276,260,24,16 FF747D8C
        R 320,252,76,32 FF303641 FF4B5362 1 4 -
        C 346.08,268 1.44 FFE4E7ED
        C 344.3,263.7 1.44 FF5E636D
        C 340,261.92 1.44 FF71767F
        C 335.7,263.7 1.44 FF848992
        C 333.92,268 1.44 FF989CA4
        C 335.7,272.3 1.44 FFABAFB6
        C 340,274.08 1.44 FFBEC1C8
        C 344.3,272.3 1.44 FFD1D4DB
        T Busy 352,260,32,16 FFE4E7ED
        R 16,212,92,32 FF303641 FF4B5362 1 4 -
        T + 32,220,8,16 FFE4E7ED
        T Iconed 48,220,48,16 FFE4E7ED
        R 116,212,92,32 FF303641 00000000 0 4 -
        T * 132,220,8,16 FF747D8C
        T Tinted 148,220,48,16 FF747D8C
        R 216,212,80,32 FF303641 FF4B5362 1 4 -
        T A labe… 228,220,56,16 FFE4E7ED
        T < 312,220,16,16 FFE4E7ED
        R 344,212,32,32 FF303641 FF4B5362 1 4 -
        T > 352,220,16,16 FFE4E7ED
        T x 392,220,16,16 FF747D8C
        """;

    private const string ExpectedScrolls = """
        # rest
        R 0,0,400,400 FF1B1E24 00000000 0 0 -
        R 156,355.2,8,28.8 FF626D80 00000000 0 4 16,264,150,120
        R 314,346.62,8,37.38 FF626D80 00000000 0 4 174,264,150,120
        R 174,266,46.22,8 FF626D80 00000000 0 4 174,264,150,120
        R 374,204,8,24 FF626D80 00000000 0 4 16,148,368,80
        # over row
        R 0,0,400,400 FF1B1E24 00000000 0 0 -
        R 156,355.2,8,28.8 FF626D80 00000000 0 4 16,264,150,120
        R 314,346.62,8,37.38 FF626D80 00000000 0 4 174,264,150,120
        R 174,266,46.22,8 FF626D80 00000000 0 4 174,264,150,120
        R 16,238,66.67,8 FF626D80 00000000 0 4 16,236,200,20
        R 374,204,8,24 FF626D80 00000000 0 4 16,148,368,80
        # hover thumb
        R 0,0,400,400 FF1B1E24 00000000 0 0 -
        R 156,355.2,8,28.8 FF7B879B 00000000 0 4 16,264,150,120
        R 314,346.62,8,37.38 FF626D80 00000000 0 4 174,264,150,120
        R 174,266,46.22,8 FF626D80 00000000 0 4 174,264,150,120
        R 374,204,8,24 FF626D80 00000000 0 4 16,148,368,80
        # drag thumb
        R 0,0,400,400 FF1B1E24 00000000 0 0 -
        R 156,325.2,8,28.8 FF95A4BD 00000000 0 4 16,264,150,120
        R 314,346.62,8,37.38 FF626D80 00000000 0 4 174,264,150,120
        R 174,266,46.22,8 FF626D80 00000000 0 4 174,264,150,120
        R 374,204,8,24 FF626D80 00000000 0 4 16,148,368,80
        # wheel both
        R 0,0,400,400 FF1B1E24 00000000 0 0 -
        R 156,325.2,8,28.8 FF626D80 00000000 0 4 16,264,150,120
        R 314,317.19,8,37.38 FF626D80 00000000 0 4 174,264,150,120
        R 188.01,266,46.22,8 FF626D80 00000000 0 4 174,264,150,120
        R 374,204,8,24 FF626D80 00000000 0 4 16,148,368,80
        """;
}
