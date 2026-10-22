using ZGF.Desktop;
using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class CloseButtonTests
{
    private int _clicks;
    private bool _tabStop = true;
    private bool _hideIdle;

    private UiFrames CreateFrames() => new(body: Body);

    private void Body(Ui ui)
    {
        if (ui.CloseButton(ui.GetId("close"), tabStop: _tabStop, hideIdle: _hideIdle))
            _clicks++;
    }

    private static bool HasFocus(UiFrames frames) => frames.Frame(ui => ui.HasFocus(ui.GetId("close")));

    [Fact]
    public void A_click_or_a_middle_click_presses_it()
    {
        var frames = CreateFrames().Frame();

        frames.ClickOn("close");
        frames.ClickOn("close", PointerButton.Middle);

        Assert.Equal(2, _clicks);
    }

    [Fact]
    public void Tab_focuses_it_and_Enter_presses_it()
    {
        var frames = CreateFrames().Frame();

        frames.Key(KeyboardKey.Tab).Frame();
        Assert.True(HasFocus(frames));
        frames.Key(KeyboardKey.Enter).Frame();

        Assert.Equal(1, _clicks);
    }

    [Fact]
    public void Without_a_tab_stop_it_takes_no_focus_and_still_shows_the_hand()
    {
        _tabStop = false;
        var frames = CreateFrames().Frame();

        frames.Key(KeyboardKey.Tab).Frame();
        frames.MovePointer(frames.RectOf("close")).Frame();

        Assert.False(HasFocus(frames));
        Assert.Equal(MouseCursor.Hand, frames.Cursor);
    }

    [Fact]
    public void A_cross_hidden_at_rest_shows_while_hovered()
    {
        _hideIdle = true;
        var frames = CreateFrames().Frame();
        Assert.Empty(frames.Canvas.Lines);

        frames.MovePointer(frames.RectOf("close")).Frame();

        Assert.Equal(2, frames.Canvas.Lines.Count);
    }

    [Fact]
    public void A_tab_shows_its_cross_only_while_selected_or_hovered()
    {
        var frames = new UiFrames(body: ui =>
        {
            using var strip = ui.TabStrip("tabs");
            strip.Tab("A", selected: true);
            strip.Tab("B", selected: false);
        }).Frame(2);
        Assert.Equal(2, frames.Canvas.Lines.Count);

        frames.MovePointer(frames.RectOf("B")).Frame();

        Assert.Equal(4, frames.Canvas.Lines.Count);
    }

    [Fact]
    public void A_floating_window_close_button_is_never_a_tab_stop()
    {
        var layout = new DockLayout { Root = DockLayout.Group("a") };
        var window = layout.Float("b", new UiRect(100f, 100f, 300f, 200f));
        var frames = new UiFrames(width: 800f, height: 600f, body: ui =>
        {
            using var dock = ui.DockSpace("dock", layout, static p => new DockTab(p));
            while (dock.Next(out _)) { }
        }).Frame();
        int CloseId(Ui ui) => UiWidgets.CloseButtonId(DockWalker.WindowId(ui, "dock", window));

        var focused = new List<bool>();
        for (var i = 0; i < 6; i++)
        {
            frames.Key(KeyboardKey.Tab).Frame();
            focused.Add(frames.Frame(ui => ui.HasFocus(CloseId(ui))));
        }
        frames.ClickOn(CloseId);

        Assert.DoesNotContain(true, focused);
        Assert.Empty(layout.Windows);
    }
}
