using ZGF.Geometry;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class PopupTests
{
    private static readonly UiRect Anchor = new(50f, 60f, 80f, 20f);

    [Fact]
    public void A_popup_sits_below_its_anchor_at_its_own_size()
    {
        var frames = new UiFrames(body: Body).Frame();

        var rect = frames.RectOf("popup");

        Assert.Equal(new UiRect(50f, 80f, 120f, 40f), rect);
    }

    [Fact]
    public void Over_the_window_a_popup_flips_above_when_there_is_no_room_below()
    {
        var frames = new UiFrames(body: ui => Popup(ui, new UiRect(50f, 270f, 80f, 20f))).Frame();

        var rect = frames.RectOf("popup");

        Assert.Equal(270f - 40f, rect.Y);
    }

    [Fact]
    public void A_popup_keeps_clicks_from_what_is_under_it()
    {
        var frames = new UiFrames();
        var clicks = 0;
        void Covered(Ui ui)
        {
            using var screen = ui.Screen();
            var under = ui.Interact("under");
            if (under.Clicked) clicks++;
            ui.Leaf(under.Id, new Box { Width = 300f, Height = 200f });
            Popup(ui, Anchor);
        }

        frames.Click(100f, 100f, Covered);
        Assert.Equal(0, clicks);

        frames.Click(250f, 150f, Covered);
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void A_pass_through_popup_lets_the_pointer_reach_what_is_under_it()
    {
        var frames = new UiFrames();
        var clicks = 0;
        void Covered(Ui ui)
        {
            using var screen = ui.Screen();
            var under = ui.Interact("under");
            if (under.Clicked) clicks++;
            ui.Leaf(under.Id, new Box { Width = 300f, Height = 200f });
            Popup(ui, Anchor, passThrough: true);
        }

        frames.Click(100f, 100f, Covered);

        Assert.Equal(1, clicks);
    }

    [Fact]
    public void The_wheel_over_a_popup_does_not_scroll_the_list_under_it()
    {
        var frames = new UiFrames(body: ui =>
        {
            using var screen = ui.Screen();
            using (ui.Scroll("list"))
            {
                for (var i = 0; i < 50; i++)
                    ui.Leaf(ui.GetId(i), new Box { Width = 10f, Height = 30f });
            }
            Popup(ui, Anchor);
        }).Frame();
        float FirstRowY() => frames.RectOf(ui => ui.GetId(0)).Y;

        frames.MovePointer(100f, 100f).Frame();
        frames.Wheel(-2).Frame(2);
        var underPopup = FirstRowY();

        frames.MovePointer(300f, 250f).Frame();
        frames.Wheel(-2).Frame(2);
        var beside = FirstRowY();

        Assert.Equal(16f, underPopup);
        Assert.True(beside < 16f);
    }

    [Fact]
    public void With_popup_windows_content_draws_on_the_popups_own_canvas_in_its_own_coordinates()
    {
        var host = new TestPopupHost();
        var frames = new UiFrames(body: Body, popups: host).Frame();

        var id = frames.Frame(ui => ui.GetId("popup"));
        var canvas = host.CanvasOf(id);
        var text = canvas.Texts.Single(t => t.Inputs.Text == "item");

        Assert.Equal(new UiRect(50f, 80f, 120f, 40f), host.Shown[id]);
        Assert.False(frames.Drew("item"));
        Assert.Equal(0f, text.Inputs.Position.Left);
        Assert.Equal(0f, text.Inputs.Position.Bottom);
    }

    [Fact]
    public void With_popup_windows_a_popup_may_extend_past_the_main_window()
    {
        var host = new TestPopupHost();
        var frames = new UiFrames(body: ui => Popup(ui, new UiRect(50f, 290f, 80f, 20f)), popups: host).Frame();

        var id = frames.Frame(ui => ui.GetId("popup"));

        Assert.Equal(310f, host.Shown[id].Y);
    }

    [Fact]
    public void Popup_content_is_clickable_in_window_coordinates()
    {
        var host = new TestPopupHost();
        var frames = new UiFrames(popups: host);
        var clicks = 0;
        void Clickable(Ui ui)
        {
            using (ui.Popup(ui.GetId("popup"), new UiRect(100f, 280f, 10f, 10f)))
            {
                var item = ui.Interact("item");
                if (item.Clicked) clicks++;
                ui.Leaf(item.Id, new Box { Width = 120f, Height = 40f });
            }
        }

        frames.Click(150f, 300f, Clickable);

        Assert.Equal(1, clicks);
    }

    private static void Body(Ui ui) => Popup(ui, Anchor);

    private static void Popup(Ui ui, UiRect anchor, bool passThrough = false)
    {
        using var screen = ui.Screen();
        using (ui.Popup(ui.GetId("popup"), anchor, new Box { Background = 0xFF202020 }, passThrough: passThrough))
            ui.Text("item", new Box { Width = 120f, Height = 40f });
    }
}
