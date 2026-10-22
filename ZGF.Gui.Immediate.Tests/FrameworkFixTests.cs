using System.Collections.Concurrent;
using ZGF.Desktop;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class VirtualScrollRequestTests
{
    private static (float Offset, int First) Frame(UiFrames frames, int count, Action<Ui.VirtualList>? inside = null) =>
        frames.Run(1f / 60f, ui =>
        {
            using var screen = ui.Screen();
            using var list = ui.VirtualScroll("rows", count, 20f, new Box { Gap = 0f });
            for (var i = list.First; i < list.End; i++)
                ui.Leaf(ui.GetId(i), new Box { Width = Sizing.Grow(), Height = 20f });
            inside?.Invoke(list);
            return (list.Offset, list.First);
        });

    [Fact]
    public void ScrollTo_in_the_frame_the_content_grows_is_kept()
    {
        var frames = new UiFrames();
        Frame(frames, 10);
        Frame(frames, 10);

        Frame(frames, 100_000, list => list.ScrollTo(50_000f));
        var after = Frame(frames, 100_000);

        Assert.Equal(50_000f, after.Offset);
        Assert.Equal(50_000 / 20 - 1, after.First);
    }

    [Fact]
    public void ScrollTo_before_the_first_layout_is_kept()
    {
        var frames = new UiFrames();

        Frame(frames, 1_000, list => list.ScrollTo(400f));
        Frame(frames, 1_000);
        var after = Frame(frames, 1_000);

        Assert.Equal(400f, after.Offset);
    }

    [Fact]
    public void An_app_owned_offset_follows_the_document_shown()
    {
        var frames = new UiFrames();
        var offsets = new[] { 500f, 0f };
        float Show(int document, float wheel = 0f)
        {
            if (wheel != 0f)
            {
                frames.MovePointer(100f, 100f);
                frames.Wheel(wheel);
            }
            frames.Run(1f / 60f, ui =>
            {
                using var screen = ui.Screen();
                using var list = ui.VirtualScroll("code", 1_000, 20f, ref offsets[document], new Box { Gap = 0f });
                for (var i = list.First; i < list.End; i++)
                    ui.Leaf(ui.GetId(i), new Box { Width = Sizing.Grow(), Height = 20f });
            });
            return offsets[document];
        }

        Show(0);
        Show(0);
        Assert.Equal(500f, Show(0));
        Assert.Equal(460f, Show(0, wheel: 1f));

        Assert.Equal(0f, Show(1));
        Assert.Equal(0f, Show(1));
        Assert.Equal(460f, Show(0));
        Assert.Equal(460f, frames.Run(0f, ui => ui.ScrollOffset(ui.GetId("code")).Y));
    }
}

public sealed class WheelRoutingTests
{
    private static void Nested(Ui ui)
    {
        using var screen = ui.Screen();
        using (ui.Scroll("outer", new Box { Gap = 0f }))
        {
            using (ui.Scroll("strip", new Box { Direction = Direction.Horizontal, Gap = 0f }))
            {
                for (var i = 0; i < 10; i++)
                    ui.Leaf(ui.GetId(i), new Box { Width = 100f, Height = 30f });
            }
            using (ui.Scroll("fits", new Box { Height = 60f, Gap = 0f }))
                ui.Leaf(new Box { Width = 10f, Height = 20f });
            ui.Leaf(new Box { Width = 10f, Height = 2_000f });
        }
    }

    private static (float Outer, float Strip) Wheel(float x, float y, double wheelY, double wheelX = 0)
    {
        var frames = new UiFrames(body: Nested).Frame(2);
        frames.MovePointer(x, y).Frame();
        frames.Wheel(wheelY, wheelX).Frame();
        return frames.Frame(ui => (ui.ScrollOffset(ui.GetId("outer")).Y, ui.ScrollOffset(ui.GetId("strip")).X));
    }

    [Fact]
    public void A_vertical_wheel_over_a_horizontal_strip_scrolls_the_column_around_it()
    {
        var (outer, strip) = Wheel(100f, 30f, -1);

        Assert.Equal(40f, outer);
        Assert.Equal(0f, strip);
    }

    [Fact]
    public void A_horizontal_wheel_over_the_strip_scrolls_the_strip()
    {
        var (outer, strip) = Wheel(100f, 30f, 0, -1);

        Assert.Equal(0f, outer);
        Assert.Equal(40f, strip);
    }

    [Fact]
    public void A_scroll_that_does_not_overflow_lets_the_wheel_through()
    {
        var (outer, _) = Wheel(100f, 70f, -1);

        Assert.Equal(40f, outer);
    }
}

public sealed class TwoAxisScrollTests
{
    private static void Body(Ui ui)
    {
        using var screen = ui.Screen();
        using (ui.Scroll("both", ScrollAxes.Both, new Box { Gap = 0f }))
            ui.Leaf(new Box { Width = 1_000f, Height = 2_000f });
    }

    private static UiFrames Settled() => new UiFrames(body: Body).Frame(3);

    private static (float X, float Y) Offset(UiFrames frames) => frames.Frame(ui => ui.ScrollOffset(ui.GetId("both")));

    [Fact]
    public void The_wheel_scrolls_each_axis()
    {
        var frames = Settled();
        frames.MovePointer(100f, 100f).Frame();

        frames.Wheel(-1, -2).Frame();

        Assert.Equal((80f, 40f), Offset(frames));
    }

    [Fact]
    public void ScrollTo_moves_both_axes_and_clamps_to_the_content()
    {
        var frames = Settled();

        frames.Frame(ui => ui.ScrollTo(ui.GetId("both"), 100f, 50f));
        Assert.True(frames.RequestedFrame);
        frames.Frame();
        Assert.Equal((100f, 50f), Offset(frames));

        frames.Frame(ui => ui.ScrollTo(ui.GetId("both"), 1e6f, 1e6f));
        frames.Frame();
        var (x, y) = Offset(frames);
        var content = frames.Frame(ui => ui.TryGetContentRect(ui.GetId("both"), out var rect) ? rect : default);

        Assert.Equal(1_000f - content.Width, x, 0.01f);
        Assert.Equal(2_000f - content.Height, y, 0.01f);
    }

    [Fact]
    public void The_content_rect_leaves_out_the_gutters_of_overflowing_axes()
    {
        var frames = Settled();

        var (rect, content) = frames.Frame(ui =>
        {
            ui.TryGetRect(ui.GetId("both"), out var r);
            ui.TryGetContentRect(ui.GetId("both"), out var c);
            return (r, c);
        });

        Assert.True(content.Width < rect.Width);
        Assert.True(content.Height < rect.Height);
        Assert.Equal(rect.X, content.X);
    }
}

public sealed class TabScrollTests
{
    private readonly List<string> _tabs = Enumerable.Range(0, 12).Select(i => $"Tab {i}").ToList();
    private int _selected;

    private void Body(Ui ui)
    {
        using var screen = ui.Screen();
        using var strip = ui.TabStrip("tabs");
        for (var i = 0; i < _tabs.Count; i++)
            strip.Tab(_tabs[i], i == _selected);
    }

    [Fact]
    public void A_tab_added_selected_in_one_frame_is_scrolled_into_view()
    {
        var frames = new UiFrames(body: Body).Frame(2);

        _tabs.Add("Newest");
        _selected = _tabs.Count - 1;
        frames.Frame(4);

        var strip = frames.RectOf("tabs");
        var tab = frames.RectOf("Newest");
        Assert.True(tab.Right <= strip.Right + 0.01f, $"{tab.Right} > {strip.Right}");
    }
}

public sealed class CancelledReleaseTests
{
    private int _clicks;
    private int _rightClicks;

    private void Body(Ui ui)
    {
        var target = ui.Interact("target");
        if (target.Clicked) _clicks++;
        if (target.ClickedWith(PointerButton.Right)) _rightClicks++;
        ui.Leaf(target.Id, new Box { Width = 200f, Height = 100f });
    }

    [Theory]
    [InlineData(PointerButton.Left)]
    [InlineData(PointerButton.Right)]
    public void Losing_focus_mid_press_clicks_nothing(PointerButton button)
    {
        var frames = new UiFrames(body: Body).Frame();
        frames.MovePointer(50f, 50f).Frame();
        frames.Press(button).Frame();

        frames.LoseFocus().Frame();

        Assert.Equal(0, _clicks);
        Assert.Equal(0, _rightClicks);
    }
}

public sealed class MeasurementInvalidationTests
{
    private sealed class Measurer : ITextMeasurer
    {
        public float CharWidth = 8f;

        public float MeasureTextWidth(ReadOnlySpan<char> text, TextStyle style) => text.Length * CharWidth;
        public float MeasureTextPrefix(ReadOnlySpan<char> text, int prefixLength, TextStyle style) => prefixLength * CharWidth;
        public float MeasureTextLineHeight(TextStyle style) => 16f;
    }

    [Fact]
    public void Registering_a_font_forgets_widths_measured_before()
    {
        var measurer = new Measurer();
        var frames = new UiFrames(measurer);
        Assert.Equal(40f, frames.Run(0f, ui => ui.MeasureText("hello")));

        measurer.CharWidth = 10f;
        Assert.Equal(40f, frames.Run(0f, ui => ui.MeasureText("hello")));
        frames.Run(0f, ui => ui.InvalidateMeasurements());

        Assert.Equal(50f, frames.Run(0f, ui => ui.MeasureText("hello")));
    }
}

public sealed class PostTests
{
    private sealed class App() : Gui<object>(new object())
    {
        protected override void Render(Ui ui, object state) { }
    }

    [Fact]
    public void Posting_before_the_app_runs_waits_for_the_first_frame()
    {
        var queue = new ConcurrentQueue<Action>();
        var ran = 0;
        queue.Enqueue(() => ran++);
        var ui = new Ui(new RecordingCanvas(new SyntheticTextMeasurer()), new TestClipboard(), null, queue);

        ui.BeginFrame(new UiInput(), 0f, 0, 100f, 100f);
        ui.EndFrame();

        Assert.Equal(1, ran);
    }

    [Fact]
    public void Gui_post_and_invalidate_are_safe_when_not_running()
    {
        var app = new App();

        app.Post(() => { });
        app.Invalidate();
    }
}

public sealed class PopupPixelTests
{
    [Fact]
    public void Popup_rects_snap_to_device_pixels()
    {
        var rect = PopupPlacement.SnapToPixels(new UiRect(10.3f, 20.5f, 50f, 40f), 1.5f);

        Assert.Equal(MathF.Round(rect.X * 1.5f), rect.X * 1.5f, 0.001f);
        Assert.Equal(MathF.Round(rect.Y * 1.5f), rect.Y * 1.5f, 0.001f);
    }

    [Theory]
    [InlineData(101, 1.5f, 1f, 152)]
    [InlineData(101, 1.25f, 1f, 126)]
    [InlineData(101, 2f, 2f, 101)]
    public void A_popup_window_holds_exactly_its_canvas(int points, float dpi, float pixelsPerScreen, int expected)
    {
        Assert.Equal(expected, PopupPlacement.ScreenSize(points, dpi, pixelsPerScreen));
    }
}

public sealed class HarnessTests
{
    [Fact]
    public void The_harness_reports_quit_bounds_and_background()
    {
        var frames = new UiFrames { WindowBounds = new WindowBounds(10, 20, 800, 600, true) };

        var bounds = frames.Run(0f, ui =>
        {
            using (ui.Screen(UiTheme.Light)) { }
            ui.Quit();
            return ui.WindowBounds;
        });

        Assert.True(frames.QuitRequested);
        Assert.Equal(new WindowBounds(10, 20, 800, 600, true), bounds);
        Assert.Equal(UiTheme.Light.Background, frames.WindowBackground);
    }

    [Fact]
    public void Saved_bounds_reopen_the_window_where_it_was()
    {
        var config = new WindowBounds(10, 20, 800, 600, true).ApplyTo(new StartupConfig { WindowWidth = 1, WindowHeight = 1, WindowTitle = "t" });

        Assert.Equal((10, 20, 800, 600, true), (config.WindowX, config.WindowY, config.WindowWidth, config.WindowHeight, config.StartMaximized));
    }
}

public sealed class TwoAxisVirtualScrollTests
{
    private const float RowHeight = 20f;
    private const int Rows = 1_000;

    private float _x;
    private float _y;
    private UiRect _clip;
    private UiRect _lastRow;

    private void Body(Ui ui, float rowWidth)
    {
        using var screen = ui.Screen();
        using var list = ui.VirtualScroll("code", Rows, RowHeight, ref _x, ref _y, new Box { Gap = 0f });
        for (var i = list.First; i < list.End; i++)
        {
            using (ui.Box(ui.GetId(i), new Box { Width = Sizing.Grow(min: rowWidth), Height = RowHeight }))
            {
                if (i == Rows - 1)
                    ui.Paint(this, static (p, t) => (t._clip, t._lastRow) = (p.Clip, p.Rect));
            }
        }
    }

    private static UiRect Content(UiFrames frames) => frames.Frame(ui => ui.TryGetContentRect(ui.GetId("code"), out var rect) ? rect : default);

    private UiFrames Settled(float rowWidth) => new UiFrames(body: ui => Body(ui, rowWidth)).Frame(4);

    [Fact]
    public void The_last_row_scrolled_to_sits_above_the_horizontal_bar()
    {
        var frames = Settled(2_000f);

        _y = 1e9f;
        frames.Frame(3);
        var content = Content(frames);

        Assert.True(content.Height < frames.Height);
        Assert.Equal(content.Bottom, _lastRow.Bottom, 0.01f);
        Assert.Equal(Rows * RowHeight - content.Height, _y, 0.01f);
    }

    [Fact]
    public void Rows_exactly_as_wide_as_the_view_scroll_sideways_past_the_vertical_bar()
    {
        var frames = Settled(400f);

        _x = 1e9f;
        _y = 1e9f;
        frames.Frame(3);
        var content = Content(frames);

        Assert.True(content.Width < 400f);
        Assert.Equal(400f - content.Width, _x, 0.01f);
        Assert.Equal(content.Right, _lastRow.Right, 0.01f);
        Assert.Equal(content.Bottom, _lastRow.Bottom, 0.01f);
    }

    [Fact]
    public void The_bars_do_not_meet_in_the_corner()
    {
        var frames = Settled(2_000f);
        _x = _y = 1e9f;
        frames.Frame(3);

        var thumb = UiTheme.Dark.ScrollThumb;
        var thumbs = frames.Canvas.Rects.Where(r => r.Inputs.Style.BackgroundColor == thumb).Select(r => r.Inputs.Position).ToList();

        Assert.Equal(2, thumbs.Count);
        var (a, b) = (thumbs[0], thumbs[1]);
        var overlaps = a.Left < b.Left + b.Width && b.Left < a.Left + a.Width && a.Bottom < b.Bottom + b.Height && b.Bottom < a.Bottom + a.Height;
        Assert.False(overlaps);
    }

    [Fact]
    public void App_owned_offsets_scroll_both_axes_and_take_the_wheel_back()
    {
        var frames = Settled(2_000f);
        _x = 300f;
        _y = 500f;
        frames.Frame(2);

        frames.MovePointer(100f, 100f).Frame();
        frames.Wheel(1, 1).Frame();

        var step = UiTheme.Dark.ScrollWheelStep;
        Assert.Equal((300f - step, 500f - step), (_x, _y));
        Assert.Equal((_x, _y), frames.Frame(ui => ui.ScrollOffset(ui.GetId("code"))));
    }

    [Fact]
    public void A_painter_knows_the_clip_a_scrolled_row_is_drawn_in()
    {
        var frames = Settled(2_000f);
        _x = 250f;
        _y = 1e9f;
        frames.Frame(3);
        var rect = frames.RectOf("code");

        Assert.Equal(rect, _clip);
        Assert.Equal(rect.X - 250f, _lastRow.X, 0.01f);
    }
}

public sealed class RecordingCanvasTests
{
    [Fact]
    public void With_recording_off_draws_are_counted_and_not_kept()
    {
        var frames = new UiFrames();
        frames.Canvas.Recording = false;

        frames.Run(0f, ui =>
        {
            using var screen = ui.Screen();
            ui.Text("hello");
            ui.Leaf(new Box { Width = 10f, Height = 10f, Background = 0xFFFF0000 });
        });

        Assert.Empty(frames.Canvas.Texts);
        Assert.Empty(frames.Canvas.Rects);
        Assert.True(frames.Canvas.CommandCount >= 2);
    }
}
