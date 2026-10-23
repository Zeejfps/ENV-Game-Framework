using ZGF.Desktop;
using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class WindowDialogTests
{
    private readonly TestPopupHost _host = new();
    private bool _open;
    private WindowBounds? _bounds;
    private int _underClicks;
    private int _okClicks;

    private void Body(Ui ui)
    {
        var under = ui.Interact("under");
        if (under.Clicked) _underClicks++;
        ui.Leaf(under.Id, new Box { Width = Sizing.Grow(), Height = Sizing.Grow() });

        using (var dialog = ui.WindowDialog("export", "Export", ref _open, 300f, 200f, ref _bounds))
        {
            if (dialog.IsOpen && ui.Button("OK"))
                _okClicks++;
        }
    }

    private UiFrames Opened(TestPopupHost? host)
    {
        var frames = new UiFrames(popups: host, width: 400f, height: 300f, body: Body).Frame();
        _open = true;
        frames.Frame(2);
        return frames;
    }

    private static int WindowId(Ui ui) => UiWidgets.DialogCardId(ui.GetId("export"));

    [Fact]
    public void The_dialog_draws_in_its_own_window_with_its_title()
    {
        var frames = Opened(_host);

        var id = frames.Frame(WindowId);
        var canvas = _host.CanvasOf(id);

        Assert.Equal(new UiRect(50f, 50f, 300f, 200f), _host.Windows[id]);
        Assert.Contains(canvas.Texts, t => t.Inputs.Text == "OK");
        Assert.Contains(canvas.Texts, t => t.Inputs.Text == "Export");
        Assert.False(frames.Drew("OK"));
    }

    [Fact]
    public void The_dialogs_own_close_button_closes_it()
    {
        var frames = Opened(_host);

        frames.ClickOn(ui => UiWidgets.CloseButtonId(ui.GetId("export")));

        Assert.False(_open);
    }

    private static UiFrames Drag(UiFrames frames, float x, float y, float dx, float dy)
    {
        frames.MovePointer(x, y).Frame();
        frames.Press().Frame();
        frames.MovePointer(x + dx, y + dy).Frame(3);
        frames.Release().Frame();
        return frames;
    }

    [Fact]
    public void Dragging_the_padding_moves_the_window_with_the_pointer()
    {
        var frames = Drag(Opened(_host), 55f, 55f, 30f, -20f);

        var id = frames.Frame(WindowId);
        Assert.Equal(new UiRect(80f, 30f, 300f, 200f), _host.Windows[id]);
    }

    [Fact]
    public void Dragging_the_empty_body_moves_the_window()
    {
        var frames = Drag(Opened(_host), 250f, 200f, -20f, 10f);

        var id = frames.Frame(WindowId);
        Assert.Equal(new UiRect(30f, 60f, 300f, 200f), _host.Windows[id]);
    }

    [Fact]
    public void Dragging_from_a_control_does_not_move_the_window()
    {
        var frames = Opened(_host);
        var ok = frames.RectOf(ui => ui.GetId("OK"));

        Drag(frames, ok.X + 4f, ok.Y + 4f, 40f, 40f);

        var id = frames.Frame(WindowId);
        Assert.Equal(new UiRect(50f, 50f, 300f, 200f), _host.Windows[id]);
    }

    [Fact]
    public void The_window_reopens_where_it_was_left()
    {
        var frames = Drag(Opened(_host), 55f, 55f, 30f, -20f);
        frames.Key(KeyboardKey.Escape).Frame(2);

        var reopened = new TestPopupHost();
        var again = new UiFrames(popups: reopened, width: 400f, height: 300f, body: Body).Frame();
        _open = true;
        again.Frame(2);

        var id = again.Frame(WindowId);
        Assert.Equal(new WindowBounds(80, 30, 300, 200, false), _bounds);
        Assert.Equal(new UiRect(80f, 30f, 300f, 200f), reopened.Windows[id]);
    }

    private static UiFrames DragGrip(UiFrames frames, float dx, float dy)
    {
        var grip = frames.RectOf(ui => HashCode.Combine(ui.GetId("export"), "resize"));
        var x = grip.X + grip.Width / 2;
        var y = grip.Y + grip.Height / 2;
        frames.MovePointer(x, y).Frame();
        frames.Press().Frame();
        frames.MovePointer(x + dx, y + dy).Frame(3);
        frames.Release().Frame();
        return frames;
    }

    [Fact]
    public void Dragging_the_corner_grip_sizes_the_window_with_the_pointer()
    {
        var frames = DragGrip(Opened(_host), 60f, 40f);

        var id = frames.Frame(WindowId);
        Assert.Equal(new UiRect(50f, 50f, 360f, 240f), _host.Windows[id]);
    }

    [Fact]
    public void The_grip_does_not_size_the_window_below_its_smallest_size()
    {
        var frames = DragGrip(Opened(_host), -500f, -500f);

        var id = frames.Frame(WindowId);
        Assert.Equal(320f, _host.Windows[id].Width);
        Assert.Equal(240f, _host.Windows[id].Height);
    }

    [Fact]
    public void The_grip_shows_the_diagonal_resize_cursor()
    {
        var frames = Opened(_host);
        var grip = frames.RectOf(ui => HashCode.Combine(ui.GetId("export"), "resize"));

        frames.MovePointer(grip).Frame(2);

        Assert.Equal(MouseCursor.ResizeNwse, frames.Cursor);
    }

    [Fact]
    public void The_window_keeps_its_size_however_small_the_main_window_is()
    {
        var frames = Opened(_host);
        frames.Width = 100f;
        frames.Height = 80f;
        frames.Frame();

        var id = frames.Frame(WindowId);

        Assert.Equal(300f, _host.Windows[id].Width);
        Assert.Equal(200f, _host.Windows[id].Height);
    }

    [Fact]
    public void Content_in_the_window_takes_clicks_and_the_main_window_takes_none()
    {
        var frames = Opened(_host);

        frames.ClickOn(ui => ui.GetId("OK"));
        frames.Click(20f, 20f);

        Assert.Equal(1, _okClicks);
        Assert.Equal(0, _underClicks);
        Assert.True(_open);
    }

    [Fact]
    public void The_windows_close_button_closes_the_dialog()
    {
        var frames = Opened(_host);
        var id = frames.Frame(WindowId);

        _host.CloseRequests.Add(id);
        frames.Frame(2);

        Assert.False(_open);
        Assert.Empty(_host.Windows);
    }

    [Fact]
    public void Escape_closes_the_dialog()
    {
        var frames = Opened(_host);

        frames.Key(KeyboardKey.Escape).Frame();

        Assert.False(_open);
    }

    [Fact]
    public void Without_windows_the_dialog_is_a_titled_card_over_the_main_window()
    {
        var frames = Opened(null);

        var card = frames.RectOf(WindowId);

        Assert.True(frames.Drew("Export"));
        Assert.True(frames.Drew("OK"));
        Assert.Equal(new UiRect(50f, 50f, 300f, 200f), card);
    }
}
