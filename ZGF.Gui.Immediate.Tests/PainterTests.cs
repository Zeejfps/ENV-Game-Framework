using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class ButtonPainterTests
{
    private const uint Pill = 0xFF17CDFF;
    private const uint PillHovered = 0xFF45D7FF;
    private const uint PillDown = 0xFF0FA8D2;
    private const uint Ink = 0xFF04121A;
    private const float PillHeight = 40f;

    private readonly Dictionary<string, ButtonState> _states = new();
    private readonly Dictionary<string, IconButtonFace> _iconFaces = new();
    private readonly List<string> _clicked = new();
    private readonly UiFrames _frames;
    private Action<Ui> _buttons = static _ => { };

    public ButtonPainterTests()
    {
        var state = new DefaultUiState
        {
            Theme = UiTheme.Dark with { ButtonPainter = PaintPill, IconButtonPainter = PaintRoundIcon },
        };
        _frames = new UiFrames(state, width: 600f, body: ui =>
        {
            using var screen = ui.Screen();
            using (ui.Row())
                _buttons(ui);
        });
    }

    private void PaintPill(Ui ui, in ButtonState state, in ButtonFace face)
    {
        _states[face.Label] = state;
        var fill = state.Down ? PillDown : state.Hovered ? PillHovered : Pill;
        using (ui.Row(state.Id, new Box
        {
            Width = Sizing.Fit(),
            Height = PillHeight,
            Padding = new Spacing(PillHeight / 2, 0f),
            Background = fill,
            Radius = PillHeight / 2,
        }))
        {
            ui.Text(face.Label.ToUpperInvariant(), color: Ink);
            if (state.FocusVisible) ui.FocusRing(PillHeight / 2);
        }
    }

    private void PaintRoundIcon(Ui ui, in ButtonState state, in IconButtonFace face)
    {
        _iconFaces[face.Glyph.Text!] = face;
        _states[face.Glyph.Text!] = state;
        using (ui.Box(state.Id, new Box
        {
            Width = PillHeight,
            Height = PillHeight,
            AlignX = Align.Center,
            AlignY = Align.Center,
            Background = state.Hovered ? PillHovered : 0u,
            Radius = PillHeight / 2,
        }))
            ui.Glyph(face.Glyph, Ink, 20f, new Box { Width = 20f, Height = 20f });
    }

    private void Buttons(Action<Ui> buttons) => _buttons = buttons;

    private void Button(Ui ui, string label, bool enabled = true, bool busy = false, ButtonVariant variant = ButtonVariant.Neutral)
    {
        if (ui.Button(label, enabled: enabled, busy: busy, variant: variant))
            _clicked.Add(label);
    }

    private bool HasStockFill(UiTheme theme) => _frames.Canvas.Rects.Any(r =>
        r.Inputs.Style.BackgroundColor == theme.Control || r.Inputs.Style.BackgroundColor == theme.Accent
        || r.Inputs.Style.BorderColor.Top.Value == theme.Border);

    [Fact]
    public void A_theme_painter_draws_every_button_and_the_stock_look_is_not_drawn()
    {
        Buttons(ui =>
        {
            Button(ui, "Save");
            Button(ui, "Open", variant: ButtonVariant.Primary);
        });
        _frames.Frame(2);

        Assert.True(_frames.Drew("SAVE"));
        Assert.True(_frames.Drew("OPEN"));
        Assert.False(_frames.Drew("Save"));
        Assert.False(HasStockFill(UiTheme.Dark));
        Assert.Equal(2, _frames.Canvas.Rects.Count(r => r.Inputs.Style.BackgroundColor == Pill));
        Assert.Equal(PillHeight, _frames.RectOf("Save").Height);
    }

    [Fact]
    public void A_painted_button_is_hovered_pressed_and_clicked_as_a_stock_one_is()
    {
        Buttons(ui => Button(ui, "Save"));
        _frames.Frame(2);
        Assert.False(_states["Save"].Hovered);

        _frames.MovePointer(_frames.RectOf("Save")).Frame();
        Assert.True(_states["Save"].Hovered);
        Assert.False(_states["Save"].Down);
        Assert.Equal(MouseCursor.Hand, _frames.Cursor);
        Assert.Contains(_frames.Canvas.Rects, r => r.Inputs.Style.BackgroundColor == PillHovered);

        _frames.Press().Frame();
        Assert.True(_states["Save"].Down);
        Assert.Contains(_frames.Canvas.Rects, r => r.Inputs.Style.BackgroundColor == PillDown);
        Assert.Empty(_clicked);

        _frames.Release().Frame();
        Assert.True(_states["Save"].Activated);
        Assert.Equal(["Save"], _clicked);
    }

    [Fact]
    public void Tab_shows_a_painted_button_its_visible_focus_and_Enter_or_Space_activate_it()
    {
        Buttons(ui =>
        {
            Button(ui, "One");
            Button(ui, "Two");
        });
        _frames.Frame();

        _frames.Key(KeyboardKey.Tab).Frame().Frame();
        Assert.True(_states["One"].FocusVisible);
        Assert.False(_states["Two"].FocusVisible);
        Assert.Contains(_frames.Canvas.Rects, r => r.Inputs.Style.BorderColor.Top.Value == UiTheme.Dark.FocusRing);

        _frames.Key(KeyboardKey.Enter).Frame();
        Assert.True(_states["One"].Down);
        Assert.True(_states["One"].Activated);
        _frames.Key(KeyboardKey.Tab).Frame().Key(KeyboardKey.Space).Frame();

        Assert.Equal(["One", "Two"], _clicked);
    }

    [Fact]
    public void A_disabled_or_busy_painted_button_says_which_and_takes_no_input()
    {
        Buttons(ui =>
        {
            Button(ui, "Off", enabled: false);
            Button(ui, "Wait", busy: true);
        });
        _frames.Frame(2);

        _frames.ClickOn("Off").ClickOn("Wait");
        _frames.MovePointer(_frames.RectOf("Wait")).Frame();
        _frames.Key(KeyboardKey.Tab).Frame().Key(KeyboardKey.Enter).Frame();

        Assert.Empty(_clicked);
        Assert.Equal(ButtonAvailability.Disabled, _states["Off"].Availability);
        Assert.Equal(ButtonAvailability.Busy, _states["Wait"].Availability);
        Assert.False(_states["Wait"].Hovered);
        Assert.False(_states["Wait"].FocusVisible);
        Assert.Equal(MouseCursor.Default, _frames.Cursor);
    }

    [Fact]
    public void An_icon_button_is_drawn_by_the_icon_button_painter_and_keeps_its_tooltip()
    {
        Buttons(ui =>
        {
            if (ui.IconButton("next", IconGlyph.FromFont(">"), tooltip: "Next page", variant: ButtonVariant.Neutral))
                _clicked.Add("next");
        });
        _frames.Frame(2);

        Assert.Equal(ButtonVariant.Neutral, _iconFaces[">"].Variant);
        Assert.Equal(PillHeight, _frames.RectOf("next").Width);
        Assert.False(HasStockFill(UiTheme.Dark));

        _frames.MovePointer(_frames.RectOf("next")).Frame();
        _frames.Wait(1.0).Frame();
        Assert.True(_frames.Drew("Next page"));
        _frames.ClickOn("next");
        Assert.Equal(["next"], _clicked);
    }

    [Fact]
    public void A_painter_can_keep_the_stock_look_for_the_variants_it_does_not_restyle()
    {
        var theme = UiTheme.Dark with
        {
            ButtonPainter = (Ui ui, in ButtonState state, in ButtonFace face) =>
            {
                if (face.Variant == ButtonVariant.Primary)
                    PaintPill(ui, state, face);
                else
                    ui.PaintButton(state, face);
            },
        };
        var frames = new UiFrames(new DefaultUiState { Theme = theme }, body: ui =>
        {
            using var row = ui.Row();
            ui.Button("Cancel");
            ui.Button("Play", variant: ButtonVariant.Primary);
        }).Frame(2);

        Assert.True(frames.Drew("Cancel"));
        Assert.True(frames.Drew("PLAY"));
        Assert.Contains(frames.Canvas.Rects, r => r.Inputs.Style.BackgroundColor == theme.Control);
        Assert.DoesNotContain(frames.Canvas.Rects, r => r.Inputs.Style.BackgroundColor == theme.Accent);
    }
}

public sealed class ButtonBehaviorTests
{
    private ButtonState _state;
    private int _activations;

    private void Back(Ui ui)
    {
        using var screen = ui.Screen();
        _state = ui.ButtonBehavior("back");
        if (_state.Activated) _activations++;
        using (ui.Row(_state.Id, new Box
        {
            Width = Sizing.Fit(),
            Height = 36f,
            Padding = new Spacing(12f, 0f),
            Radius = 18f,
            Background = _state.Hovered ? 0xFF1A2A33 : 0u,
        }))
        {
            ui.Glyph(IconGlyph.FromFont("<"), 0xFF17CDFF, 16f, new Box { Width = 16f, Height = 16f });
            ui.Text("Back", color: 0xFF17CDFF);
        }
    }

    [Fact]
    public void An_app_painting_its_own_button_gets_hover_click_focus_and_keys_from_the_behavior()
    {
        var frames = new UiFrames(new DefaultUiState(), body: Back).Frame(2);

        frames.MovePointer(frames.RectOf("back")).Frame();
        Assert.True(_state.Hovered);
        Assert.Equal(MouseCursor.Hand, frames.Cursor);
        Assert.Contains(frames.Canvas.Rects, r => r.Inputs.Style.BackgroundColor == 0xFF1A2A33);

        frames.ClickOn("back");
        Assert.Equal(1, _activations);

        frames.MovePointer(500f, 290f).Key(KeyboardKey.Tab).Frame().Frame();
        Assert.True(_state.FocusVisible);
        frames.Key(KeyboardKey.Space).Frame();
        Assert.Equal(2, _activations);
        Assert.Single(frames.Canvas.Rects);
    }
}

public sealed class ScrollBarPainterTests
{
    private const uint Track = 0x3317CDFF;
    private const uint Thumb = 0xFF17CDFF;
    private const float ContentHeight = 400f;

    private readonly List<ScrollBarState> _bars = new();
    private readonly List<(Direction Axis, UiRect Track, UiRect Thumb)> _painted = new();
    private readonly UiFrames _frames;

    public ScrollBarPainterTests()
    {
        var state = new DefaultUiState { Theme = UiTheme.Dark with { ScrollBarPainter = PaintSlim } };
        _frames = new UiFrames(state, body: ui =>
        {
            using var screen = ui.Screen();
            using (ui.Scroll("list", new Box { Width = 200f, Height = 100f }))
                ui.Leaf(new Box { Width = Sizing.Grow(), Height = ContentHeight });
        });
        _frames.BeforeFrame = () =>
        {
            _bars.Clear();
            _painted.Clear();
        };
    }

    private void PaintSlim(Ui ui, in ScrollBarState bar)
    {
        _bars.Add(bar);
        ui.Paint((this, bar), static (p, s) => s.Item1.Painted(p, s.bar));
    }

    private void Painted(UiPainter p, ScrollBarState bar)
    {
        var track = bar.TrackRect(p.Rect);
        var thumb = bar.ThumbRect(p.Rect);
        _painted.Add((bar.Axis, track, thumb));
        p.FillRect(track, Track, bar.Thickness / 2);
        p.FillRect(thumb, Thumb, bar.Thickness / 2);
    }

    private UiRect ThumbHit() => _frames.RectOf(ui => Ui.VerticalThumbId(ui.GetId("list")));

    [Fact]
    public void The_painter_gets_the_track_and_thumb_of_the_bar_and_the_stock_thumb_is_not_drawn()
    {
        _frames.Frame(3);
        var theme = UiTheme.Dark;
        var list = _frames.RectOf("list");
        var lane = list.Right - theme.ScrollBarInset - theme.ScrollBarWidth;

        var (axis, track, thumb) = Assert.Single(_painted);
        Assert.Equal(Direction.Vertical, axis);
        Assert.Equal(new UiRect(lane, list.Y, theme.ScrollBarWidth, list.Height), track);
        Assert.Equal(new UiRect(lane, list.Y, theme.ScrollBarWidth, list.Height * list.Height / ContentHeight), thumb);
        Assert.Equal(0f, Assert.Single(_bars).Scrolled);
        Assert.DoesNotContain(_frames.Canvas.Rects, r => r.Inputs.Style.BackgroundColor == theme.ScrollThumb);
        Assert.Contains(_frames.Canvas.Rects, r => _frames.ToUi(r.Inputs.Position) == track && r.Inputs.Style.BackgroundColor == Track);

        _frames.Frame(ui => ui.ScrollTo(ui.GetId("list"), 0f, 1e9f));
        _frames.Frame(2);
        (_, track, thumb) = Assert.Single(_painted);
        Assert.Equal(track.Bottom, thumb.Bottom, 0.01f);
        Assert.Equal(1f, Assert.Single(_bars).Scrolled);
    }

    [Fact]
    public void Dragging_a_painted_thumb_still_scrolls_and_the_painter_sees_it_dragged()
    {
        _frames.Frame(3);
        var hit = ThumbHit();
        var travel = 100f - 100f * 100f / ContentHeight;
        var max = ContentHeight - 100f;

        _frames.MovePointer(hit).Frame();
        Assert.True(_bars[0].Hovered);
        _frames.Press().Frame();
        _frames.MovePointer(hit.X + hit.Width / 2, hit.Y + hit.Height / 2 + 30f);
        _frames.Frame(2);

        Assert.True(_bars[0].Dragged);
        Assert.Equal(30f * max / travel, _frames.Frame(ui => ui.ScrollOffset(ui.GetId("list"))).Y, 0.01f);
        Assert.Equal(30f, _painted[0].Thumb.Y - _painted[0].Track.Y, 0.01f);
    }

    [Fact]
    public void The_painter_knows_when_the_pointer_is_over_the_scroll_and_how_long_since_it_moved()
    {
        _frames.Frame(3);
        Assert.False(_bars[0].PointerWithin);
        Assert.Equal(double.PositiveInfinity, _bars[0].SinceMoved);

        _frames.MovePointer(_frames.RectOf("list"));
        _frames.Frame();
        Assert.True(_bars[0].PointerWithin);

        _frames.Wheel(-1);
        _frames.Frame();
        Assert.Equal(0.0, _bars[0].SinceMoved);

        _frames.Wait(0.5);
        _frames.Frame();
        Assert.Equal(0.5, _bars[0].SinceMoved, 6);
    }

    [Fact]
    public void A_virtual_scroll_both_ways_paints_both_bars_with_the_painter_clear_of_each_other()
    {
        float x = 0f, y = 0f;
        _frames.Body = ui =>
        {
            using var screen = ui.Screen();
            using var list = ui.VirtualScroll(ui.GetId("code"), 100, 20f, ref x, ref y, new Box { Width = 200f, Height = 100f });
            for (var i = list.First; i < list.End; i++)
                ui.Leaf(new Box { Width = 1000f, Height = 20f });
        };
        _frames.Frame(3);
        var view = _frames.RectOf("code");
        var gutter = UiTheme.Dark.ScrollBarWidth + UiTheme.Dark.ScrollBarInset * 2;

        var vertical = Assert.Single(_painted, p => p.Axis == Direction.Vertical);
        var horizontal = Assert.Single(_painted, p => p.Axis == Direction.Horizontal);
        Assert.Equal(view.Height - gutter, vertical.Track.Height, 0.01f);
        Assert.Equal(view.Width - gutter, horizontal.Track.Width, 0.01f);
        Assert.Equal(view.X, horizontal.Track.X, 0.01f);
        Assert.False(vertical.Track.Intersects(horizontal.Track));
    }
}

public sealed class PainterAllocationTests
{
    private static void Scene(Ui ui)
    {
        using var screen = ui.Screen();
        ui.Button("Save");
        ui.IconButton("next", Next);
        using (ui.Scroll("list", new Box { Width = 200f, Height = 100f }))
            ui.Leaf(new Box { Width = Sizing.Grow(), Height = 400f });
    }

    private static readonly IconGlyph Next = IconGlyph.FromFont(">");

    private static void PaintButton(Ui ui, in ButtonState state, in ButtonFace face)
    {
        using (ui.Row(state.Id, new Box { Width = Sizing.Fit(), Height = 40f, Background = state.Hovered ? 0xFF45D7FFu : 0xFF17CDFFu, Radius = 20f }))
            ui.Text(face.Label);
    }

    private static void PaintIconButton(Ui ui, in ButtonState state, in IconButtonFace face)
    {
        using (ui.Box(state.Id, new Box { Width = 40f, Height = 40f, Radius = 20f }))
            ui.Glyph(face.Glyph, 0xFF17CDFFu, 20f, new Box { Width = 20f, Height = 20f });
    }

    private static void PaintBar(Ui ui, in ScrollBarState bar) =>
        ui.Paint(bar, static (p, s) => p.FillRect(s.ThumbRect(p.Rect), s.Dragged ? 0xFF17CDFFu : 0x8017CDFFu, s.Thickness / 2));

    private static long AllocatedPerRun(UiTheme theme)
    {
        var frames = new UiFrames(new DefaultUiState { Theme = theme }, body: Scene);
        frames.MovePointer(30f, 30f);
        for (var i = 0; i < 4; i++)
            frames.Run(0f, Scene);
        frames.Canvas.Recording = false;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 20; i++)
            frames.Run(0f, Scene);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact]
    public void Stock_and_theme_painted_buttons_and_bars_allocate_nothing_per_frame()
    {
        var painted = UiTheme.Dark with { ButtonPainter = PaintButton, IconButtonPainter = PaintIconButton, ScrollBarPainter = PaintBar };

        Assert.Equal(0, AllocatedPerRun(UiTheme.Dark));
        Assert.Equal(0, AllocatedPerRun(painted));
    }
}
